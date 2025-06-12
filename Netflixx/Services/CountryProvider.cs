using System.Collections.Generic;
using System.Linq;
using System.Globalization;
namespace Netflixx.Services
{
    public static class CountryProvider
    {
        public static List<string> GetCountryNames()
        {
            return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .Select(c => new RegionInfo(c.LCID).EnglishName)
                .Distinct()
                .OrderBy(name => name)
                .ToList();
        }
    }
}