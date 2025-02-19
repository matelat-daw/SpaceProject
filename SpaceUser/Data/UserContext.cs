﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SpaceUser.Data
{
    public class UserContext(DbContextOptions<UserContext> options) : IdentityDbContext<Models.User.SpaceUser>(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // USERS SEEDING
            List<Models.User.SpaceUser> users =
            [
                new()
            {
                Name = "Leslie Ann",
                Surname1 = "Ledesma",
                Surname2 = "",
                UserName = "ledesma.leslie@gmail.com",
                NormalizedUserName = "LEDESMA.LESLIE@GMAIL.COM",
                Email = "ledesma.leslie@gmail.com",
                NormalizedEmail = "LEDESMA.LESLIE@GMAIL.COM",
                PhoneNumber = "644388160",
                Bday = DateOnly.Parse("1995-07-15"),
                ProfileImage = "/images/1/profile.jpg",
                Active = true
            },
            new()
            {
                Name = "Patrick Edward",
                Surname1 = "Murphy",
                Surname2 = "González",
                UserName = "patrickmurphygon@gmail.com",
                NormalizedUserName = "PATRICKMURPHYGON@GMAIL.COM",
                Email = "patrickmurphygon@gmail.com",
                NormalizedEmail = "PATRICKMURPHYGON@GMAIL.COM",
                PhoneNumber = "634547833",
                Bday = DateOnly.Parse("2001-01-03"),
                ProfileImage = "/images/2/profile.jpg",
                Active = true
            },
            new()
            {
                Name = "César Osvaldo",
                Surname1 = "Matelat",
                Surname2 = "Borneo",
                UserName = "cesarmatelat@gmail.com",
                NormalizedUserName = "CESARMATELAT@GMAIL.COM",
                Email = "cesarmatelat@gmail.com",
                NormalizedEmail = "CESARMATELAT@GMAIL.COM",
                PhoneNumber = "664774821",
                Bday = DateOnly.Parse("1968-04-05"),
                ProfileImage = "/images/3/profile.jpg",
                Active = true
            }
            ];
            builder.Entity<Models.User.SpaceUser>().HasData(users);
            // PASSWORD HASH
            var passwordHasher = new PasswordHasher<Models.User.SpaceUser>();
            users[0].PasswordHash = passwordHasher.HashPassword(users[0], "Leslie@Leader");
            users[1].PasswordHash = passwordHasher.HashPassword(users[1], "Patrick@Coleader");
            users[2].PasswordHash = passwordHasher.HashPassword(users[2], "Cesar@Peon");

            // ROLES SEEDING
            List<IdentityRole> roles =
            [
                new()
            {
                Name = "User",
                NormalizedName = "USER"
            },
            new()
            {
                Name = "Premium",
                NormalizedName = "PREMIUM"
            },
            new()
            {
                Name = "Admin",
                NormalizedName = "ADMIN"
            }
            ];
            builder.Entity<IdentityRole>().HasData(roles);

            // USERS-ROLES SEEDING
            List<IdentityUserRole<string>> usersRoles =
            [
                new()
            {
                UserId = users[0].Id,
                RoleId = roles[2].Id
            },
            new()
            {
                UserId = users[1].Id,
                RoleId = roles[2].Id
            },
            new()
            {
                UserId = users[2].Id,
                RoleId = roles[2].Id
            }
            ];
            builder.Entity<IdentityUserRole<string>>().HasData(usersRoles);
        }
    }
}