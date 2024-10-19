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
    // Seyahat önerilerini alıyoruz.
    var travelRecommendations = await _claudeService.GetTravelRecommendations(country, city, travelDate, culturalPercentage, modernPercentage, foodPercentage);

    // Şehir bilgilerini alıyoruz.
    var cityInformation = await _claudeService.GetCityInformation(country, city);

    // Sonuçları modele ekliyoruz.
    var model = new TravelRequestModel
    {
        Country = country,
        City = city,
        TravelDate = travelDate,
        CulturalPercentage = culturalPercentage,
        ModernPercentage = modernPercentage,
        FoodPercentage = foodPercentage,
        Recommendations = travelRecommendations.Split('\n').ToList(),
        CityInformation = cityInformation // Şehir bilgisini modele ekledik.
    };

    // Index view'ını modeliyle birlikte döndürüyoruz.
    return View("Index", model);
}

    }
}
