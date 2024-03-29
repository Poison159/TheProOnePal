﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ProOnePal
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Teams", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
               name: "Fixtures",
               url: "{controller}/{action}/{id}",
               defaults: new { controller = "Fixtures", action = "Index" }
           );
        }
    }
}
