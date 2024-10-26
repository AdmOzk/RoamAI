using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoamAI.Context;
using RoamAI.Models;
using RoamAI.Models.Entities;
using RoamAI.Services;
using System.Collections;
using System.Security.Cryptography;
using System.Text.Json;

namespace RoamAI.Controllers
{
    public class TripController : Controller
    {

        private readonly ApplicationDbContext  _db;
        private readonly ClaudeService _claudeService;

        public TripController(ApplicationDbContext db, ClaudeService claudeService)
        {
            _db = db;
            _claudeService = claudeService;


        }



        // GET: TripController
        public ActionResult Index()
        {
            
            

            return View();
        }

        public ActionResult myTrips(string userId)
        {
            var trips = getTripsByUserId(userId);


            return View(trips);
        }


        public ActionResult CurrentTrip(int userId)
        {
            var trip = _db.Trips.Where(x => x.Id == userId && x.IsDone == false);


            return View(trip);
        }





        // GET: TripController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: TripController/Create
        public ActionResult CreateTrip()
        {
            return View();
        }

        // POST: TripController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateTrip(Trip trip)
        {
            //users!!

            var newTrip = new Trip()
            {
                EntertainmantPercentage = trip.EntertainmantPercentage,
                FoodPercentage = trip.FoodPercentage,
                CulturalPercentage=trip.CulturalPercentage, 
                DayCountToStay = trip.DayCountToStay,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                Score = trip.Score,
                IsDone = false,
                Locations = trip.Locations, 
                Description = trip.Description,

            };

            foreach(var Location in newTrip.Locations) { }

            _db.Add(newTrip);
            
            _db.SaveChanges();

            return RedirectToAction("Index");//todo
            
        }

        public void changeDoneTripStatus(int tripId)
        {
            var trip = _db.Trips.FirstOrDefault(x => x.Id == tripId);

            if(trip != null)
            {
                trip.IsDone = true;
                _db.SaveChanges();
            }
        }

        
        public ActionResult Delete(int id)
        {
            var trip = _db.Trips.FirstOrDefault(x => x.Id == id);

            if (trip != null)
            {
                trip.IsDeleted = true;
                _db.SaveChanges();
            }

            return RedirectToAction("Index");
        }
        


        public IList<Trip> getTripsByUserId(string id)
        {
            var trips =  _db.UserTrips
           .Where(x => x.User.Id == id)
           .Select(x => x.Trip)
           .ToList();

            return trips;
        }



        public async Task<IActionResult> GetRecommendations(string country, string city, string StartDate,string EndDate, int culturalPercentage, int entertainmantPercentage, int foodPercentage)
        {
            // Seyahat önerilerini alıyoruz.
            var travelRecommendations = await _claudeService.GetTravelRecommendations(country, city, StartDate, EndDate , culturalPercentage, entertainmantPercentage, foodPercentage);

            // Şehir bilgilerini alıyoruz.
            var cityInformation = await _claudeService.GetCityInformation(country, city);

            var locationCoordinatesJson = System.Text.Json.JsonSerializer.Serialize(ClaudeService.locationCoordinates);

            // ViewBag'e JSON olarak ekliyoruz
            ViewBag.LocationCoordinates = locationCoordinatesJson;

            // Sonuçları modele ekliyoruz.
            var model = new TravelRequestModel
            {
                Country = country,
                City = city,
                StartDate = StartDate,
                EndDate = EndDate,
                CulturalPercentage = culturalPercentage,
                EntertainmantPercentage = entertainmantPercentage,
                FoodPercentage = foodPercentage,
                Recommendations = travelRecommendations.Split('\n').ToList(),
                CityInformation = cityInformation // Şehir bilgisini modele ekledik.
            };

            // Index view'ını modeliyle birlikte döndürüyoruz.
            return View("CreateTrip", model);
        }

    }
}

