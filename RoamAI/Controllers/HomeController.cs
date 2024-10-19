using Microsoft.AspNetCore.Mvc;

namespace RoamAI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
