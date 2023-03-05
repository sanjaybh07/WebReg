using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Http;
using WebRegApiCore.App_methods;
using WebRegApiCore.Models;

namespace WebRegApiCore
{
    public class AuthEventsHandler : JwtBearerEvents
    {
        private const string BearerPrefix = "Bearer";

        private AuthEventsHandler() => OnMessageReceived = MessageReceivedHandler;

        /// <summary>
        /// Gets single available instance of <see cref="AuthEventsHandler"/>
        /// </summary>
        public static AuthEventsHandler Instance { get; } = new AuthEventsHandler();

        //protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        //{
        //    string apikey = HttpUtility.ParseQueryString(request.RequestUri.Query).Get("apikey");
        //    if (string.IsNullOrWhiteSpace(apikey))
        //    {
        //        HttpResponseMessage response = request.CreateErrorResponse(HttpStatusCode.Forbidden, "You can't use the API without the key.");
        //        throw new HttpResponseException(response);
        //    }
        //    else
        //    {
        //        return base.SendAsync(request, cancellationToken);
        //    }
        //}

        //public static HttpResponseException CreateErrorResponseException(this ApiController controller, HttpStatusCode statusCode, string message)
        //{
        //    ErrorResponse error = new ErrorResponse()
        //    {
        //        StatusCode = (int)statusCode + ": " + statusCode.ToString(),
        //        Message = message
        //    };
        //    HttpResponseMessage responseMessage = controller.Request.CreateResponse(statusCode, error, controller.Request.GetConfiguration());
        //    return new HttpResponseException(responseMessage);
        //}'

        

        private Task MessageReceivedHandler(MessageReceivedContext context)
        {
            AppConfigModel.tokenType = 0;

            if (context.Request.Headers.TryGetValue("Authorization", out StringValues headerValue))
            {
                string token = headerValue;

                var bearerToken = context.Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");

                if (!string.IsNullOrEmpty(token))
                {
                    
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadToken(bearerToken);
                    var jwtSecurityToken = jsonToken as JwtSecurityToken;


                    //AppConfigModel.issueTime = jwtSecurityToken.IssuedAt;
                    AppConfigModel.validToTime =jwtSecurityToken.ValidTo.ToString();    

                    var claims = jwtSecurityToken.Claims.ToList();

                    int nClaims = claims.Count();

                    string [,] TokenInfo = new string[nClaims, 2];
                    int n = 0;
                    foreach (var claim in claims)
                    {
                        if (claim.Type.ToUpper() == "ROLECODE")
                            AppConfigModel.roleCode = claim.Value;
                        else
                        if (claim.Type.ToUpper() == "USERID")
                                AppConfigModel.userId= claim.Value;
                        else
                        if (claim.Type.ToUpper() == "TOKENTYPE")
                            AppConfigModel.tokenType   = Convert.ToInt32(claim.Value);
                    }
                }
                                               

                context.Token = bearerToken;
            }

            return Task.CompletedTask;
        }
    }
}
