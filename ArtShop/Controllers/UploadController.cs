﻿using ArtShop.Models;
using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Utilities;
using DataLayer.Extentions;
using System.Configuration;
using ArtShop.Util;
using Microsoft.AspNet.Identity;
using DataLayer.Enitities;
using System.Net.Http;
using System.Threading.Tasks;
using System.Globalization;
using System.Data.Entity.Validation;
using ArtShop.Helper;
using static ArtShop.Models.UploadViewModel;

namespace ArtShop.Controllers
{
    [Authorize]
    public class UploadController : BaseController
    {
        ApplicationDbContext db = new ApplicationDbContext();
        [Route("{culture}/upload/start")]
        public ActionResult Index()
        {
            return View();
        }

        private ImageServer getAvaibleServer()
        {
            return db.ImageServers.FirstOrDefault();
        }
        private async Task<ISResizeViewModel> resize(UploadViewModel.step4 model)
        {
            ISResizeViewModel obj = null;
            var iserverid = (int)Session["iserver"];
            var iserver = db.ImageServers.Find(iserverid);
            obj = await resizeasync(model, "https://" + iserver.Host);
            return obj;
        }

        private async Task<ISResizeViewModel> resizeasync(UploadViewModel.step4 model, string uri)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(uri);
            HttpResponseMessage response = await client.PostAsJsonAsync("upload/resize", model);
            response.EnsureSuccessStatusCode();
            var res = await response.Content.ReadAsAsync<ISResizeViewModel>();

            return res;
        }

        [Route("{culture}/upload/review")]
        public ActionResult indexStart()
        {
            return View();
        }

        public ActionResult start_ltr()
        {
            return PartialView();
        }

        public ActionResult strat_rtl()
        {
            return PartialView();
        }

        //upload picture
        public ActionResult Setep1()
        {
            var iserver = getAvaibleServer();
            Session["iserver"] = iserver.Id;
            if (iserver != null)
            {
                //if (Request.IsSecureConnection)
                ViewBag.iserver = "https://" + iserver.Host + "/upload/upload";
                //else
                //    ViewBag.iserver = "http://" + iserver.Host + "/upload/upload";
                ViewBag.lastpic = (string)Session["imageAddress"];

                return PartialView();
            }
            else
            {
                return HttpNotFound();
            }
        }
        [HttpPost]
        public ActionResult Setep1(UploadViewModel.step1 model)
        {
            Session["imageAddress"] = model.img;

            Session["img_width"] = model.width;
            Session["img_height"] = model.height;
            if (string.IsNullOrEmpty(model.img))
            {
                ViewBag.Error = Resources.UploadRes.Image_cannot_be_empty;
                return PartialView();
            }
            return RedirectToActionPermanent("Setep2");
        }

        // category and subject
        public ActionResult Setep2()
        {
            ViewBag.subjects = CashManager.Instance.Subjects;
            ViewBag.category = CashManager.Instance.Categories;
            return PartialView();
        }
        [HttpPost]
        public ActionResult Setep2(UploadViewModel.step2 model)
        {
            if (model.category == 0 || model.subject == 0)
            {
                ViewBag.Error = Resources.UploadRes.select_subject;
                ViewBag.subjects = CashManager.Instance.Subjects;
                ViewBag.category = CashManager.Instance.Categories;
                return PartialView();
            }
            Session["category"] = model.category;
            Session["subject"] = model.subject;
            return RedirectToActionPermanent("Setep3");
        }

        //year,forsale,print and copyright
        public ActionResult Setep3()
        {
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var profile = user.userDetail;
            ViewBag.auction = profile.AuctionCapability;
            return PartialView();
        }
        [HttpPost]
        public ActionResult Setep3(UploadViewModel.step3 model)
        {
            if (model.copyright == false)
            {
                ViewBag.Error = Resources.UploadRes.copyright_error;
                return PartialView();
            }
            if (model.createYearString.Length == 0)
            {
                ViewBag.Error = Resources.UploadRes.step3_year_string;
                return PartialView();
            }
            if (Session["imageAddress"] == null)
                return RedirectToActionPermanent("Setep1");

            Session["copyright"] = model.copyright;
            Session["createYear"] = model.createYearString;
            Session["isOrginal"] = model.isOrginal;
            Session["auction"] = model.auction;
            Session["printAvable"] = false;// model.printAvable;
            return RedirectToActionPermanent("Setep4");
        }

        //RESIZE PICTURE
        public ActionResult Setep4()
        {
            bool printAvable = (bool)Session["printAvable"];
            bool isforsale = (bool)Session["isOrginal"];
            float total = 7 + (isforsale ? 3 : 0) + (printAvable ? 1 : 0);
            float current = 4;
            ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";

            ViewBag.img = (string)Session["imageAddress"];
            return PartialView();
        }
        [HttpPost]
        public async Task<ActionResult> Setep4(UploadViewModel.step4 model)
        {
            model = model.square_width == 0 || model.wide_width == 0 || model.square_height == 0 || model.wide_height == 0 ? new UploadViewModel.step4()
            {
                wide_x = float.Parse(Request["wide_x"].Replace(".", "/")),
                wide_height = float.Parse(Request["wide_height"].Replace(".", "/")),
                wide_width = float.Parse(Request["wide_width"].Replace(".", "/")),
                wide_y = float.Parse(Request["wide_y"].Replace(".", "/")),
                square_height = float.Parse(Request["square_height"].Replace(".", "/")),
                square_width = float.Parse(Request["square_width"].Replace(".", "/")),
                square_x = float.Parse(Request["square_x"].Replace(".", "/")),
                square_y = float.Parse(Request["square_y"].Replace(".", "/")),
            } : model;

            model.image = (string)Session["imageAddress"];

            var result = await resize(model);

            if (result.result)
            {
                Session["WideFullPath"] = result.WideFullPath;
                Session["SqureFullPath"] = result.SqureFullPath;
            }
            else
            {
                ViewBag.img = (string)Session["imageAddress"];
                ViewBag.error = result.error;
                db.logs.Add(new Log()
                {
                    Location = "step4",
                    Message = "" + result.error
                });
                db.SaveChanges();

                bool printAvable = (bool)Session["printAvable"];
                bool isforsale = (bool)Session["isOrginal"];
                float total = 7 + (isforsale ? 3 : 0) + (printAvable ? 1 : 0);
                float current = 4;
                ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";

                return PartialView();
            }

            return RedirectToActionPermanent("Setep5");
        }

        //medum,material,style and keywords
        public ActionResult Setep5()
        {
            ViewBag.lastpic = Session["WideFullPath"];
            ViewBag.Mediums = CashManager.Instance.Mediums;
            ViewBag.Materials = CashManager.Instance.Materials;
            ViewBag.Styles = CashManager.Instance.Styles;

            bool printAvable = (bool)Session["printAvable"];
            bool isforsale = (bool)Session["isOrginal"];
            float total = 7 + (isforsale ? 3 : 0) + (printAvable ? 1 : 0);
            float current = 5;
            ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";

            return PartialView();
        }
        [HttpPost]
        public ActionResult Setep5(UploadViewModel.step5 model)
        {
            if (string.IsNullOrEmpty(model.Keywords) || model.Materials.Length == 0 || string.IsNullOrEmpty(model.Mediums) || string.IsNullOrEmpty(model.Styles))
            {
                ViewBag.lastpic = Session["imageAddress"];
                ViewBag.Mediums = CashManager.Instance.Mediums;
                ViewBag.Materials = CashManager.Instance.Materials;
                ViewBag.Styles = CashManager.Instance.Styles;
                ViewBag.error = Resources.UploadRes.Empty_Error;
                return PartialView();
            }

            if (model.Keywords.Split(',').Count() < 5)
            {
                ViewBag.error = Resources.UploadRes.keyword_lenght_error;

                bool printAvable = (bool)Session["printAvable"];
                bool isforsale = (bool)Session["isOrginal"];
                float total = 7 + (isforsale ? 3 : 0) + (printAvable ? 1 : 0);
                float current = 5;
                ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";

                return PartialView();
            }

            Session["Mediums"] = model.Mediums;
            Session["Materials"] = model.Materials;
            Session["Styles"] = model.Styles;
            Session["Keywords"] = model.Keywords;

            Session["firstmedium"] = model.Mediums.Split(',').First();
            Session["firstmaterial"] = CashManager.Instance.Materials.SingleOrDefault(x => x.Key == model.Materials.First()).Value;
            return RedirectToActionPermanent("Setep6");
        }

        //get size with canvas
        public ActionResult Setep6()
        {
            bool printAvable = (bool)Session["printAvable"];
            bool isforsale = (bool)Session["isOrginal"];
            float total = 7 + (isforsale ? 3 : 0) + (printAvable ? 1 : 0);
            float current = 6;
            ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";

            return PartialView();
        }
        [HttpPost]
        public ActionResult Setep6(UploadViewModel.step6 model)
        {
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


            bool error = false;
            if (model.Height == 0)
            {
                ViewBag.error = Resources.UploadRes.zeroHeight_error;
                error = true;
            }
            if (model.Width == 0)
            {
                ViewBag.error = Resources.UploadRes.zeroWidth_error;
                error = true;
            }
            if (model.Depth == 0)
            {
                ViewBag.error = Resources.UploadRes.zeroDepth_error;
                error = true;
            }

            if (error)
            {
                bool printAvable = (bool)Session["printAvable"];
                bool isforsale = (bool)Session["isOrginal"];
                float total = 7 + (isforsale ? 3 : 0) + (printAvable ? 1 : 0);
                float current = 6;
                ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";
                return PartialView();
            }

            Session["Height"] = model.Height;
            Session["Width"] = model.Width;
            Session["Depth"] = model.Depth;

            return RedirectToActionPermanent("Setep7");
        }

        //title and description // done if nor fore sale
        public ActionResult Setep7()
        {
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            ViewBag.Keywords = Session["Keywords"];
            ViewBag.firstmedium = Session["firstmedium"];
            ViewBag.firstmaterial = Session["firstmaterial"];
            ViewBag.profileType = user.userDetail.profileType;
            ViewBag.Agencys = db.UserProfiles.Where(x => x.profileType == ProfileType.Agency && (x.FirstName != null && x.LastName != null)).Select(a => new AgencyViewModel
            {
                Id = a.Id,
                Firstname = a.FirstName,
                Lastname = a.LastName

            }).ToList();

            int category = (int)Session["category"];
            ViewBag.categoryString = CashManager.Instance.Categories.FirstOrDefault(a => a.id == category).name;
            bool printAvable = (bool)Session["printAvable"];
            bool isforsale = (bool)Session["isOrginal"];
            float total = 7 + (isforsale ? 3 : 0) + (printAvable ? 1 : 0);
            float current = 7;
            ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";

            return PartialView();
        }
        [HttpPost]
        public ActionResult Setep7(UploadViewModel.step7 model)
        {
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);

            bool printAvable = (bool)Session["printAvable"];
            bool isforsale = (bool)Session["isOrginal"];
            float total = 7 + (isforsale ? 3 : 0) + (printAvable ? 1 : 0);
            float current = 7;

            if (string.IsNullOrEmpty(model.AgencyName) && user.userDetail.profileType == ProfileType.Collector)
            {
                ViewBag.error = Resources.UploadRes.AgencyName_Null_error;
                ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";
                return PartialView();
            }
            //if (model.Title.Length > 35)
            //{
            //    ViewBag.error = Resources.UploadRes.TitleLenth_error;
            //    ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";
            //    return PartialView();
            //}
            //if (string.IsNullOrEmpty(model.Description) || model.Description.Length < 10)
            //{
            //    ViewBag.error = Resources.UploadRes.descriptionnull_error;
            //    ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";
            //    return PartialView();
            //}
            if (model.AllEntity < model.avaible)
            {
                ViewBag.error = Resources.UploadRes.avaibleEntity_error;
                ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";
                return PartialView();
            }
            Session["Title"] = model.Title;
            Session["avaible"] = model.avaible;
            Session["Description"] = model.Description;
            Session["AllEntity"] = model.AllEntity;
            Session["AgencyName"] = model.AgencyName;

            if (!isforsale && !printAvable)
            {
                int id = 0;
                var error = uploadnow(out id);
                if (error == string.Empty)
                {
                    return RedirectToActionPermanent("Stepfinish", new { id = id });
                }
                else
                {
                    ViewBag.error = error;
                    ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";
                    return PartialView();
                }
            }
            else if (!isforsale && printAvable)
            {
                return RedirectToActionPermanent("Setep9_5");
            }
            else
                return RedirectToActionPermanent("Setep8");
        }

        //Packaging and frame
        public ActionResult Setep8()
        {
            bool printAvable = (bool)Session["printAvable"];
            bool isforsale = (bool)Session["isOrginal"];
            float total = 7 + (isforsale ? 3 : 0) + (printAvable ? 1 : 0);
            float current = 8;
            ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";

            return PartialView();
        }
        [HttpPost]
        public ActionResult Setep8(UploadViewModel.step8 model)
        {
            Session["Packaging"] = model.Packaging;
            Session["framed"] = model.framed;
            Session["multi_paneled"] = model.multi_paneled;
            Session["frameMaterial"] = model.frameMaterial;
            Session["frameColor"] = model.frameColor;
            Session["frameType"] = model.frameType;

            return RedirectToActionPermanent("Setep9");
        }

        public ActionResult Setep9()
        {
            var userId = User.Identity.GetUserId();

            var userProfile = db.UserProfiles.Find(userId);

            bool printAvable = (bool)Session["printAvable"];
            bool isforsale = (bool)Session["isOrginal"];
            float total = 7 + (isforsale ? 3 : 0) + (printAvable ? 1 : 0);
            float current = 9;
            ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";
            var count = CashManager.Instance.Countries;
            UploadViewModel.step9 model = new UploadViewModel.step9();
            if (userProfile.billingInfo != null)
            {
                model.Street_Address = userProfile.billingInfo.Street + " " + userProfile.billingInfo.Unit; ;
                model.City = userProfile.billingInfo.City;
                model.Country = userProfile.billingInfo.country.Current().Name;
                model.Region = userProfile.billingInfo.Region;
                model.Zip_code = userProfile.billingInfo.ZipCode;
                model.phoneNumber = userProfile.billingInfo.PhoneNumber;
            }
            else
            {
                model.Country = CashManager.Instance.Countries.FirstOrDefault(a => a.Key == 2).Value;
            }

            return PartialView(model);
        }
        [HttpPost]
        public ActionResult Setep9(UploadViewModel.step9 model)
        {
            bool printAvable = (bool)Session["printAvable"];
            bool isforsale = (bool)Session["isOrginal"];
            float total = 7 + (isforsale ? 3 : 0) + (printAvable ? 1 : 0);
            float current = 9;

            if (string.IsNullOrEmpty(model.Street_Address) || string.IsNullOrEmpty(model.City)
                || string.IsNullOrEmpty(model.Country) || string.IsNullOrEmpty(model.Region) || string.IsNullOrEmpty(model.Zip_code) ||
                string.IsNullOrEmpty(model.phoneNumber))
            {
                ViewBag.error = Resources.UploadRes.Empty_Error;
                ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";
                return PartialView(model);
            }

            if (model.weight == 0)
                model.weight = float.Parse(Request["weight"].Replace(".", "/"));
            if (model.weight == 0)
            {
                ViewBag.error = Resources.UploadRes.Empty_Error;
                ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";
                return PartialView(model);
            }

            Session["weight"] = model.weight;
            Session["Street_Address"] = model.Street_Address;
            Session["Address_2"] = model.Address_2;
            Session["City"] = model.City;
            Session["Country"] = model.Country;
            Session["Region"] = model.Region;
            Session["Zip_code"] = model.Zip_code;
            Session["phoneNumber"] = model.phoneNumber;

            if (printAvable)
            {
                return RedirectToActionPermanent("Setep9_5");
            }
            else
                return RedirectToActionPermanent("Setep10");
        }

        //print type options
        public ActionResult Setep9_5()
        {
            bool printAvable = (bool)Session["printAvable"];
            bool isforsale = (bool)Session["isOrginal"];
            float total = 7 + (isforsale ? 3 : 0) + (printAvable ? 1 : 0);
            float current = isforsale ? 10 : 8;
            ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";

            ViewBag.printoptions = CashManager.Instance.PrintMaterial;
            return PartialView();
        }
        [HttpPost]
        public ActionResult Setep9_5(UploadViewModel.step9_5 model)
        {
            bool printAvable = (bool)Session["printAvable"];
            bool isforsale = (bool)Session["isOrginal"];
            if (isforsale)
            {
                return RedirectToActionPermanent("Setep10");
            }
            else
            {
                int id = 0;
                var error = uploadnow(out id);
                if (error == string.Empty)
                {
                    return RedirectToActionPermanent("Stepfinish", new { id = id });
                }
                else
                {
                    ViewBag.error = error;
                    float total = 7 + (isforsale ? 3 : 0) + (printAvable ? 1 : 0);
                    float current = isforsale ? 10 : 8;
                    ViewBag.progress = ((current / total) * 740f).ToString(CultureInfo.CreateSpecificCulture("en-US")) + "px";

                    return PartialView();
                }
            }
        }


        public ActionResult Setep10()
        {
            return PartialView();
        }
        [HttpPost]
        public ActionResult Setep10(UploadViewModel.step10 model)
        {
            Session["price"] = (int)model.Price;
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var profile = user.userDetail;

            int id = 0;
            var error = uploadnow(out id);
            if (error == string.Empty)
            {
                Session["imageAddress"] = "";
                if (!profile.isIDConfirmed && (bool)Session["gotoConfirmPage"])
                    return Json(new { result = "id" }, JsonRequestBehavior.AllowGet);
                else
                    return Json(new { result = "artworks" }, JsonRequestBehavior.AllowGet);


                //return RedirectToActionPermanent("Stepfinish", new { id = id });
            }
            else
            {
                ViewBag.error = error;
                return PartialView();
            }
        }

        public ActionResult Stepfinish(int id)
        {
            return PartialView();
        }

        private string uploadnow(out int id)
        {
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var profile = user.userDetail;
            if (!profile.isIDConfirmed && (bool)Session["isOrginal"])
            {
                Session["gotoConfirmPage"] = true;
                Session["isOrginal"] = false;
            }
            try
            {
                //if (profile.billingInfo != null)
                //{
                //    profile.billingInfo.Street = (string)Session["Street_Address"];
                //    profile.billingInfo.City = (string)Session["City"];
                //    //profile.billingInfo.CountryId = int.Parse((string)Session["Country"]);
                //    profile.billingInfo.Region = (string)Session["Region"];
                //    profile.billingInfo.ZipCode = (string)Session["Zip_code"];
                //    profile.billingInfo.PhoneNumber = (string)Session["phoneNumber"];
                //}
                //else
                //{
                //    profile.billingInfo = new BillingInfo();
                //    profile.billingInfo.Street = (string)Session["Street_Address"];
                //    profile.billingInfo.City = (string)Session["City"];
                //    //profile.billingInfo.CountryId = int.Parse((string)Session["Country"]);
                //    profile.billingInfo.Region = (string)Session["Region"];
                //    profile.billingInfo.ZipCode = (string)Session["Zip_code"];
                //    profile.billingInfo.PhoneNumber = (string)Session["phoneNumber"];
                //}

                var widepath = (string)Session["WideFullPath"];
                var sqpath = (string)Session["SqureFullPath"];
                var orginalpic = (string)Session["imageAddress"];
                var categoryId = (int)Session["category"];
                var subjectId = (int)Session["subject"];

                int img_width = (int)Session["img_width"];
                int img_height = (int)Session["img_height"];

                string Mediums = (string)Session["Mediums"];
                int[] Materials = (int[])Session["Materials"];
                string Styles = (string)Session["Styles"];
                string AgencyName = (string)Session["AgencyName"];
                var isAuction = (bool)Session["auction"];

                var Agency = db.UserProfiles.FirstOrDefault(x => (x.FirstName + " " + x.LastName) == AgencyName);
                string AgencyId = "";
                if (Agency != null)
                {
                    AgencyId = Agency.Id;
                }
                var medumsList = Mediums.Split(',');
                var stylelist = Styles.Split(',');

                var product = new Product()
                {
                    photo = new Photo() { Path = orginalpic, width = img_width, Height = img_height },
                    Widephoto = new Photo() { Path = widepath },
                    Sqphoto = new Photo() { Path = sqpath },
                    Title = (string)Session["Title"] == null ? "" : (string)Session["Title"],
                    Description = (string)Session["Description"] ?? "",
                    Price = (int)(Session["price"] ?? 0),
                    ISOrginalForSale = (bool)Session["isOrginal"],
                    AllEntity = (int)Session["AllEntity"],
                    //ArtCreatedYear = (int)Session["createYear"],
                    ArtCreatedYearString = (string)Session["createYear"],
                    avaible = (int)Session["avaible"],
                    Depth = (float)Session["Depth"],
                    Height = (float)Session["Height"],
                    Width = (float)Session["Width"],
                    IsPrintAvaibled = false,
                    Packaging = Session["Packaging"] == null ? Productpackage.box : (Productpackage)Session["Packaging"],
                    Keywords = (string)Session["Keywords"],
                    categoryId = categoryId,
                    subjectId = subjectId,
                    artist_id = AgencyId,
                    artistName = AgencyName,
                    user_id = userId,
                    TotalWeight = Session["weight"] == null ? 0 : (float)Session["weight"],
                    Status = ((bool)Session["isOrginal"]) ? ProductStatus.forSale : ProductStatus.NotForSale,
                    IsAuctionAvailable = isAuction
                };
                product.Materials = new List<Material>();
                product.Styles = new List<Style>();
                product.Mediums = new List<Medium>();

                foreach (var item in Materials)
                    product.Materials.Add(db.Materials.FirstOrDefault(x => item == x.Id));
                foreach (var item in stylelist)
                {
                    var temp = db.StyleTranslations.FirstOrDefault(x => x.Name == item);
                    if (temp != null)
                        product.Styles.Add(temp.style);
                }

                foreach (var item in medumsList)
                {
                    var temp = db.MediumTranslations.FirstOrDefault(x => x.Name == item);
                    if (temp != null)
                        product.Mediums.Add(temp.medium);
                }

                int frameTypeId;
                int frameColorId;
                int frameMaterialId;

                if (Session["framed"] != null && (bool)Session["framed"])
                {
                    frameTypeId = int.Parse((string)Session["frameType"]);
                    frameColorId = int.Parse((string)Session["frameColor"]);
                    frameMaterialId = int.Parse((string)Session["frameMaterial"]);
                    product.frameType = db.ProductFrameTypes.FirstOrDefault(x => x.Id == frameTypeId);
                    product.frameMaterial = db.ProductFrameMaterials.FirstOrDefault(x => x.Id == frameMaterialId);
                    product.frameColor = db.ProductFrameColors.FirstOrDefault(x => x.Id == frameColorId);
                }

                profile.Products.Add(product);

                db.SaveChanges();
                id = product.Id;
                return string.Empty;
            }

            catch (DbEntityValidationException e)
            {
                id = 0;
                string message = string.Empty;
                foreach (var eve in e.EntityValidationErrors)
                    foreach (var ve in eve.ValidationErrors)
                        message += Environment.NewLine + (ve.PropertyName + " " +
                        eve.Entry.CurrentValues.GetValue<object>(ve.PropertyName) + " " +
                        ve.ErrorMessage);
                db.logs.Add(new Log()
                {
                    Location = "upload now",
                    Message = "" + message
                });
                db.SaveChanges();
                return Resources.UploadRes.Image_cannot_be_empty;
            }
            catch (Exception ex)
            {
                id = 0;
                return Resources.UploadRes.Image_cannot_be_empty;
            }
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}