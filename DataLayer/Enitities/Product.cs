﻿using DataLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataLayer.Enitities
{
    public class Product : ITranslatable<Product, ProductTranslation>
    {
        public virtual int Id { get; set; }
        [ForeignKey("user")]
        public virtual String user_id { get; set; }
        [Display(Name = "کاربر")]
        public virtual UserProfile user { get; set; }
        public virtual String artist_id { get; set; }
        public String artistName { get; set; }
        public virtual ICollection<ProductTranslation> Translations { get; set; }
        public virtual ICollection<ReserveList> reservedlist { get; set; }
        public int Price { get; set; }
        [ForeignKey("OrginalPhoto")]
        public int? OrginalPhotoId { get; set; }
        public virtual Photo OrginalPhoto { get; set; }
        [ForeignKey("photo")]
        public int? photoId { get; set; }
        public virtual Photo photo { get; set; }
        [ForeignKey("Sqphoto")]
        public int? SqphotoId { get; set; }
        public virtual Photo Sqphoto { get; set; }
        [ForeignKey("Widephoto")]
        public int? WidephotoId { get; set; }
        public virtual Photo Widephoto { get; set; }
        public DateTime CreateDate { get; set; }
        public int ArtCreatedYear { get; set; }
        public string ArtCreatedYearString { get; set; }
        public bool ISOrginalForSale { get; set; }
        public bool IsArchive { get; set; }
        public bool IsPrintAvaibled { get; set; }
        public bool framed { get; set; }
        public bool multiPaneled { get; set; }
        public bool IsAuctionAvailable { get; set; }
        public virtual ProductFrameType frameType { get; set; }
        public virtual ProductFrameMaterial frameMaterial { get; set; }
        public virtual ProductFrameColor frameColor { get; set; }
        public string frame_detail { get; set; }
        public virtual Category category { get; set; }
        [ForeignKey("category")]
        public int categoryId { get; set; }
        public virtual Subject subject { get; set; }
        [ForeignKey("subject")]
        public int subjectId { get; set; }
        public string Keywords { get; set; }
        public float Height { get; set; }
        public float Width { get; set; }
        public float Depth { get; set; }
        public float ArtWeight { get; set; }
        public float BoxWeight { get; set; }
        public float TotalWeight { get; set; }
        public string Title { get; set; }
        public int ViewCount { get; set; }
        public int FavoritedCount { get; set; }
        public int avaible { get; set; }
        public int AllEntity { get; set; }
        public string Description { get; set; }
        public Productpackage Packaging { get; set; }
        public ProductStatus Status { get; set; }
        public virtual ICollection<Style> Styles { get; set; }
        public virtual ICollection<Material> Materials { get; set; }
        public virtual ICollection<Medium> Mediums { get; set; }
        public virtual ProductshippingDetail productshippingDetail { get; set; }
        public virtual ProductPrintDetail productPrintDetail { get; set; }        

        [NotMapped]
        public List<int> MaterialList { get; set; }
        public Product()
        {
            CreateDate = DateTime.Now;
            Status = ProductStatus.NotForSale;
        }

        public object tojason()
        {
            return new
            {
                id = Id,
                allEntity = AllEntity,
                createdYear = ArtCreatedYear,
                createdYearString = ArtCreatedYearString,
                avaible = avaible,
                categoryId = categoryId,
                createDate = CreateDate,
                package = Packaging,
                depth = Depth,
                country = user.billingInfo != null ? user.billingInfo.country.Id : 0,
                description = Description,
                height = Height,
                isForSale = ISOrginalForSale,
                isPrintAvaibled = IsPrintAvaibled,
                keywords = Keywords,
                author = user.FirstName + " " + user.LastName,
                authorId = user_id,
                photo = photo.Path,
                price = Price,
                status = Status,
                sqphoto = Sqphoto.Path,
                subjectId = subjectId,
                styles = Styles.Select(x => x.Id),
                mediums = Mediums.Select(x => x.Id),
                materials = Materials.Select(x => x.Id),
                widephoto = Widephoto.Path,
                width = Width,
                frameType = frameType != null ? frameType.Id : 0,
                frameColor = frameColor != null ? frameColor.Id : 0,
                frameMaterial = frameMaterial != null ? frameMaterial.Id : 0,
                isFramed = framed,
                totalWeight = TotalWeight,
                title = Title                
            };
        }

        public string GenerateSlug()
        {
            string phrase = string.Format("{0}-{1}", Id, string.IsNullOrEmpty(Title) ? artistName : Title);

            string str = phrase.ToLower();//RemoveAccent(phrase).ToLower();
            // invalid chars           
            str = Regex.Replace(str, @"[^a-z0-9\u0600-\u06FF\s-]", "");
            // convert multiple spaces into one space   
            str = Regex.Replace(str, @"\s+", " ").Trim();
            // cut and trim 
            str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
            str = Regex.Replace(str, @"\s", "-"); // hyphens   
            return str;
        }

        private string RemoveAccent(string text)
        {
            byte[] bytes = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(text);
            return System.Text.Encoding.ASCII.GetString(bytes);
        }
    }

    public enum ProductStatus { Sold, forSale, NotForSale }

    public class ProductPrintDetail
    {
        public int Id { get; set; }
        public virtual ICollection<ProductPrintDetailmaterial> ProductPrintDetailmaterials { get; set; }
    }

    public class ProductPrintDetailmaterial
    {
        public int Id { get; set; }
        [ForeignKey("printMaterial")]
        public int printMaterialId { get; set; }
        public virtual PrintMaterial printMaterial { get; set; }
    }

    public enum Productpackage
    {
        box, crate, tube
    }

    public class ProductshippingDetail
    {
        public int Id { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string ZipCode { get; set; }
        public string PhoneNumber { get; set; }
        public int CountryId { get; set; }
        [ForeignKey("CountryId")]
        public virtual Country country { get; set; }
    }

    public class ProductTranslation : ITranslation<Product>
    {
        [Key, Column(Order = 0)]
        [ForeignKey("language")]
        public string languageId { get; set; }
        public virtual Language language { get; set; }
        [Key, Column(Order = 1)]
        public virtual int ProductId { get; set; }
        public virtual Product Product { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
