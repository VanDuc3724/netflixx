using Microsoft.AspNetCore.Mvc.RazorPages;
using Netflixx.Models;

public class IndexModel : PageModel
{
    public IEnumerable<FilmsModel> Films { get; set; }

    public void OnGet()
    {
        // TODO: Load your films here, e.g. from a database
        Films = new List<FilmsModel>(); // Replace with actual data
    }
}