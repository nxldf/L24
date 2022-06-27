﻿using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using DataLayer.Enitities;
using DataLayer.Configurations;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace DataLayer
{
    public class ApplicationUser : IdentityUser
    {
        public virtual UserProfile userDetail { get; set; }
        public virtual AdminProfile adminDetail { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager, string authenticationType)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, authenticationType);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext() : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .HasRequired(c => c.photo)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Category>()
                .HasRequired(s => s.photo)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ProductshippingDetail>()
              .HasRequired(s => s.country)
              .WithMany()
              .WillCascadeOnDelete(false);

            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            //modelBuilder.Entity<Product>().HasRequired<UserProfile>(s => s.user).WithMany(a => a.Products);

            modelBuilder.Entity<Article>().HasMany(m => m.ReletedArticles).WithMany();

            modelBuilder.Configurations.Add(new ShoppingCartConfig());

            base.OnModelCreating(modelBuilder);
        }
        public DbSet<Gallery> Galleries { get; set; }
        public DbSet<Bid> Bids { get; set; }
        public DbSet<ClosingHistory> ClosingHistories { get; set; }
        public DbSet<Listing> Listings { get; set; }
        public DbSet<AuctionInfo> Auctions { get; set; }
        public DbSet<Watch> Watches { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<VisitRequest> VisitRequests { get; set; }
        public DbSet<VisitorLog> VisitorLogs { get; set; }
        public DbSet<ProductTranslation> ProductTranslations { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<SubjectTranslation> SubjectTranslations { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CategoryTranslation> CategoryTranslations { get; set; }
        public DbSet<Medium> Mediums { get; set; }
        public DbSet<MediumTranslation> MediumTranslations { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<MaterialTranslation> MaterialTranslations { get; set; }
        public DbSet<Style> Styles { get; set; }
        public DbSet<StyleTranslation> StyleTranslations { get; set; }
        public DbSet<SiteParam> SiteParams { get; set; }
        public DbSet<SiteParamTranslation> SiteParamTranslations { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<AdminProfile> AdminProfiles { get; set; }
        public DbSet<ImageServer> ImageServers { get; set; }
        public DbSet<NavigationCategory> NavigationCategories { get; set; }
        public DbSet<NavigationCategoryFavStyle> NavigationCategoryFavStyles { get; set; }
        public DbSet<NavigationCategoryFavMedium> NavigationCategoryFavMediums { get; set; }
        public DbSet<NavigationCategorySubject> NavigationCategorySubjects { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<CollectionProduct> CollectionProduct { get; set; }
        public DbSet<PersonalInformation> PersonalInformations { get; set; }
        public DbSet<UserLink> UserLinks { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<CountryTranslation> CountryTranslations { get; set; }
        public DbSet<Pricethreshold> Pricethresholds { get; set; }
        public DbSet<PricethresholdTranslation> PricethresholdTranslations { get; set; }
        public DbSet<Favorit> Favorits { get; set; }
        public DbSet<BillingInfo> BillingInfoes { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<PrintMaterial> PrintMaterials { get; set; }
        public DbSet<PrintSize> PrintSizes { get; set; }
        public DbSet<PrintFrame> PrintFrames { get; set; }
        public DbSet<PrintFrameTranslation> PrintFrameTranslations { get; set; }
        public DbSet<PrintSizeTranslation> PrintSizeTranslations { get; set; }
        public DbSet<PrintMaterialTranslation> PrintMaterialTranslations { get; set; }
        public DbSet<SitePage> SitePages { get; set; }
        public DbSet<SitePageTranslation> SitePageTranslations { get; set; }
        public DbSet<sliderImage> sliderImages { get; set; }
        public DbSet<footerCell> footerCells { get; set; }
        public DbSet<footerCellTranslation> footerCellTranslations { get; set; }
        public DbSet<footercellRow> footercellRows { get; set; }
        public DbSet<sliderImageTranslation> sliderImageTranslations { get; set; }
        public DbSet<FirstPageSection> FirstPageSections { get; set; }
        public DbSet<ProductFrameColor> ProductFrameColors { get; set; }
        public DbSet<ProductFrameColorTranslation> ProductFrameColorTranslations { get; set; }
        public DbSet<ProductFrameMaterial> ProductFrameMaterials { get; set; }
        public DbSet<ProductFrameMaterialTranslation> ProductFrameMaterialTranslations { get; set; }
        public DbSet<ProductFrameType> ProductFrameTypes { get; set; }
        public DbSet<ProductFrameTypeTranslation> ProductFrameTypeTranslations { get; set; }
        public DbSet<TransactionDetail> TransactionDetails { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<WordCategory> WordCategories { get; set; }
        public DbSet<Word> Words { get; set; }
        public DbSet<WordTranslation> WordTranslations { get; set; }
        public DbSet<Log> logs { get; set; }
        public DbSet<SettingValue> SettingValues { get; set; }
        public DbSet<PayoutRequest> PayoutRequests { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<SupportCategory> SupportCategories { get; set; }
        public DbSet<SupportSubCategory> SupportSubCategories { get; set; }
        public DbSet<MobileHomePage> MobileHomePages { get; set; }
        public DbSet<MobileHomePageItem> MobileHomePageItems { get; set; }
        public DbSet<MobileHomePageTranslation> MobileHomePageTranslations { get; set; }
    }
}