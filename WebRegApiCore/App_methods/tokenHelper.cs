using IdentityServer4.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using WebRegApiCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Dynamic;

namespace TasksApi.Helpers
{
    public class tokenHelper
    {

        public dynamic validateRefreshToken(string connStr)
        {
            
            dynamic result = new ExpandoObject();
            result.Message = "";
            result.tokenExpired = false;
            try
            { 
                string cExpr = $"select refreshTokenValidity validity from rw_users (NOLOCK) WHERE userCode='{AppConfigModel.userId}'";
                SqlConnection conn = new SqlConnection(connStr);

                SqlCommand cmd = new SqlCommand(cExpr, conn);
                DataTable dt = new DataTable();
                SqlDataAdapter sda = new SqlDataAdapter(cmd);

                sda.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    DateTime dtValidity = Convert.ToDateTime(dt.Rows[0]["validity"]);
                    if (dtValidity < DateTime.Now)
                    {
                        result.Message = $"Refresh Token Expired..";
                        result.tokenExpired = true;
                    }
                }
            }

            catch (Exception ex)
            {
                result.Message = ex.Message.ToString();
            }

            return result;
        }


        public static SecurityToken GenerateToken(IConfiguration config, User user,int tokenMode)
        {

            //// Need to do this as Jwt token by default considers UTC time and we need to get the local IST 
            //var date = DateTime.Now.ToString();
            //DateTime convertedDate = DateTime.SpecifyKind(
            //    DateTime.Parse(date),
            //    DateTimeKind.Utc);
            //DateTime dt = convertedDate.ToLocalTime();

            //create claims details based on the user information
            var claims = new[] {
                        new Claim(JwtRegisteredClaimNames.Sub, config["Jwt:Subject"]),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToString()),
                        new Claim("userId", user.userCode.ToString()),
                        new Claim("roleCode", user.roleCode.ToString()),
                        new Claim("tokenType",tokenMode.ToString())
                    };

                var key = new
                    SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));
                var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            if (tokenMode == 2)
                AppConfigModel.refreshTokenValidity = DateTime.Now.AddMinutes(5); // DateTime.Now.AddDays(30);

            var token = new JwtSecurityToken(
                config["Jwt:Issuer"],
                config["Jwt:Audience"],
                claims,
                expires: (tokenMode==1? DateTime.Now.AddMinutes(2):AppConfigModel.refreshTokenValidity),
                signingCredentials: signIn);


            return token;

         }

    public static byte[] GetSecureSalt()


        {


            // Starting .NET 6, the Class RNGCryptoServiceProvider is obsolete,


            // so now we have to use the RandomNumberGenerator Class to generate a secure random number bytes
            return RandomNumberGenerator.GetBytes(32);


        }


        public static string HashUsingPbkdf2(string password, byte[] salt)


        {


            byte[] derivedKey = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, iterationCount: 300000, 32);
            
            return Convert.ToBase64String(derivedKey);


        }

    }
}