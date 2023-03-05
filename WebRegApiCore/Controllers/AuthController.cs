using WebRegApiCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using IdentityServer4.Models;
using TasksApi.Helpers;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Runtime.ConstrainedExecution;
using WebRegApiCore.App_methods;
//using IdentityServer4.Models;

namespace AspNetCoreApp.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public IConfiguration _configuration;
        String connStr;
        SqlConnection conn;


        public AuthController(IConfiguration config)
        {
            _configuration = config;
            connStr = _configuration["ConnectionStrings:CON_REG"];
            conn = new SqlConnection(connStr);
        }


        [HttpGet]
        [Route("~/~/Users")]
        public object Getusers()
        {
            String cErr = "";

            commonMethods globalMethods= new commonMethods();

            String cGetConStr = globalMethods.GetSqlConnection(connStr, ref cErr);

            dynamic result = new ExpandoObject();

           

            userMethods userMethod = new userMethods();


            result = userMethod.GetUsersList(cGetConStr);

            return this.Ok(result);

        }


        [HttpPost]
        [Route("~/validateUser")]
        public async Task<IActionResult> AuthenticateUser(User _userData)
        {
            if (_userData != null && _userData.userCode != null && _userData.passwd != null)
            {
                User user = GetUser(_userData.userCode, _userData.passwd);

                if (user != null && user.roleCode != null)
                {
            
                    _userData.roleCode = user.roleCode;

                    var accessTokenData = tokenHelper.GenerateToken(_configuration,_userData,1);

                    var refreshTokenData = tokenHelper.GenerateToken(_configuration, _userData,2);

                    dynamic tokenObj = new ExpandoObject();

                    tokenObj.accessToken = new JwtSecurityTokenHandler().WriteToken(accessTokenData);
                    tokenObj.refreshToken = new JwtSecurityTokenHandler().WriteToken(refreshTokenData);

                    string cMessage = updateRefreshTokenValidity(_userData.userCode);

                    if (String.IsNullOrEmpty(cMessage))
                        return Ok(tokenObj);
                    else
                        return BadRequest(cMessage);

                    //return Ok(new JwtSecurityTokenHandler().WriteToken(token));
                }
                else
                {
                    return BadRequest("Invalid credentials");
                }
            }
            else
            {
                return BadRequest();
            }
        }


        protected string updateRefreshTokenValidity(string userId)
        {


            try
            {
                string cExpr = $"update rw_users SET refreshTokenValidity='{AppConfigModel.refreshTokenValidity}'" +
                $" where userCode='{userId}'";

                SqlCommand cmd = new SqlCommand(cExpr, conn);
            
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
        }

        [NonAction]
        //private async Task<UserInfo> GetUser(string userCode, string password)
        protected User GetUser(string userCode, string password)
        {
            User userInfo = new User();

            string cExpr = $"Select top 1 userCode,roleCode" +
                $" from rw_users (nolock) where userCode='{userCode}'" +
                $" and passwd='{password}'";

            SqlCommand cmd = new SqlCommand(cExpr, conn);
            DataTable dtExists = new DataTable();
            SqlDataAdapter sda = new SqlDataAdapter(cmd);

            sda.Fill(dtExists);

            if (dtExists.Rows.Count > 0)
            {
                userInfo.userCode = dtExists.Rows[0]["userCode"].ToString();
                userInfo.roleCode = dtExists.Rows[0]["roleCode"].ToString();
            }

            //return await userInfo.FirstOrDefaultAsync(u => u.userId == userCode && u.Password == password);

            return userInfo;
        }

    }
}
