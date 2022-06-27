﻿using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Utilities;
using ArtShop.Util;
using Microsoft.AspNet.Identity;
using ArtShop.Models;
using DataLayer.Enitities;
using DataLayer.Extentions;
using System.Globalization;

namespace ArtShop.Controllers
{
    public class ProductsController : BaseController
    {

        [Route("{culture}/search")]
        public ActionResult Search(int category = 0, int style = 0, int subject = 0, int medium = 0, int price = 0, int page = 1)
        {
            int pageSize = 18;
            var manager = CashManager.Instance;
            var category_cash = manager.Categories.SingleOrDefault(x => x.id == category);
            ViewBag.categoryName = category_cash != null ? category_cash.name : Resources.SearchRes.All_Categories;
            ViewBag.categoryId = category;
            var style_cash = manager.Styles.Any(x => x.Key == style);
            ViewBag.styleName = style_cash ? manager.Styles[style] : Resources.SearchRes.All_Styles;
            ViewBag.styleId = style;
            var subject_cash = manager.Subjects.Any(x => x.Key == subject);
            ViewBag.subjectName = subject_cash ? manager.Subjects[subject] : Resources.SearchRes.All_Subjects;
            ViewBag.subjectId = subject;
            var medium_cash = manager.Mediums.Any(x => x.Key == medium);
            ViewBag.mediumName = medium_cash ? manager.Mediums[medium] : Resources.SearchRes.All_Mediums;
            ViewBag.mediumId = medium;
            var price_cash = manager.Pricethresholds.SingleOrDefault(x => x.Id == price);
            ViewBag.priceName = price_cash != null ? price_cash.Name : Resources.SearchRes.All_Prices;
            ViewBag.priceId = price;

            var p = db.Products.OrderByDescending(x => x.CreateDate).AsQueryable();
            p = p.Where(x => category == 0 || x.categoryId == category).AsQueryable();
            p = p.Where(x => style == 0 || x.Styles.FirstOrDefault(y => y.Id == style) != null);
            p = p.Where(x => subject == 0 || x.subjectId == subject).AsQueryable();
            p = p.Where(x => medium == 0 || x.Mediums.FirstOrDefault(y => y.Id == medium) != null);
            if (price_cash != null && price_cash.max.HasValue)
                p = p.Where(x => x.Price < price_cash.max.Value && x.ISOrginalForSale);
            if (price_cash != null && price_cash.min.HasValue)
                p = p.Where(x => x.Price >= price_cash.min.Value && x.Price > 0 && x.ISOrginalForSale);

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.Identity.GetUserId();

                var userProfile = db.UserProfiles.Find(userId);
                ViewBag.favorites = userProfile.Favorits;
            }

            var count = p.Count();
            page = Math.Min(page, (int)Math.Ceiling((float)count / (float)pageSize));
            page = Math.Max(1, page);
            ViewBag.page = page;
            ViewBag.count = count;
            ViewBag.pageSize = pageSize;

            p = p.Skip((page - 1) * pageSize).Take(pageSize);

            var res = p.ToList();
            return View(res);
        }

        [Route("{culture}/search/{category}/{style}/{subject}/{medium}/{price}/{page?}")]
        public ActionResult Searchparameters(int category = 0, int style = 0, int subject = 0, int medium = 0, int price = 0, int page = 1)
        {
            return RedirectToActionPermanent("search", new { category = category, style = style, subject = subject, medium = medium, price = price, page = page });
        }

        public ActionResult single(int id)
        {
            var p = db.Products.Find(id);
            string ip = Request.UserHostAddress;
            string browser = Request.UserAgent;


            ViewBag.metaDescription = GenerateMeta(p);

            if (!db.VisitorLogs.Any(x => x.LocationIP == ip && x.ArtID == id))
            {
                db.VisitorLogs.Add(new VisitorLog { BrowserName = browser, LocationIP = ip, ArtID = id });
                p.ViewCount++;
                db.SaveChanges();
            }

            bool mine = false;
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.Identity.GetUserId();
                var user = db.Users.Find(userId);
                var profile = user.userDetail;
                ViewBag.favorites = profile.Favorits;
                mine = profile.Products.Any(x => x.Id == id);
                var candeleted = !db.Orders.Where(x => x.TransactionDetail != null).Where(x => x.TransactionDetail.Payed).SelectMany(x => x.OrderDetails).Select(x => x.ProductId).Any(x => x == id);
                ViewBag.candeleted = candeleted;
            }
            ViewBag.mine = mine;
            var Agency = p.artist_id == "" ? null : db.UserProfiles.FirstOrDefault(a => a.Id == p.artist_id);
            ViewBag.Agency = Agency;
            if (Agency == null)
            {
                ViewBag.artistName = p.artistName;
            }
            return View(p);
        }

        [Authorize]
        public ActionResult RequestVisit(int id)
        {
            var p = db.Products.Find(id);
            ViewBag.artwork = p;
            ViewBag.metaDescription = GenerateMeta(p);

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.Identity.GetUserId();
                var user = db.Users.Find(userId);
                var profile = user.userDetail;

            }

            VisitRequestViewModel model = new VisitRequestViewModel();
            model.Id = id;

            var Agency = p.artist_id == "" ? null : db.UserProfiles.FirstOrDefault(a => a.Id == p.artist_id);
            ViewBag.Agency = Agency;
            if (Agency == null)
            {
                ViewBag.artistName = p.artistName;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestVisit(VisitRequestViewModel model)
        {
            var p = db.Products.Find(model.Id);
            ViewBag.artwork = p;
            ViewBag.metaDescription = GenerateMeta(p);

            var Agency = p.artist_id == "" ? null : db.UserProfiles.FirstOrDefault(a => a.Id == p.artist_id);
            ViewBag.Agency = Agency;
            if (Agency == null)
            {
                ViewBag.artistName = p.artistName;
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var profile = user.userDetail;

            VisitRequest vr = new VisitRequest();
            vr.ArtworkID = model.Id;
            vr.FirstName = model.FirstName;
            vr.LastName = model.LastName;
            vr.PhoneNumber = model.PhoneNumber;
            vr.Description = model.Description;
            vr.user_id = userId;
            vr.user = profile;
            vr.SubmittedOn = DateTime.Now;
            db.VisitRequests.Add(vr);
            db.SaveChanges();
            
            return RedirectToActionPermanent("single", new { id = p.Id });
        }
        public string GenerateMeta(Product p)
        {
            string meta = "";
            string mediums = "";
            string material = "";
            bool first = true;
            string currentCultureName = CultureInfo.CurrentCulture.Name.Substring(0, 2);

            foreach (var item in p.Mediums)
            {
                mediums += (first == true ? " " : ",") + item.Translations.FirstOrDefault(x => x.languageId == currentCultureName).Name;
                first = false;
            }

            first = true;

            foreach (var item in p.Materials)
            {
                material += (first == true ? " " : ",") + item.Translations.FirstOrDefault(x => x.languageId == currentCultureName).Name;
                first = false;
            }

            return meta = (p.Title + " " + Resources.SearchRes.by + " " + p.user.FirstName ?? " " + " " + p.user.LastName ?? " ") + " " + Resources.ShareRes.With_specification + mediums + " " + Resources.SearchRes.On + "" + material + " " + Resources.ShareRes.Created_in + " " + (p.ArtCreatedYear == 0 ? p.ArtCreatedYearString : p.ArtCreatedYear.ToString()) + " " + Resources.ShareRes.Submitted_on + " " + p.CreateDate;
        }

        [HttpGet]
        public ActionResult remove(int id)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.Identity.GetUserId();
                var user = db.Users.Find(userId);
                var profile = user.userDetail;
                var p = profile.Products.SingleOrDefault(x => x.Id == id);
                if (p != null)
                {
                    var candeleted = !db.Orders.Where(x => x.TransactionDetail != null).Where(x => x.TransactionDetail.Payed).SelectMany(x => x.OrderDetails).Select(x => x.ProductId).Any(x => x == id);
                    if (candeleted)
                    {
                        db.Products.Remove(p);
                        db.SaveChanges();
                    }
                    return RedirectToActionPermanent("artworks", "profile", new { });
                }
            }
            return Content("error");
        }

        [HttpGet]
        public ActionResult archive(int id)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.Identity.GetUserId();
                var user = db.Users.Find(userId);
                var profile = user.userDetail;
                var p = profile.Products.SingleOrDefault(x => x.Id == id);
                if (p != null)
                {
                    p.IsArchive = true;
                    db.SaveChanges();
                    return RedirectToActionPermanent("artworks", "profile", new { });
                }
            }
            return Content("error");
        }

        public ActionResult AddOrRemoveFavorit(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { IsAuthenticated = false, isInMyFavList = false }, JsonRequestBehavior.AllowGet);
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var profile = user.userDetail;
            var art = db.Products.FirstOrDefault(x => x.Id == id);

            if (profile.Favorits.Any(x => x.productId == id))
                profile.Favorits.Remove(profile.Favorits.First(x => x.productId == id));
            else
            {
                profile.Favorits.Add(new DataLayer.Enitities.Favorit() { productId = id });
                art.FavoritedCount++;
            }
            db.SaveChanges();
            bool isInMylist = profile.Favorits.Any(x => x.productId == id);
            return Json(new { IsAuthenticated = true, isInMyFavList = isInMylist }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AddToCollection(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { IsAuthenticated = false }, JsonRequestBehavior.AllowGet);
            var p = db.Products.Find(id);
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var profile = user.userDetail;
            ViewBag.collections = profile.Collections.ToList();
            ViewBag.productid = id;
            return PartialView(p);
        }
        [HttpPost]
        public ActionResult AddToCollection(SearchCollectionViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            var p = db.Products.Find(model.productId);
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var profile = user.userDetail;
            if (model.collectionId == 0)
            {
                Collection newCollection = new Collection() { Title = model.collectionName };
                newCollection.Artworks = new List<CollectionProduct>();
                newCollection.Artworks.Add(new CollectionProduct() { productId = model.productId });
                profile.Collections.Add(newCollection);
            }
            else
            {
                var oldCollection = profile.Collections.SingleOrDefault(x => x.Id == model.collectionId);
                if (oldCollection != null && !oldCollection.Artworks.Any(x => x.productId == model.productId))
                    oldCollection.Artworks.Add(new CollectionProduct() { productId = model.productId });
            }
            try
            {
                db.SaveChanges();
                return Json(new { result = true }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }
        }



        [Authorize]
        public ActionResult Edit(int id)
        {
            var p = db.Products.Include("photo").Include("productshippingDetail").Single(x => x.Id == id);
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var profile = user.userDetail;
            ViewBag.Agencys = db.UserProfiles.Where(x => x.profileType == ProfileType.Agency && (x.FirstName != null && x.LastName != null)).Select(a => new AgencyViewModel
            {
                Id = a.Id,
                Firstname = a.FirstName,
                Lastname = a.LastName

            }).ToList();

            bool mine = profile.Products.Any(x => x.Id == id);
            if (!mine)
                return HttpNotFound();
            if (profile.billingInfo == null)
                profile.billingInfo = new BillingInfo();
            return View(p);
        }
        [Authorize]
        [HttpPost]
        public ActionResult Edit(Product model)
        {
            var p = db.Products.Include("photo").Include("productshippingDetail").Single(x => x.Id == model.Id);
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var profile = user.userDetail;
            bool mine = profile.Products.Any(x => x.Id == model.Id);
            ViewBag.Agencys = db.UserProfiles.Where(x => x.profileType == ProfileType.Agency && (x.FirstName != null && x.LastName != null)).Select(a => new AgencyViewModel
            {
                Id = a.Id,
                Firstname = a.FirstName,
                Lastname = a.LastName

            }).ToList();
            if (!mine)
                return HttpNotFound();
            if (string.IsNullOrEmpty(model.artistName) && user.userDetail.profileType == ProfileType.Collector)
            {
                ModelState.AddModelError(string.Empty, "Agency name can not be empty.");
                if (model.productshippingDetail == null)
                    model.productshippingDetail = new ProductshippingDetail();
                return View(model);
            }
            p.Title = model.Title ?? "";
            if (profile.isIDConfirmed)
            {
                p.Status = model.Status;
                if (p.avaible < 1)
                {
                    p.Status = ProductStatus.Sold;
                }
            }
            else
                p.Status = ProductStatus.NotForSale;


            string artistName = model.artistName;

            var Agency = db.UserProfiles.FirstOrDefault(x => (x.FirstName + " " + x.LastName) == artistName);
            string AgencyId = "";
            if (Agency != null)
            {
                AgencyId = Agency.Id;
            }
            p.artist_id = AgencyId;
            p.artistName = artistName;
            if (model.TotalWeight == 0)
                model.TotalWeight = float.Parse(Request["TotalWeight"].Replace(".", "/"));

            if (model.Depth == 0)
            {
                model.Depth = float.Parse(Request["Depth"].Replace(".", "/"));
            }
            if (model.Height == 0)
            {
                model.Height = float.Parse(Request["Height"].Replace(".", "/"));

            }
            if (model.Width == 0)
            {
                model.Width = float.Parse(Request["Width"].Replace(".", "/"));
            }

            p.TotalWeight = model.TotalWeight;
            p.Height = model.Height;
            p.Width = model.Width;
            p.Depth = model.Depth;
            if (model.Status == ProductStatus.forSale)
            {
                if (p.user.billingInfo == null)
                    p.user.billingInfo = new BillingInfo();
                p.user.billingInfo.Street = model.user.billingInfo.Street;
                p.user.billingInfo.City = model.user.billingInfo.City;
                if (model.user.billingInfo.CountryId != 0)
                    p.user.billingInfo.CountryId = model.user.billingInfo.CountryId;
                p.user.billingInfo.PhoneNumber = model.user.billingInfo.PhoneNumber;
                p.user.billingInfo.Region = model.user.billingInfo.Region;
                p.user.billingInfo.ZipCode = model.user.billingInfo.ZipCode;
            }
            db.SaveChanges();
            if (p.user.billingInfo == null)
                p.user.billingInfo = new BillingInfo();
            return View(p);
        }

        [Authorize]
        public ActionResult EditDetail(int id)
        {
            var p = db.Products.Include("photo").Single(x => x.Id == id);
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var profile = user.userDetail;

            if (p.ArtCreatedYear != 0)
            {
                p.ArtCreatedYearString = p.ArtCreatedYear.ToString();
            }

            bool mine = profile.Products.Any(x => x.Id == id);
            if (!mine)
                return HttpNotFound();
            return View(p);
        }
        [HttpPost]
        [Authorize]
        public ActionResult EditDetail(Product model)
        {
            var p = db.Products.Include("photo").Single(x => x.Id == model.Id);
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var profile = user.userDetail;
            bool mine = profile.Products.Any(x => x.Id == model.Id);
            if (!mine)
                return HttpNotFound();

            p.categoryId = model.categoryId;

            if (p.IsAuctionAvailable)
            {
                p.Status = ProductStatus.NotForSale;
            }

            p.IsAuctionAvailable = model.IsAuctionAvailable;
            p.subjectId = model.subjectId;
            p.ArtCreatedYearString = model.ArtCreatedYearString;
            p.Mediums.Clear();
            p.Styles.Clear();
            var medumsList = Request["Mediums"].Split(',');
            foreach (var item in medumsList)
            {
                var temp = db.MediumTranslations.FirstOrDefault(x => x.Name == item);
                if (temp != null)
                    p.Mediums.Add(temp.medium);
            }
            var styeList = Request["Styleslist"].Split(',');
            foreach (var item in styeList)
            {
                var temp = db.StyleTranslations.FirstOrDefault(x => x.Name == item);
                if (temp != null)
                    p.Styles.Add(temp.style);
            }
            db.SaveChanges();
            return View(p);
        }

        [Authorize]
        public ActionResult EditDescription(int id)
        {
            var p = db.Products.Include("photo").Single(x => x.Id == id);
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var profile = user.userDetail;
            bool mine = profile.Products.Any(x => x.Id == id);
            if (!mine)
                return HttpNotFound();
            return View(p);
        }
        [HttpPost]
        [Authorize]
        public ActionResult EditDescription(Product model)
        {
            var p = db.Products.Include("photo").Single(x => x.Id == model.Id);
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var profile = user.userDetail;
            bool mine = profile.Products.Any(x => x.Id == model.Id);
            if (!mine)
                return HttpNotFound();

            p.Materials.Clear();
            foreach (var item in model.MaterialList)
            {
                var temp = db.Materials.Find(item);
                if (temp != null)
                    p.Materials.Add(temp);
            }
            p.Description = model.Description;
            p.Keywords = model.Keywords;
            db.SaveChanges();
            return View(p);
        }

        [Authorize]
        public ActionResult EditPackag(int id)
        {
            var p = db.Products.Include("photo").Single(x => x.Id == id);
            bool mine = false;
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.Identity.GetUserId();
                var user = db.Users.Find(userId);
                var profile = user.userDetail;
                mine = profile.Products.Any(x => x.Id == id);
            }
            if (!mine)
                return HttpNotFound();

            return View(p);
        }

        [HttpPost]
        [Authorize]
        public ActionResult EditPackag(Product model)
        {
            var p = db.Products.Include("photo").Single(x => x.Id == model.Id);
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var profile = user.userDetail;
            bool mine = profile.Products.Any(x => x.Id == model.Id);
            if (!mine)
                return HttpNotFound();

            p.Packaging = model.Packaging;
            p.multiPaneled = model.multiPaneled;
            p.framed = model.framed;

            if (model.frameMaterial != null)
                p.frameMaterial = db.ProductFrameMaterials.Find(model.frameMaterial.Id);
            if (model.frameColor != null)
                p.frameColor = db.ProductFrameColors.Find(model.frameColor.Id);
            if (model.frameType != null)
                p.frameType = db.ProductFrameTypes.Find(model.frameType.Id);

            db.SaveChanges();
            return View(p);
        }

        [Authorize]
        public ActionResult EditPricing(int id)
        {
            var p = db.Products.Include("photo").Single(x => x.Id == id);
            bool mine = false;
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.Identity.GetUserId();
                var user = db.Users.Find(userId);
                var profile = user.userDetail;
                mine = profile.Products.Any(x => x.Id == id);
            }
            if (!mine)
                return HttpNotFound();

            return View(p);
        }

        [HttpPost]
        [Authorize]
        public ActionResult EditPricing(Product model)
        {
            var p = db.Products.Include("photo").Single(x => x.Id == model.Id);
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var profile = user.userDetail;
            bool mine = profile.Products.Any(x => x.Id == model.Id);
            if (!mine)
                return HttpNotFound();

            p.Price = model.Price;
            db.SaveChanges();
            return View(p);
        }

        [Authorize]
        public ActionResult EditImage()
        {

            return View();
        }
        [Authorize]
        [HttpPost]
        public ActionResult EditImage(Product model)
        {

            return View();
        }

    }
}