using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebRegApiCore.Models;

namespace WebRegApiCore
{
    public class RedirectingAction : IActionFilter

    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            throw new NotImplementedException();
        }

        public  void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string controllerName = filterContext.RouteData.Values["controller"].ToString();
            string actionName = filterContext.RouteData.Values["action"].ToString();

            //your other code here...

            AppConfigModel.apiFullName = controllerName.ToString().ToUpper() + "." + actionName.ToString().ToUpper();


            if (AppConfigModel.tokenType == 2 && AppConfigModel.apiFullName != "WEBREG.REISSUEACCESSTOKEN"
                && AppConfigModel.apiFullName != "AUTH.GETUSERS" && AppConfigModel.apiFullName != "AUTH.AUTHENTICATEUSER")
            {

                HttpResponseMessage response = new HttpResponseMessage();

                filterContext.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                filterContext.HttpContext.Response.WriteAsync("API call with Refreshh token is not allowed...");

            }

            //base.OnActionExecuting(filterContext);
        }

        private async Task InvokeAsync(HttpContext context)
        {
            var controllerName = context.GetRouteData().Values["controller"];
            var actionName = context.GetRouteData().Values["action"];

            AppConfigModel.apiFullName = controllerName.ToString().ToUpper() + "." + actionName.ToString().ToUpper();
        }


        //public override async Task OnActionExecutionAsync(ActionExecutingContext context,ActionExecutionDelegate next)
        //{
        //    try
        //    {
        //        string controllerName = context.RouteData.Values["controller"].ToString();
        //        string actionName = context.RouteData.Values["action"].ToString();

        //    }
        //    finally
        //    {
        //        await base.OnActionExecutionAsync(context, next); 
        //    }
        //}

    }
}
