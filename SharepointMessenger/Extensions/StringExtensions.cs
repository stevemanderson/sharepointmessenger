using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace SharepointMessenger.Extensions
{
    public static class StringExtensions
    {
        public static string CleanXSS(this string value)
        {
            return HttpUtility.HtmlDecode(Regex.Replace(value, "<[^>]*(>|$)", string.Empty));
        }
    }
}
