﻿using DataLayer;
using DataLayer.Enitities;
using MobileApi.Models;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using Microsoft.AspNet.Identity;

namespace MobileApi.Controllers
{
    [EnableCors("*", "*", "GET,POST")]
    [RoutePrefix("api/Card")]
    [Authorize]
    public class CardController : ApiController
    {
        ApplicationDbContext db = new ApplicationDbContext();
        internal JsonMediaTypeFormatter formatter = MJsonMaker();
        private static Func<JsonMediaTypeFormatter> MJsonMaker = () =>
        {
            var formatter = new JsonMediaTypeFormatter();
            var json = formatter.SerializerSettings;
            json.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;
            json.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
            json.NullValueHandling = Newtonsoft.Json.NullValueHandling.Include;
            json.Formatting = Newtonsoft.Json.Formatting.Indented;
            json.ContractResolver = new CamelCasePropertyNamesContractResolver();
            json.Culture = new CultureInfo("it-IT");
            return formatter;
        };

        [HttpGet, Route("GetCard")]
        public HttpResponseMessage GetCard()
        {
            var cart = CartManager.GetCart(User.Identity.GetUserId());
            var viewModel = new
            {
                CartItems = cart.GetCartItems().Select(x => new { x.type, x.Quantity, product = x.Product.tojason() }),
                CartTotal = cart.GetTotal()
            };
            return Request.CreateResponse(HttpStatusCode.OK, viewModel, formatter);
        }

        [HttpPost, Route("RemoveFromCart")]
        public HttpResponseMessage RemoveFromCart(int id)
        {
            var cart = CartManager.GetCart(User.Identity.GetUserId());
            decimal itemCount = cart.RemoveAllFromCart(id);
            var results = new
            {
                Message = " با موفقیت از سبد خرید حذف شد ",
                CartTotal = cart.GetTotal(),
                CartCount = cart.GetCount(),
                ItemCount = itemCount,
                DeleteId = id
            };
            return Request.CreateResponse(HttpStatusCode.OK, results, formatter);
        }

        [HttpPost, Route("ChangeQuantity")]
        public HttpResponseMessage ChangeQuantity(int id, int cnt)
        {
            var cart = CartManager.GetCart(User.Identity.GetUserId());
            string Pname = db.ShoppingCarts.Single(item => item.Id == id).Product.Title;
            decimal itemCount = cart.newcnt(id, cnt);
            var results = new
            {
                Message = " تغییر کرد ",
                CartTotal = cart.GetTotal(),
                CartCount = cart.GetCount(),
                ItemCount = itemCount,
                DeleteId = id
            };
            return Request.CreateResponse(HttpStatusCode.OK, results, formatter);
        }

        [HttpGet, Route("AddToCart")]
        public HttpResponseMessage AddToCart(int id, Ordertype type)
        {
            var addedAlbum = db.Products.Find(id);
            var cart = CartManager.GetCart(User.Identity.GetUserId());
            bool isExist = cart.GetCartItems().Any(a => a.ProductId == id);
            if (!isExist)
            {
                cart.AddToCart(addedAlbum, type);

                return Request.CreateResponse(HttpStatusCode.OK, new { success = true, message = "Added" }, formatter);
            }
            else
                return Request.CreateResponse(HttpStatusCode.OK, new { success = false, message = "item is exist" }, formatter);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
