namespace Netflixx.Models
{
    public class FilterViewModel
    {

        public string SearchName { get; set; }
        public string RoleType { get; set; }
        public string SortOrder { get; set; }
        public string Address { get; set; }

        public int MinAge { get; set; } = 0;
        public int MaxAge { get; set; } = 100;

        public List<string> FavoriteGenres { get; set; } = new();
        public string Status { get; set; }
    }
}
