﻿using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TASVideos.Data;
using TASVideos.Data.Entity;
using TASVideos.Services;
using TASVideos.Tasks;

namespace TASVideos.Test.MVC.Tasks
{
	[TestClass]
	[TestCategory("UserTasks")]
	public class UserTaskTests
    {
		private const string TestUserName = "TestUser";
		private const PermissionTo TestPermission = PermissionTo.CreateForumPosts;
		private const PermissionTo TestPermission2 = PermissionTo.EditHomePage; // These can't be the same

		private UserTasks _userTasks;
		private ApplicationDbContext _db;
		private Mock<IPointsService> _pointsService;

		private static User TestUser => new User
		{
			Id = 1,
			UserName = TestUserName
		};

		private static Role TestRole => new Role
		{
			Id = 1,
			Name = "TestRole",
			RolePermission = new[]
			{
				new RolePermission
				{
					RoleId = 1,
					PermissionId = TestPermission
				}
			}
		};

		[TestInitialize]
		public void Initialize()
		{
			_pointsService = new Mock<IPointsService>(MockBehavior.Strict);

			var options = new DbContextOptionsBuilder<ApplicationDbContext>()
				.UseInMemoryDatabase("TestDb")
				.Options;

			_db = new ApplicationDbContext(options, null);
			_db.Database.EnsureDeleted();

			_userTasks = new UserTasks(_db, null, null, new NoCacheService(), _pointsService.Object); // TODO: managers
		}

		[TestCleanup]
		public void Cleanup()
		{
			_db.Dispose();
		}

		[TestMethod]
		public async Task GetUserPermissionsById_UserWithRoleAndPermission()
		{
			_db.Users.Add(TestUser);
			_db.Roles.Add(TestRole);
			_db.UserRoles.Add(new UserRole
			{
				UserId = TestUser.Id,
				RoleId = TestRole.Id
			});
			_db.SaveChanges();

			var result = await _userTasks.GetUserPermissionsById(1);
			Assert.IsNotNull(result, "Result should not be null");
			var list = result.ToList();
			Assert.AreEqual(1, list.Count, "Result should have 1 permission");
			Assert.AreEqual(TestPermission, list[0], $"Permission must be {nameof(TestPermission)}");
		}

		[TestMethod]
		[TestCategory("Test124")]
		public async Task GetUserPermissionsById_UserWIthNoRoles_ReturnsEmptyList()
		{
			_db.Users.Add(TestUser);
			_db.SaveChanges();

			var result = await _userTasks.GetUserPermissionsById(1);
			Assert.IsNotNull(result, "Result should not be null");
			Assert.AreEqual(0, result.Count(), "User should have no permissions");
		}

		[TestMethod]
		public async Task GetUserPermissionsById_UserWithRoleWithNoPermissions_ReturnsEmptyList()
		{
			_db.Users.Add(TestUser);
			_db.Roles.Add(new Role { Id = 1, Name = "No Permissions" });
			_db.SaveChanges();

			var result = await _userTasks.GetUserPermissionsById(1);
			Assert.IsNotNull(result, "Result should not be null");
			Assert.AreEqual(0, result.Count(), "User should have no permissions");
		}

		[TestMethod]
		public async Task GetUserPermissionsById_UserWithRolesWithSamePermissions_ReturnsNoDuplicates()
		{
			_db.Users.Add(TestUser);
			_db.Roles.Add(TestRole);
			_db.UserRoles.Add(new UserRole
			{
				UserId = TestUser.Id,
				RoleId = TestRole.Id
			});
			_db.Roles.Add(new Role
			{
				Id = 2,
				Name = "Redundant Role",
				RolePermission = new[]
				{
					new RolePermission
					{
						RoleId = 2,
						PermissionId = TestPermission
					}
				}
			});

			_db.SaveChanges();
			var result = await _userTasks.GetUserPermissionsById(1);
			Assert.IsNotNull(result, "Result should not be null");
			Assert.AreEqual(1, result.Count(), "User should have 1 permission");
		}

		[TestMethod]
		public async Task GetUserPermissionsById_MultipleRoles_ReturnsAggregate()
		{
			_db.Users.Add(TestUser);
			_db.Roles.Add(TestRole);
			_db.UserRoles.Add(new UserRole
			{
				UserId = TestUser.Id,
				RoleId = TestRole.Id
			});
			_db.Roles.Add(new Role
			{
				Id = 2,
				Name = "New Role",
				RolePermission = new[]
				{
					new RolePermission
					{
						RoleId = 2,
						PermissionId = TestPermission2
					}
				}
			});
			_db.UserRoles.Add(new UserRole
			{
				UserId = TestUser.Id,
				RoleId = 2
			});

			_db.SaveChanges();
			var result = await _userTasks.GetUserPermissionsById(1);
			Assert.IsNotNull(result, "Result should not be null");
			var list = result.ToList();
			Assert.AreEqual(2, list.Count, "User should have 2 permissions");
			Assert.AreNotEqual(list[0], list[1], "Permissions must be different");
		}

		[TestMethod]
		public async Task GetUserNameById_ValidId_ReturnsCorrectName()
		{
			_db.Users.Add(TestUser);
			_db.SaveChanges();

			var result = await _userTasks.GetUserNameById(1);
			Assert.AreEqual(TestUserName, result, $"UserName should be {nameof(TestUserName)}");
		}

		[TestMethod]
		public async Task GetUserNameById_InvalidId_ReturnsNull()
		{
			_db.Users.Add(TestUser);
			_db.SaveChanges();

			var result = await _userTasks.GetUserNameById(int.MaxValue);
			Assert.IsNull(result, "Result shoudl be null");
		}
	}
}
