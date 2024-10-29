using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoamAI.Context;
using RoamAI.Models;
using RoamAI.Models.Entities;
using RoamAI.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoamAI.Controllers
{
    public class TripController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ClaudeService _claudeService;
        private readonly UserManager<IdentityUser> _userManager;

        public TripController(ApplicationDbContext db, ClaudeService claudeService, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _claudeService = claudeService;
            _userManager = userManager;
        }

        // GET: TripController
        public async Task<ActionResult> Index()
        {
            return View();
        }

        public async Task<ActionResult> CurrentTrip()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            var trip = await _db.Trips
               .Include(t => t.Locations)
               .FirstOrDefaultAsync(x => x.IdentityUserId == userId && x.IsDone == false && x.IsConfirmed == true);

            return View(trip);
        }

        public async Task<ActionResult> TripDetail(int id)
        {
            var trip = await _db.Trips
                .Include(t => t.Locations)
                .FirstOrDefaultAsync(x => x.Id == id);
            
            return View(trip);
        }

        public async Task<ActionResult> MyTrips()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            var trips = await getTripsByUserIdAsync(userId);
            return View(trips);
        }

        // GET: TripController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            return View();
        }

        // GET: TripController/Create
        public async Task<ActionResult> CreateTrip()
        {
            return View();
        }

        // POST: TripController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateTrip(Trip trip)
        {
            // Kullanıcı bilgilerini asenkron olarak alın
            var user = await _userManager.GetUserAsync(User);

            // Kullanıcı kimliğini ve kullanıcıyı trip nesnesine atayın
            trip.IdentityUserId = user.Id;
            trip.IdentityUser = user;

            // Trip nesnesine bağlı Locations koleksiyonuna bağlı her bir konumu ekleyin
            foreach (var loc in trip.Locations)
            {
                loc.Trip = trip;
                _db.Locations.Add(loc);
            }

            // Trip kaydını ekleyin ve veritabanına kaydedin
            _db.Trips.Add(trip);
            await _db.SaveChangesAsync(); // SaveChanges işlemini yalnızca bir kez yapın

            return RedirectToAction("MyTrips", "Trip");
        }

        public async Task<ActionResult> changeDoneTripStatus(int id)
        {
            var trip = await _db.Trips.FirstOrDefaultAsync(x => x.Id == id);

            if (trip != null)
            {
                trip.IsDone = true;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("MyTrips");
        }


        public async Task<ActionResult> confirm(int id)
        {
            var trip = await _db.Trips.FirstOrDefaultAsync(x => x.Id == id);

            if (trip != null)
            {
                trip.IsConfirmed = true;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("CurrentTrip");
        }

        public async Task<ActionResult> Delete(int id)
        {
            var trip = await _db.Trips.FirstOrDefaultAsync(x => x.Id == id);

            if (trip != null)
            {
                trip.IsDeleted = true;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("MyTrips");
        }

        public async Task<List<Trip>> getTripsByUserIdAsync(string id)
        {
            return await _db.Trips.Where(x => x.IdentityUserId == id && x.IsDeleted == false && x.IsConfirmed==true).ToListAsync();
        }

        [HttpPost]
        public async Task<IActionResult> GetRecommendations(string country, string city, DateTime StartDate, DateTime EndDate, int culturalPercentage, int entertainmantPercentage, int foodPercentage)
        {
            var travelRecommendations = await _claudeService.GetTravelRecommendations(country, city, StartDate, EndDate, culturalPercentage, entertainmantPercentage, foodPercentage);
            var cityInformation = await _claudeService.GetCityInformation(country, city);

            // Konumları hazırla
            var locations = travelRecommendations.LocationCoordinates.Select(location => new Location
            {
                LocationName = location.Key,
                Coordinates = location.Value
            }).ToList();

            var user = await _userManager.GetUserAsync(User);

            // Trip kaydını oluştur ve veritabanına ekle
            var tripModel = new Trip()
            {
                Country = country,
                City = city,
                StartDate = StartDate,
                EndDate = EndDate,
                DayCountToStay = (EndDate - StartDate).Days,
                CulturalPercentage = culturalPercentage,
                EntertainmantPercentage = entertainmantPercentage,
                FoodPercentage = foodPercentage,
                Description = cityInformation,
                Locations = locations,
                IdentityUser = user,
                IdentityUserId = user.Id
            };
            
            _db.Trips.Add(tripModel);
            await _db.SaveChangesAsync();

            // Verileri ViewBag ile geç
            ViewBag.Locations = locations;
            ViewBag.CityInfo = cityInformation;
            ViewBag.TripId = tripModel.Id;


            return RedirectToAction("RecommendationResult", new { tripId = tripModel.Id });
        }

        [HttpGet]
        [Route("Trip/GetLocations")]
        public string GetLocations(int tripId)
        {
            var locations = _db.Locations
                .Where(l => l.tripId == tripId)
                .Select(l => $"{l.LocationName}|{l.Coordinates}")
                .ToList();

            return string.Join(";", locations);
        }




        [HttpGet]
        public async Task<IActionResult> RecommendationResult(int tripId)
        {
            var trip = await _db.Trips
                .Include(t => t.Locations)
                .FirstOrDefaultAsync(t => t.Id == tripId);

            if (trip == null)
            {
                return NotFound();
            }

            GetLocations(tripId);

            ViewBag.Locations = trip.Locations; // Marker bilgilerini ViewBag ile geçiyoruz
            ViewBag.TripId = tripId;

            return View(trip);
        }


    }
}
