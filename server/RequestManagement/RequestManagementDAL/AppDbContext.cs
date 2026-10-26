using Microsoft.EntityFrameworkCore;

namespace RequestManagementDAL
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<RequestEntity> Requests { get; set; }
        public DbSet<RequestStatusHistory> StatusHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // סינון לפי סטטוס + עדיפות ומיון לפי תאריך
            modelBuilder.Entity<RequestEntity>()
                .HasIndex(r => new { r.Status, r.Priority, r.CreatedAt });

            // חיפוש/סינון לפי שם ארגון
            modelBuilder.Entity<RequestEntity>()
                .HasIndex(r => r.OrganizationName);

            // סינון לפי מטפל
            modelBuilder.Entity<RequestEntity>()
                .HasIndex(r => r.AssignedTo);

            // סינון/מיון לפי טווח תאריכים
            modelBuilder.Entity<RequestEntity>()
                .HasIndex(r => r.CreatedAt);

            modelBuilder.Entity<RequestStatusHistory>()
                .HasIndex(h => h.RequestId);
        }
    }
}
