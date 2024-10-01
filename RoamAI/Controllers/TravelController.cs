using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using RoamAI.Services;
using RoamAI.Models;

namespace RoamAI.Controllers
{
    public class TravelController : Controller
    {
        private readonly ClaudeService _claudeService;

        public TravelController(ClaudeService claudeService)
        {
            _claudeService = claudeService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new TravelRequestModel());
        }

        [HttpPost]
        public async Task<IActionResult> GetRecommendations(string country, string city, string travelDate, string preferences)
        {
            var result = await _claudeService.GetTravelRecommendations(country, city, travelDate, preferences);

            var model = new TravelRequestModel
            {
                Country = country,
                City = city,
                TravelDate = travelDate,
                Preferences = preferences,
                Recommendations = result.Split('\n').ToList()
            };

            return View("Index", model);
        }
    }
}
