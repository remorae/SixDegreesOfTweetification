using Microsoft.EntityFrameworkCore;
using SixDegrees.Model;

namespace SixDegrees.Data
{
    public class TwitterCacheDbContext : DbContext
    {
        public TwitterCacheDbContext(DbContextOptions<TwitterCacheDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<UserResult> Users { get; set; }
        public DbSet<UserConnection> UserConnections { get; set; }

        public DbSet<UserConnectionLookupStatus> UserConnectionLookups { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<UserResult>()
                .ToTable("Users")
                .HasKey(user => user.ID);
            modelBuilder
                .Entity<UserConnection>()
                .ToTable("UserConnections")
                .HasKey(connection => new { connection.Start, connection.End });
        }
    }

    public class UserConnectionLookupStatus
    {
        public string ID { get; set; }
        public bool Queried { get; set; } = false;
    }

    public class UserConnection
    {
        public string Start { get; set; }
        public string End { get; set; }
    }
}
