using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SharpSpades.Api.Utils
{
    public static class Extensions
    {
        public static string ToSnakeCase(this string s)
        {
            Throw.IfNull(s);
            // https://www.30secondsofcode.org/c-sharp/s/to-snake-case
            return String.Join('_', Regex.Matches(s, "[A-Z]{2,}(?=[A-Z][a-z]+[0-9]*|\b)|[A-Z]?[a-z]+[0-9]*|[A-Z]|[0-9]+")).ToLower();
        }

        public static string ToTitleCase(this string s)
        {
            Throw.IfNull(s);
            return String.Join("", s.Split('_').Select(x => String.Join("", x[0].ToString().ToUpper(), x.Substring(1))));
        }
    }
}
