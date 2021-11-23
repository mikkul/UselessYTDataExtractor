using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace UselessYoutubeDataExtractor.Pages
{
    public class IndexModel : PageModel
    {
        private readonly string _validYoutubeVideoUrlRegex = @"^((http|https)\:\/\/)?(www\.youtube\.com|youtu\.?be)\/((watch\?v=)?([a-zA-Z0-9]{11}))(&.*)*$";

        public void OnGet()
        {
        }

        [BindProperty]
		public string VideoUrl { get; set; }

		public IActionResult OnPost()
		{
            var match = Regex.Match(VideoUrl ?? "", _validYoutubeVideoUrlRegex);
            if(!match.Success)
			{
                TempData["ErrorMessage"] = "Invalid URL";
                return RedirectToPage("Index");
			}

            var videoId = match.Groups[6];

            return RedirectToPage("Results", new { videoId = videoId });
		}
    }
}
