using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace NihonUnisys.Olyzae
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // コントローラーをグループ分けして名前空間を設定しているため、
            // 名前空間ごとに探索ルートを登録する。

            // Accounts

            routes.MapRoute(
                name: "Accounts_Project",
                url: "{controller}/project/{projectId}/{action}/{id}",
                defaults: new { action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "NihonUnisys.Olyzae.Controllers.Accounts" }
            );

            routes.MapRoute(
                name: "Accounts",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "CompanyHome", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "NihonUnisys.Olyzae.Controllers.Accounts" }
            );

            // Participants

            routes.MapRoute(
                name: "Participants_Project",
                url: "{controller}/project/{projectId}/{action}/{id}",
                defaults: new { action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "NihonUnisys.Olyzae.Controllers.Participants" }
            );

            routes.MapRoute(
                name: "Participants",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "NihonUnisys.Olyzae.Controllers.Participants" }
            );

            // Administrators

            routes.MapRoute(
                name: "Administrators",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "AdministratorHome", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "NihonUnisys.Olyzae.Controllers.Administrators" }
            );

            // デフォルト

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Auth", action = "Login", id = UrlParameter.Optional }
            );
        }
    }
}
