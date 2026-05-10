using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Web;
//sluzi da bi prepoznao paramter i kada nisu u istom redosledu

namespace PrviProjekat
{
  
    public static class Helper
    {
        public static string Normalizuj(string query)
        {
            if (string.IsNullOrEmpty(query))
                return "";

            var parts = query
                .ToLower()
                .Split('&')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .OrderBy(p => p);

            return string.Join("&", parts);
        }
    }

}

    



