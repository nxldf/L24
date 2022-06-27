﻿using Blog.Areas.Admin.Controllers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace Blog.Controllers
{
    public class TagController : BaseController
    {
        // GET: Tag
        public ActionResult Index(string id)
        {
            string currentCultureName = CultureInfo.CurrentCulture.Name.Substring(0, 2);
            int intId = int.Parse(GetIdValue(id).ToString());
            var tags = db.Tags.Find(intId);
            var posts = tags.Posts.Where(a => a.postType == Objects.PostType.Sqr && a.Translations.Any(x => x.languageId == currentCultureName && ( x.Description != null))).OrderByDescending(a => a.PostedOn).ToList();
            ViewBag.TagName = tags.Name;
            return View(posts);
        }
        private object GetIdValue(object id)
        {
            if (id != null)
            {
                string idValue = id.ToString();

                var regex = new Regex(@"^(?<id>\d+).*$");
                var match = regex.Match(idValue);

                if (match.Success)
                {
                    return match.Groups["id"].Value;
                }
            }

            return id;
        }
    }
}