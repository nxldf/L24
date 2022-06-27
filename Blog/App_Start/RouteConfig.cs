﻿using Blog.Extentions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Blog
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            //routes.MapMvcAttributeRoutes();

            routes.Add("MoreCategory", new SeoFriendlyRoute("{culture}/Category/More/{id}",
      new RouteValueDictionary(new { controller = "Category", action = "More", culture = string.Empty, id = UrlParameter.Optional }),
      new MvcRouteHandler()));

            routes.Add("SubCategory", new SeoFriendlyRoute("{culture}/Category/{Index}/{id}",
          new RouteValueDictionary(new { controller = "Category", action = "SubCategory", culture = string.Empty, Index = UrlParameter.Optional }),
          new MvcRouteHandler()));

            routes.Add("Category", new SeoFriendlyRoute("{culture}/Category/{id}",
          new RouteValueDictionary(new { controller = "Category", action = "Index", culture = string.Empty, Index = UrlParameter.Optional}),
          new MvcRouteHandler()));

            routes.MapRoute(
              "item_details",
              "{culture}/search/{id}",
              new { controller = "Search", action = "Index", culture = string.Empty }
        );

            routes.MapRoute(
          "tag_details",
          "{culture}/Tag/{id}",
          new { controller = "Tag", action = "Index", culture = string.Empty }
           );

            routes.Add("PostDetailsModified", new SeoFriendlyRoute("post/{Index}/{id}",
        new RouteValueDictionary(new { controller = "post", action = "Index", Index = UrlParameter.Optional }),
        new MvcRouteHandler()));

            routes.Add("PostDetails", new SeoFriendlyRoute("{culture}/post/{Index}/{id}",
                new RouteValueDictionary(new { controller = "post", action = "Index", culture = CultureHelper.GetCurrentCulture(), Index = UrlParameter.Optional }),
                new MvcRouteHandler()));


            routes.MapRoute(
                name: "Default",
                url: "{culture}/{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", culture = string.Empty, id = UrlParameter.Optional },
              namespaces: new[] { "Blog.Controllers" }
            );

            //routes.MapRoute(
            //           name: "DefaultBase",
            //           url: "{controller}/{action}/{id}",
            //           defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
            //         namespaces: new[] { "Blog.Controllers" }
            //       );

        }
    }
}
