using System.Collections.Generic;

namespace Netflixx.Models.ViewModel
{
    public class FilmpackageIndexViewModel
    {
        public int PackageId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public List<string> Films { get; set; } = new();
    }
}
