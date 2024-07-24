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

        public DbSet<AgencyRegistration> AgencyRegistrations { get; set; }
        public DbSet<AgencyContact> AgencyContacts { get; set; }
        public DbSet<AdditionalUser> AdditionalUsers { get; set; }
        public DbSet<ShipInformation> ShipInformations { get; set; }
        public DbSet<County> Counties { get; set; }
        public DbSet<EaspSepsRegistrationCounty> EaspSepsRegistrationCounties { get; set; }
        public DbSet<Suffix> Suffixes { get; set; }
        public DbSet<Prefix> Prefixes { get; set; }
        public DbSet<ShipToSite> ShipToSites { get; set; }
        public DbSet<ShipToSiteCounty> ShipToSiteCounties { get; set; }
        public DbSet<LkAgencyClassification> LkAgencyClassifications { get; set; }
        public DbSet<AgentClassificationData> AgentClassificationData { get; set; }
        public DbSet<Product> Products { get; set; }

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

            modelBuilder.Entity<AgencyRegistration>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AgencyContact>()
                .HasOne<AgencyRegistration>()
                .WithMany()
                .HasForeignKey(a => a.AgencyRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdditionalUser>()
                .HasOne<AgencyRegistration>()
                .WithMany()
                .HasForeignKey(a => a.AgencyRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShipInformation>()
                .HasOne<AgencyRegistration>()
                .WithMany()
                .HasForeignKey(s => s.AgencyRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EaspSepsRegistrationCounty>()
                .HasKey(e => new { e.AgencyRegistrationId, e.CountyId });

            modelBuilder.Entity<EaspSepsRegistrationCounty>()
                .HasOne(e => e.AgencyRegistration)
                .WithMany(e => e.CountiesServed)
                .HasForeignKey(e => e.AgencyRegistrationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EaspSepsRegistrationCounty>()
                .HasOne(e => e.County)
                .WithMany()
                .HasForeignKey(e => e.CountyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Suffix>().ToTable("lk_Suffix");
            modelBuilder.Entity<Prefix>().ToTable("lk_Prefix");

            modelBuilder.Entity<ShipToSite>()
                .HasOne(e => e.AgencyRegistration)
                .WithMany(e => e.ShipToSites)
                .HasForeignKey(e => e.AgencyRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShipToSiteCounty>()
                .HasKey(s => new { s.ShipToSiteId, s.CountyId });

            modelBuilder.Entity<ShipToSiteCounty>()
                .HasOne(s => s.ShipToSite)
                .WithMany(s => s.PrimaryCountiesServed)
                .HasForeignKey(s => s.ShipToSiteId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<ShipToSiteCounty>()
                .HasOne(s => s.County)
                .WithMany()
                .HasForeignKey(s => s.CountyId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<LkAgencyClassification>().ToTable("lk_agency_classification")
                .HasKey(c => c.agency_classification_id);

            // Add this configuration for AgentClassificationData
            modelBuilder.Entity<AgentClassificationData>()
                .HasKey(a => a.Id);

            modelBuilder.Entity<AgentClassificationData>()
                .Property(a => a.OtherClassificationText)
                .HasMaxLength(300);

            modelBuilder.Entity<AgentClassificationData>()
                .Property(a => a.UniqueId)
                .IsRequired();

            modelBuilder.Entity<Product>().ToTable("lk_product");

            modelBuilder.Entity<ApplicationUser>().ToTable("AspNetUsers");
            modelBuilder.Entity<IdentityRole>().ToTable("AspNetRoles");
        }
    }
}
