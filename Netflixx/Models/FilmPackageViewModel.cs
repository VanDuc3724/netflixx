namespace Netflixx.Models
{
    public class FilmPackageViewModel
    { 
        public string PackageId { get; set; }
        public string PackageName { get; set; }
        public int PackagePrice { get; set; }
        public int ProviderId { get; set; } = 1;

    }
}
