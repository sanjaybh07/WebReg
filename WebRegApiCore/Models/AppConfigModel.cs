using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRegApiCore.Models
{
    public class AppConfigModel
    {
        public static string DefaultConnectionString { get; set; }
        public static string userId { get; set; } = string.Empty;
        public static string roleCode { get; set; } = string.Empty;
        public static int tokenType { get; set; } 
        public static DateTime refreshTokenValidity { get; set; }
        public bool isAuthenticated { get; set; }
        public static bool rereshTokenExpired { get; set; }    
        public static string apiRejectedMsg { get; set; } = string.Empty;

        public static string apiFullName { get; set; } = string.Empty;

        public static DateTime issueTime { get; set; } = DateTime.MinValue;
        public static string validToTime { get; set; }=string.Empty;

    }



}

