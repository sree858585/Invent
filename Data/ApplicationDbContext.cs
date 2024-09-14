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
        public DbSet<LnkAgencyClassificationData> Lnk_AgencyClassificationData { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<QuarterlyReport> QuarterlyReports { get; set; }
        public DbSet<CollectionDetail> CollectionDetails { get; set; } // New DbSet for CollectionDetails

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
                .HasOne(a => a.AgencyRegistration)
                .WithMany(r => r.AgencyContacts)
                .HasForeignKey(a => a.AgencyRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdditionalUser>()
                .HasOne(a => a.AgencyRegistration)
                .WithMany(r => r.AdditionalUsers)
                .HasForeignKey(a => a.AgencyRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShipInformation>()
                .HasOne(s => s.AgencyRegistration)
                .WithMany(r => r.ShipInformations)
                .HasForeignKey(s => s.AgencyRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EaspSepsRegistrationCounty>()
    .HasKey(e => new { e.AgencyRegistrationId, e.CountyId });

            modelBuilder.Entity<EaspSepsRegistrationCounty>()
                .HasOne(e => e.AgencyRegistration)
                .WithMany(ar => ar.CountiesServed)
                .HasForeignKey(e => e.AgencyRegistrationId)
                .OnDelete(DeleteBehavior.Cascade); // Use Cascade if you want to delete related CountiesServed when an Agency is deleted.

            modelBuilder.Entity<EaspSepsRegistrationCounty>()
                .HasOne(e => e.County)
                .WithMany()
                .HasForeignKey(e => e.CountyId)
                .OnDelete(DeleteBehavior.Cascade); // Similarly, use Cascade if you want the relationship to be deleted when a County is deleted.


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

            modelBuilder.Entity<LnkAgencyClassificationData>()
                .HasKey(a => a.Id);

            modelBuilder.Entity<LnkAgencyClassificationData>()
                .Property(a => a.UniqueId)
                .IsRequired(false);

            modelBuilder.Entity<LnkAgencyClassificationData>()
                .HasOne(a => a.AgencyRegistration)
                .WithMany(ar => ar.LnkAgencyClassificationData)
                .HasForeignKey(a => a.AgencyRegistrationId);

            modelBuilder.Entity<Product>().ToTable("lk_product")
                .HasQueryFilter(p => !p.is_deleted);

            modelBuilder.Entity<Order>()
                 .ToTable("orders");

            modelBuilder.Entity<OrderDetail>()
                .ToTable("order_details")
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany()
                .HasForeignKey(od => od.product_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CollectionDetail>()
     .HasOne(cd => cd.QuarterlyReport)
     .WithMany(qr => qr.CollectionDetails)
     .HasForeignKey(cd => cd.QuarterlyReportId)
     .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUser>().ToTable("AspNetUsers");
            modelBuilder.Entity<IdentityRole>().ToTable("AspNetRoles");
        }
    }
}
