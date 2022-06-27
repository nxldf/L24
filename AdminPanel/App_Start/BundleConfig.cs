﻿using System.Web;
using System.Web.Optimization;

namespace AdminPanel
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            bundles.Add(new ScriptBundle("~/bundles/angular").Include(
             "~/Scripts/angular.min.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css"));

            bundles.Add(new StyleBundle("~/Content/default").Include(
                          "~/Content/Admin/styles/webfont.css",
                          "~/Content/Admin/styles/climacons-font.css",
                          "~/Content/Admin/vendor/bootstrap/dist/css/bootstrap.css",
                          "~/Content/Admin/vendor/checkbo/src/0.1.4/css/checkBo.min.css",
                          "~/Content/image-picker.css",
                          "~/Content/Admin/styles/font-awesome.css",
                          "~/Content/Admin/styles/card.css",
                          "~/Content/Admin/styles/sli.css",
                          "~/Content/Admin/styles/animate.css",
                          "~/Content/Admin/styles/app.css",
                          "~/Content/Admin/styles/app.skins.css"
                          ));
        }
    }
}
