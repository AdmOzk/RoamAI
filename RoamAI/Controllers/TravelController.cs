using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using RoamAI.Services;
using RoamAI.Models;
using System.Linq;

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
        public async Task<IActionResult> GetRecommendations(string country, string city, string travelDate, int culturalPercentage, int modernPercentage, int foodPercentage)
        {
            // ClaudeService'e dinamik olarak kullanıcıdan alınan yüzdeleri gönderiyoruz.
            var result = await _claudeService.GetTravelRecommendations(country, city, travelDate, culturalPercentage, modernPercentage, foodPercentage);

            // Sonuçları modele ekliyoruz
            var model = new TravelRequestModel
            {
                Country = country,
                City = city,
                TravelDate = travelDate,
                CulturalPercentage = culturalPercentage,
                ModernPercentage = modernPercentage,
                FoodPercentage = foodPercentage,
                Recommendations = result.Split('\n').ToList()
            };

            // Index view'ını modeliyle birlikte döndürüyoruz
            return View("Index", model);
        }
    }
}
