﻿using ArtShop.Models;
using ArtShop.Util;
using DataLayer;
using DataLayer.Enitities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DataLayer.Extentions;
using Microsoft.AspNet.Identity;
using Postal;
using paypal = PayPal.Api;
using ArtShop.Helper;
using System.Data.Entity;

namespace ArtShop.Controllers
{
    public class CardController : BaseController
    {
        //checkout and get info
        [Route("{culture}/checkout")]
        public ActionResult Index()
        {
            var cart = CartManager.GetCart(this.HttpContext);
            var viewModel = new ShoppingCartViewModel
            {
                CartItems = cart.GetCartItems(),
                CartTotal = cart.GetTotal()
            };
            return View(viewModel);
        }

        [Authorize]
        public ActionResult getinfo(int c = 0)
        {
            var user = db.UserProfiles.Find(User.Identity.GetUserId());
            ViewBag.name = user.FirstName;
            ViewBag.lastname = user.LastName;
            if (user.billingInfo != null)
            {
                ViewBag.Street = user.billingInfo.Street == "" ? user.billingInfo.Street + " " + user.billingInfo.Unit : "";
                ViewBag.City = user.billingInfo.City;
                ViewBag.Country = user.billingInfo.CountryId;
                ViewBag.Region = user.billingInfo.Region;
                ViewBag.ZipCode = user.billingInfo.ZipCode;
                ViewBag.PhoneNumber = user.billingInfo.PhoneNumber;
            }
            if (c != 0)
            {
                ViewBag.Country = c;
            }
            var cart = CartManager.GetCart(this.HttpContext);
            var viewModel = new ShoppingCartViewModel
            {
                CartItems = cart.GetCartItems(),
                CartTotal = cart.GetTotal()
            };
            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        public ActionResult getinfo(checkInfoViewModel model)
        {
            var cart = CartManager.GetCart(this.HttpContext);
            var cartItems = cart.GetCartItems();
            var allProductId = cartItems.ToDictionary(x => x.ProductId, y => y.Quantity);
            foreach (var item in allProductId)
            {
                var p = db.Products.Find(item.Key);
                var avaible = p.avaible;
                var reserve = p.reservedlist.Where(x => (x.datetime.AddMinutes(15)) > DateTime.Now).Sum(x => x.quantity);
                if (item.Value > avaible)
                {
                    ModelState.AddModelError("", "product has been sold");
                    return View();
                }
                p.reservedlist.Add(new ReserveList((int)item.Value));
            }
            db.SaveChanges();
            Session["cvm"] = model;
            return RedirectPermanent("/" + CultureHelper.GetCurrentCulture() + "/card/pay");
        }

        //payment segment
        [HttpGet]
        [Authorize]
        public ActionResult Pay()
        {
            var model = (checkInfoViewModel)Session["cvm"];
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var profile = user.userDetail;
            var cart = CartManager.GetCart(this.HttpContext);
            var cartItems = cart.GetCartItems();
            var setting = db.SettingValues.FirstOrDefault();
            decimal orderTotal = 0;
            Order o = new Order()
            {
                user_id = userId,
                OrderDetails = new List<OrderDetail>(),
                ReceiverName = model.firstname + " " + model.lastname,
                Address = model.address,
                CountryCode = model.country,
                City = model.city,
                PhoneNumber = model.PhoneNumber,
            };
            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    ProductId = item.ProductId,
                    UnitPrice = item.Product.Price,
                    Quantity = item.Quantity,
                    type = item.type
                };
                orderTotal += (item.Quantity * item.Product.Price);
                o.OrderDetails.Add(orderDetail);
            }
            o.TotalPrice = (double)orderTotal;
            o.TransactionDetail = new TransactionDetail()
            {
                amount = orderTotal,
                Method = model.paymentMethod,
                currencyRate = setting.IRRialRate
            };
            db.Orders.Add(o);
            db.SaveChanges();

            if (model.paymentMethod == PaymentMethod.zarinpall)
            {
                System.Net.ServicePointManager.Expect100Continue = false;
                ZPServiceReference.PaymentGatewayImplementationServicePortTypeClient zp = new ZPServiceReference.PaymentGatewayImplementationServicePortTypeClient();
                string Authority;
                int Status = zp.PaymentRequest("d0e193da-75d0-11e7-b744-000c295eb8fc", (int)(orderTotal * setting.IRRialRate), profile.FirstName + " " + profile.LastName, user.Email, user.PhoneNumber, "http://cuber.dev/" + CultureHelper.GetCurrentCulture() + "/card/Verify", out Authority);
                long longAuth = 0;
                long.TryParse(Authority, out longAuth);
                o.TransactionDetail.Number = longAuth.ToString();
                db.SaveChanges();
                if (Status == 100)
                {
                    Response.RedirectPermanent("https://zarinpal.com/pg/StartPay/" + Authority);
                    return View();
                }
                else
                {
                    return Content("error: " + Status);
                }
            }
            else
            {
                try
                {
                    var payment = CreatePayment(cartItems, orderTotal, o.Id.ToString());
                    o.TransactionDetail.Number = payment.id;
                    db.SaveChanges();
                    var approveurl = payment.links.FirstOrDefault(x => x.rel.Equals("approval_url", StringComparison.OrdinalIgnoreCase));
                    return RedirectPermanent(approveurl.href);
                }
                catch (Exception ex)
                {
                    return Content(ex.ToString());
                }
            }
        }

        public ActionResult verify()
        {
            if (Request.QueryString["Status"] != "" && Request.QueryString["Status"] != null && Request.QueryString["Authority"] != "" && Request.QueryString["Authority"] != null)
            {
                long longAuth = 0;
                long.TryParse(Request.QueryString["Authority"], out longAuth);
                var tran = db.TransactionDetails.FirstOrDefault(x => x.Number == longAuth.ToString());
                var order = db.Orders.FirstOrDefault(x => x.TransactionDetailId == tran.Id);
                var orderId = order.Id;
                if (tran != null)
                {
                    if (Request.QueryString["Status"].ToString().Equals("OK"))
                    {
                        decimal Amount = tran.amount * tran.currencyRate;
                        long RefID;
                        System.Net.ServicePointManager.Expect100Continue = false;
                        ZPServiceReference.PaymentGatewayImplementationServicePortTypeClient zp = new ZPServiceReference.PaymentGatewayImplementationServicePortTypeClient();
                        int Status = zp.PaymentVerification("e21e6fde-e5f6-11e6-bdac-000c295eb8fc", Request.QueryString["Authority"].ToString(), (int)Amount, out RefID);
                        tran.TransactionNumber = RefID.ToString();
                        tran.Description = Status.ToString();
                        if (Status == 100)
                        {
                            tran.Payed = true;
                            foreach (var item in order.OrderDetails)
                            {
                                item.Product.user.Account += (item.UnitPrice * item.Quantity) * (decimal)((100d - 10d) / 100d);
                                item.Product.avaible--;
                                if (item.Product.avaible == 0)
                                {
                                    item.Product.Status = ProductStatus.Sold;
                                }
                            }
                            db.SaveChanges();
                            SendOrderDetail(order);
                            SendInvoice(order);
                            CartManager.GetCart(this.HttpContext).EmptyCart();
                            return RedirectToActionPermanent("paymentReport", new { culture = CultureHelper.GetCurrentCulture(), id = orderId });
                        }
                        else
                        {
                            db.SaveChanges();
                            return RedirectToActionPermanent("paymentReport", new { culture = CultureHelper.GetCurrentCulture(), id = orderId });
                        }
                    }
                    else
                    {
                        tran.Description = Request.QueryString["Status"].ToString();
                        db.SaveChanges();
                        return RedirectToActionPermanent("paymentReport", new { culture = CultureHelper.GetCurrentCulture(), id = orderId });
                    }
                }
                else
                {
                    return Content("not valid address");
                }
            }
            else
            {
                return Content("not valid address");
            }
        }

        //paypal segment
        private paypal.APIContext getPaypalApiContect()
        {
            var config = paypal.ConfigManager.Instance.GetProperties();
            var accessToken = new paypal.OAuthTokenCredential(config).GetAccessToken();
            var apiContext = new paypal.APIContext(accessToken);
            return apiContext;
        }

        private paypal.Payment CreatePayment(List<ShoppingCart> cartItems, decimal orderTotal, string invoice_number)
        {
            var apiContext = getPaypalApiContect();
            var items = cartItems.Select(x => new paypal.Item()
            {
                name = x.Product.Title,
                currency = "USD",
                price = ((int)x.Product.Price).ToString(),
                quantity = ((int)x.Quantity).ToString(),
                sku = "sku"
            }).ToList();
            var transactionList = new List<paypal.Transaction>();
            var amount = new paypal.Amount
            {
                currency = "USD",
                total = orderTotal.ToString(),
                details = new paypal.Details
                {
                    tax = "0",
                    shipping = "0",
                    subtotal = orderTotal.ToString()
                }
            };
            transactionList.Add(new paypal.Transaction()
            {
                description = "R24 shopping store",
                invoice_number = invoice_number,
                amount = amount,
                item_list = new paypal.ItemList { items = items }
            });
            var payer = new paypal.Payer() { payment_method = "paypal" };
            return paypal.Payment.Create(apiContext, new paypal.Payment
            {
                intent = "sale",
                payer = payer,
                transactions = transactionList,
                redirect_urls = new paypal.RedirectUrls
                {
                    return_url = "https://cuber.dev/" + CultureHelper.GetCurrentCulture() + "/card/PaypalReturn",
                    cancel_url = "https://cuber.dev/" + CultureHelper.GetCurrentCulture() + "/card/PaypalCancel"
                }
            });
        }

        public ActionResult PaypalReturn(string payerId, string paymentId)
        {
            var tran = db.TransactionDetails.FirstOrDefault(x => x.Number == paymentId);
            var order = db.Orders.FirstOrDefault(x => x.TransactionDetailId == tran.Id);
            var orderId = order.Id;
            var apiContext = getPaypalApiContect();
            var paymentExecution = new paypal.PaymentExecution() { payer_id = payerId };
            var payment = new paypal.Payment() { id = paymentId };
            var executedpayment = payment.Execute(apiContext, paymentExecution);
            if (executedpayment.state.ToLower() != "approved")
            {
                return RedirectToActionPermanent("paymentReport", new { culture = CultureHelper.GetCurrentCulture(), id = orderId });
            }
            tran.Payed = true;
            tran.Description = paymentId;
            foreach (var item in order.OrderDetails)
            {
                item.Product.user.Account += (item.UnitPrice * item.Quantity) * (decimal)((100d - 10d) / 100d);
                item.Product.avaible--;
                if (item.Product.avaible == 0)
                {
                    item.Product.Status = ProductStatus.Sold;
                }
            }
            db.SaveChanges();
            SendOrderDetail(order);
            SendInvoice(order);
            CartManager.GetCart(this.HttpContext).EmptyCart();
            return RedirectToActionPermanent("paymentReport", new { culture = CultureHelper.GetCurrentCulture(), id = orderId });
        }

        public ActionResult PaypalCancel(string payerId, string paymentId)
        {
            var tran = db.TransactionDetails.FirstOrDefault(x => x.Number == paymentId);
            var order = db.Orders.FirstOrDefault(x => x.TransactionDetailId == tran.Id);
            var orderId = order.Id;
            return RedirectToActionPermanent("paymentReport", new { culture = CultureHelper.GetCurrentCulture(), id = orderId });
        }


        //finish
        public ActionResult paymentReport(int id)
        {
            var order = db.Orders.SingleOrDefault(x => x.Id == id);
            return View(order);
        }


        //card segment
        [OutputCache(VaryByParam = "*", Duration = 0, NoStore = true)]
        public ActionResult AddToCart(int id, Ordertype type)
        {
            var addedAlbum = db.Products.Find(id);
            var cart = CartManager.GetCart(this.HttpContext);
            var basket = cart.GetCountProduct(id);
            if (basket + 1 > addedAlbum.avaible)
                return RedirectPermanent("/" + CultureHelper.GetCurrentCulture() + "/checkout");
            cart.AddToCart(addedAlbum, type);
            return RedirectPermanent("/" + CultureHelper.GetCurrentCulture() + "/checkout");
        }

        [HttpPost]
        public ActionResult RemoveFromCart(int id)
        {
            // Remove the item from the cart
            var cart = CartManager.GetCart(this.HttpContext);

            // Get the name of the album to display confirmation
            string Pname = db.ShoppingCarts
                .Single(item => item.Id == id).Product.Title;

            // Remove from cart
            decimal itemCount = cart.RemoveAllFromCart(id);

            // Display the confirmation message
            var results = new ShoppingCartRemoveViewModel
            {
                Message = Server.HtmlEncode(Pname) +
                    " با موفقیت از سبد خرید حذف شد ",
                CartTotal = cart.GetTotal(),
                CartCount = cart.GetCount(),
                ItemCount = itemCount,
                DeleteId = id
            };
            return Json(results);
        }

        [HttpPost]
        public ActionResult changecnt(int id, int cnt)
        {
            // Remove the item from the cart
            var cart = CartManager.GetCart(this.HttpContext);

            // Get the name of the album to display confirmation
            string Pname = db.ShoppingCarts
                .Single(item => item.Id == id).Product.Current().Title;

            // Remove from cart
            decimal itemCount = cart.newcnt(id, cnt);

            // Display the confirmation message
            var results = new ShoppingCartRemoveViewModel
            {
                Message = Server.HtmlEncode(Pname) +
                    " تغییر کرد ",
                CartTotal = cart.GetTotal(),
                CartCount = cart.GetCount(),
                ItemCount = itemCount,
                DeleteId = id
            };
            return Json(results);
        }

        [ChildActionOnly]
        public ActionResult CartSummary()
        {
            var cart = CartManager.GetCart(this.HttpContext);

            ViewData["CartCount"] = cart.GetCount();
            return PartialView("CartSummary");
        }

        //for mobile application
        public ActionResult Mobileverify()
        {
            if (Request.QueryString["Status"] != "" && Request.QueryString["Status"] != null && Request.QueryString["Authority"] != "" && Request.QueryString["Authority"] != null)
            {
                long longAuth = 0;
                long.TryParse(Request.QueryString["Authority"], out longAuth);
                var tran = db.TransactionDetails.FirstOrDefault(x => x.Number == longAuth.ToString());
                var order = db.Orders.FirstOrDefault(x => x.TransactionDetailId == tran.Id);
                var orderId = order.Id;
                if (tran != null)
                {
                    if (Request.QueryString["Status"].ToString().Equals("OK"))
                    {
                        decimal Amount = tran.amount * tran.currencyRate;
                        long RefID;
                        System.Net.ServicePointManager.Expect100Continue = false;
                        ZPServiceReference.PaymentGatewayImplementationServicePortTypeClient zp = new ZPServiceReference.PaymentGatewayImplementationServicePortTypeClient();
                        int Status = zp.PaymentVerification("e21e6fde-e5f6-11e6-bdac-000c295eb8fc", Request.QueryString["Authority"].ToString(), (int)Amount, out RefID);
                        tran.TransactionNumber = RefID.ToString();
                        tran.Description = Status.ToString();
                        if (Status == 100)
                        {
                            tran.Payed = true;
                            foreach (var item in order.OrderDetails)
                            {
                                item.Product.user.Account += (item.UnitPrice * item.Quantity) * (decimal)((100d - 10d) / 100d);
                                item.Product.avaible--;
                                if (item.Product.avaible == 0)
                                {
                                    item.Product.Status = ProductStatus.Sold;
                                }
                            }
                            db.SaveChanges();
                            SendOrderDetail(order);
                            SendInvoice(order);
                            CartManager.GetCart(order.user_id).EmptyCart();
                            return RedirectToActionPermanent("MobilePaymentReport", new { culture = CultureHelper.GetCurrentCulture(), id = orderId });
                        }
                        else
                        {
                            db.SaveChanges();
                            return RedirectToActionPermanent("MobilePaymentReport", new { culture = CultureHelper.GetCurrentCulture(), id = orderId });
                        }
                    }
                    else
                    {
                        tran.Description = Request.QueryString["Status"].ToString();
                        db.SaveChanges();
                        return RedirectToActionPermanent("MobilePaymentReport", new { culture = CultureHelper.GetCurrentCulture(), id = orderId });
                    }
                }
                else
                {
                    return Content("not valid address");
                }
            }
            else
            {
                return Content("not valid address");
            }
        }
        public ActionResult MobilePaypalReturn(string payerId, string paymentId)
        {
            var tran = db.TransactionDetails.FirstOrDefault(x => x.Number == paymentId);
            var order = db.Orders.FirstOrDefault(x => x.TransactionDetailId == tran.Id);
            var orderId = order.Id;
            var apiContext = getPaypalApiContect();
            var paymentExecution = new paypal.PaymentExecution() { payer_id = payerId };
            var payment = new paypal.Payment() { id = paymentId };
            var executedpayment = payment.Execute(apiContext, paymentExecution);
            if (executedpayment.state.ToLower() != "approved")
            {
                return RedirectToActionPermanent("MobilePaymentReport", new { culture = CultureHelper.GetCurrentCulture(), id = orderId });
            }
            tran.Payed = true;
            tran.Description = paymentId;
            foreach (var item in order.OrderDetails)
            {
                item.Product.user.Account += (item.UnitPrice * item.Quantity) * (decimal)((100d - 10d) / 100d);
                item.Product.avaible--;
                if (item.Product.avaible == 0)
                {
                    item.Product.Status = ProductStatus.Sold;
                }
            }
            db.SaveChanges();
            SendOrderDetail(order);
            SendInvoice(order);
            CartManager.GetCart(this.HttpContext).EmptyCart();
            return RedirectToActionPermanent("MobilePaymentReport", new { culture = CultureHelper.GetCurrentCulture(), id = orderId });
        }
        public ActionResult MobilePaypalCancel(string payerId, string paymentId)
        {
            var tran = db.TransactionDetails.FirstOrDefault(x => x.Number == paymentId);
            var order = db.Orders.FirstOrDefault(x => x.TransactionDetailId == tran.Id);
            var orderId = order.Id;
            return RedirectToActionPermanent("MobilePaymentReport", new { culture = CultureHelper.GetCurrentCulture(), id = orderId });
        }
        public ActionResult MobilePaymentReport(int id)
        {
            //R24://payment/{status}/{orderid}
            var order = db.Orders.SingleOrDefault(x => x.Id == id);
            return Redirect("R24://payment/" + order.TransactionDetail.Payed + "/" + id);
        }

        //email segment
        private void SendInvoice(Order order)
        {
            dynamic email = new Email("Invoice");
            email.To = order.user.ApplicationUserDetail.UserName;
            email.Subject = "R24 Invoice | " + order.Id.ToString();
            email.orderid = order.Id;
            email.fullname = order.ReceiverName;
            email.address = order.Address;
            email.shipaddress = order.Address;
            email.orderdate = order.BuyDate.ToString();
            email.products = order.OrderDetails.ToList();
            email.subtotal = order.TotalPrice;
            email.total = order.TotalPrice;
            email.Send();
        }
        private void SendOrderDetail(Order order)
        {
            foreach (var item in order.OrderDetails.GroupBy(a => a.Product.user))
            {
                var user = item.Key;
                dynamic email = new Email("Order");
                email.To = user.ApplicationUserDetail.Email;
                email.Subject = "Order" + order.Id.ToString();
                email.orderid = order.Id;
                email.ReceiverName = order.ReceiverName;
                email.orderdate = order.BuyDate.ToString();
                email.products = item.Select(x => new productemailviewmodel()
                {
                    title = x.Product.Title,
                    photo = x.Product.Sqphoto.Path,
                    quantity = x.Quantity,
                    unitPrice = x.UnitPrice,
                    package = x.Product.Packaging
                }).ToList();

                email.subtotal = order.TotalPrice;
                email.total = order.TotalPrice;
                email.Send();
            }
        }

    }
}