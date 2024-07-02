using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<EaspSepsRegistration> EaspSepsRegistrations { get; set; }
        public DbSet<AgencyContact> AgencyContacts { get; set; }
        public DbSet<AdditionalUser> AdditionalUsers { get; set; }
        public DbSet<ShipInformation> ShipInformations { get; set; }
        public DbSet<County> Counties { get; set; }
        public DbSet<EaspSepsRegistrationCounty> EaspSepsRegistrationCounties { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.Role).HasColumnName("Role");
                entity.Property(e => e.IsApproved).HasColumnName("IsApproved");
            });

            modelBuilder.Entity<County>().ToTable("lk_county")
                .HasKey(c => c.county_id);

            modelBuilder.Entity<EaspSepsRegistration>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AgencyContact>()
                .HasOne<EaspSepsRegistration>()
                .WithMany()
                .HasForeignKey(a => a.EaspSepsRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdditionalUser>()
                .HasOne<EaspSepsRegistration>()
                .WithMany()
                .HasForeignKey(a => a.EaspSepsRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShipInformation>()
                .HasOne<EaspSepsRegistration>()
                .WithMany()
                .HasForeignKey(s => s.EaspSepsRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EaspSepsRegistrationCounty>()
                .HasKey(e => new { e.EaspSepsRegistrationId, e.CountyId });

            modelBuilder.Entity<EaspSepsRegistrationCounty>()
                .HasOne(e => e.EaspSepsRegistration)
                .WithMany(e => e.CountiesServed)
                .HasForeignKey(e => e.EaspSepsRegistrationId);

            modelBuilder.Entity<EaspSepsRegistrationCounty>()
                .HasOne(e => e.County)
                .WithMany()
                .HasForeignKey(e => e.CountyId);

            modelBuilder.Entity<ApplicationUser>().ToTable("AspNetUsers");
            modelBuilder.Entity<IdentityRole>().ToTable("AspNetRoles");
        }
    }
}