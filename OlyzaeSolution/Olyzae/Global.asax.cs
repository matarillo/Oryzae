using NihonUnisys.Olyzae.Framework;
using System;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace NihonUnisys.Olyzae
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Database.SetInitializer<Models.Entities>(new Models.EntitiesInitializer()); 
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            ExecutionContext.Create = CreateExecutionContext;
        }

        protected void Application_PostAuthenticateRequest()
        {
            var ec = ExecutionContext.Create();
            ec.SetCurrentUserFromIdentity();

            if (HttpContext.Current.User.Identity.IsAuthenticated)
            {
                var role = ec.CurrentUser.Role;
                var principal = new GenericPrincipal(User.Identity, new[] { role });
                HttpContext.Current.User = principal;
                System.Threading.Thread.CurrentPrincipal = principal;
            }
        }

        protected void Application_PreRequestHandlerExecute()
        {
            ExecutionContext.Create().PreRequestHandlerExecute();
        }

        protected void Application_PostRequestHandlerExecute()
        {
            ExecutionContext.Create().PostRequestHandlerExecute();
        }

        private static ExecutionContext CreateExecutionContext()
        {
            return new ExecutionContext(new HttpContextWrapper(HttpContext.Current));
        }
    }
}
