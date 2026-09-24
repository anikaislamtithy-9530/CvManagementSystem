using CvManagement.Domain.Entities;
using CvManagement.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CvManagement.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        // ==========================================
        // 1. Seed Roles
        // ==========================================
        foreach (var roleName in new[] { "Candidate", "Recruiter", "Administrator" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // ==========================================
        // 2. Seed Built-in Attributes ("Me" section)
        // ==========================================
        if (!await db.AttributeDefinitions.AnyAsync(a => a.IsBuiltIn))
        {
            var builtIns = new List<AttributeDefinition>
            {
                new()
                {
                    Name = "First Name",
                    Description = "User's first name",
                    Category = AttributeCategory.PersonalInformation,
                    DataType = AttributeDataType.String,
                    IsBuiltIn = true,
                    MaxLength = 100
                },
                new()
                {
                    Name = "Last Name",
                    Description = "User's last name",
                    Category = AttributeCategory.PersonalInformation,
                    DataType = AttributeDataType.String,
                    IsBuiltIn = true,
                    MaxLength = 100
                },
                new()
                {
                    Name = "Location",
                    Description = "User's current location (city, country)",
                    Category = AttributeCategory.PersonalInformation,
                    DataType = AttributeDataType.String,
                    IsBuiltIn = true,
                    MaxLength = 200
                },
                new()
                {
                    Name = "Personal Photo",
                    Description = "Profile photo URL (external cloud storage)",
                    Category = AttributeCategory.PersonalInformation,
                    DataType = AttributeDataType.Image,
                    IsBuiltIn = true
                }
            };

            db.AttributeDefinitions.AddRange(builtIns);
            await db.SaveChangesAsync();
        }

        // ==========================================
        // 3. Seed Sample Library Attributes
        // ==========================================
        if (!await db.AttributeDefinitions.AnyAsync(a => !a.IsBuiltIn))
        {
            var libraryAttrs = new List<AttributeDefinition>
            {
                new()
                {
                    Name = "IELTS Score",
                    Description = "Overall IELTS band score (0.0 - 9.0)",
                    Category = AttributeCategory.Language,
                    DataType = AttributeDataType.Numeric,
                    MinValue = 0,
                    MaxValue = 9,
                    Unit = "band"
                },
                new()
                {
                    Name = "Remote Work Availability",
                    Description = "Willing to work remotely",
                    Category = AttributeCategory.SoftSkills,
                    DataType = AttributeDataType.Boolean
                },
                new()
                {
                    Name = "Presentation Skills",
                    Description = "Public speaking and presentation level",
                    Category = AttributeCategory.SoftSkills,
                    DataType = AttributeDataType.OneOfMany,
                    DropdownOptionsJson = "[\"Beginner\",\"Intermediate\",\"Advanced\",\"Expert\"]"
                },
                new()
                {
                    Name = "English Level",
                    Description = "English proficiency (CEFR)",
                    Category = AttributeCategory.Language,
                    DataType = AttributeDataType.OneOfMany,
                    DropdownOptionsJson = "[\"A1\",\"A2\",\"B1\",\"B2\",\"C1\",\"C2\",\"Native\"]"
                },
                new()
                {
                    Name = "GPA",
                    Description = "Grade Point Average (0.0 - 4.0)",
                    Category = AttributeCategory.Education,
                    DataType = AttributeDataType.Numeric,
                    MinValue = 0,
                    MaxValue = 4
                },
                new()
                {
                    Name = "Bio",
                    Description = "Short biography (Markdown supported)",
                    Category = AttributeCategory.PersonalInformation,
                    DataType = AttributeDataType.Text,
                    MaxLength = 2000
                },
                new()
                {
                    Name = "Certifications",
                    Description = "Professional certifications (Markdown)",
                    Category = AttributeCategory.Certification,
                    DataType = AttributeDataType.Text,
                    MaxLength = 3000
                }
            };

            db.AttributeDefinitions.AddRange(libraryAttrs);
            await db.SaveChangesAsync();
        }

        // ==========================================
        // 4. Seed Default Admin User
        // ==========================================
        const string adminEmail = "admin@cvsystem.com";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        
        if (existingAdmin == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator",
                Location = "Remote"
            };

            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Administrator");

                // Create profile
                db.Profiles.Add(new Profile { UserId = admin.Id });
                await db.SaveChangesAsync();
            }
        }
    }
}