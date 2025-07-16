using Netflixx.Models;
using System.Collections.Generic;

namespace Netflixx.Models.ViewModel
{
    public class MyListViewModel
    {
        public IEnumerable<FilmsModel> MyList { get; set; }
        public IEnumerable<FilmsModel> BasedOnYourList { get; set; }

        // for search + sort UI
        public string Search { get; set; }
        public string SortOrder { get; set; }
    }
}
