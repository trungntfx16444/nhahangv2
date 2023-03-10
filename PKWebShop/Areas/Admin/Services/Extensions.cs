using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PKWebShop.Areas.Admin.Services
{
    public static class Extensions
    {
        public static string NullIfWhiteSpace(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) 
            { 
                return null; 
            }
            return value;
        }
    }
}