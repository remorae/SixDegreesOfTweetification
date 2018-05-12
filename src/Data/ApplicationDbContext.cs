using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SixDegrees.Model;

namespace SixDegrees.Data
{
    /// <summary>
    /// Used to access the Identity database.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> contextOptions)
            : base(contextOptions)
        {
            Database.EnsureCreated();
        }
    }
}
