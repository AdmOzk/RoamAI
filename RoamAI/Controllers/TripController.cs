using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoamAI.Context;
using RoamAI.Models;
using RoamAI.Models.Entities;
using RoamAI.Services;
using System.Collections;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

namespace RoamAI.Controllers
{
    public class TripController : Controller
    {

        private readonly ApplicationDbContext  _db;
        private readonly ClaudeService _claudeService;
        private readonly UserManager<IdentityUser> _userManager;

        public TripController(ApplicationDbContext db, ClaudeService claudeService, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _claudeService = claudeService;
            _userManager = userManager;


        }



        // GET: TripController
        public ActionResult Index()
        {
            
            

            return View();
        }

        public ActionResult CurrentTrip()
        {
            var user = _userManager.GetUserAsync(User);
            var userId = user.Result.Id;
            var trip = _db.Trips.FirstOrDefault(x => x.IdentityUserId == userId && x.IsDone == false);


            return View(trip);
        }


        public ActionResult TripDetail(int tripId)
        {
            var trip = _db.Trips.Where(x => x.Id == tripId && x.IsDone == true).ToList();


            return View(trip);
        }

        public ActionResult MyTrips()
        {
            var user = _userManager.GetUserAsync(User);
            var userId = user.Result.Id;
            var trip = getTripsByUserId(userId);


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

            foreach(var loc in trip.Locations)
            {
                loc.Trip = trip;

                _db.Locations.Add(loc);
            }
            var user = _userManager.GetUserAsync(User);
            _db.SaveChanges();
            

            var newTrip = new Trip()
            {
                EntertainmantPercentage = trip.EntertainmantPercentage,
                FoodPercentage = trip.FoodPercentage,
                CulturalPercentage = trip.CulturalPercentage, 
                DayCountToStay = trip.DayCountToStay,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                Country = trip.Country,
                City = trip.City,
                Score = trip.Score,
                IsDone = false,
                Locations = trip.Locations, 
                Description = trip.Description,
                IdentityUser = user.Result,
                IdentityUserId = user.Result.Id


            };

            _db.Trips.Add(newTrip);
            
            _db.SaveChanges();

            return RedirectToAction("CurrentTrip");//todo
            
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

            return RedirectToAction("MyTrips");
        }
        


        public List<Trip> getTripsByUserId(string id)
        {
            var trips =  _db.Trips.Where(x=> x.IdentityUserId == id).ToList();   

            return trips;
        }



        public void GetRecommendations(string country, string city, DateTime StartDate,DateTime EndDate, int culturalPercentage, int entertainmantPercentage, int foodPercentage)
        {
            // Seyahat önerilerini alıyoruz.
            var travelRecommendations =  _claudeService.GetTravelRecommendations(country, city, StartDate, EndDate , culturalPercentage, entertainmantPercentage, foodPercentage).Result.text;

            // Şehir bilgilerini alıyoruz.
            var cityInformation =  _claudeService.GetCityInformation(country, city).Result;

            var locationCoordinatesJson = System.Text.Json.JsonSerializer.Serialize(ClaudeService.locationCoordinates);

            // ViewBag'e JSON olarak ekliyoruz
            ViewBag.LocationCoordinates = locationCoordinatesJson;

            // Sonuçları modele ekliyoruz.
            //var model = new TravelRequestModel
            //{
            //    Country = country,
            //    City = city,
            //    StartDate = StartDate,
            //    EndDate = EndDate,
            //    CulturalPercentage = culturalPercentage,
            //    EntertainmantPercentage = entertainmantPercentage,
            //    FoodPercentage = foodPercentage,
            //    Recommendations = travelRecommendations.Split('\n').ToList(),
            //    CityInformation = cityInformation // Şehir bilgisini modele ekledik.
            //};

            List<Location> locations = new List<Location>();
            var tempDict = _claudeService.GetTravelRecommendations(country, city, StartDate, EndDate, culturalPercentage, entertainmantPercentage, foodPercentage).Result.LocationCoordinates;
            //dictionary -> ICollection

            foreach (var location in tempDict) {

                var loc = new Location()
                {
                    LocationName = location.Key,
                    Coordinates = location.Value,
                };

                locations.Add(loc);
            
            
            }

            tempDict.Clear();
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
                

            };


            CreateTrip(tripModel);
        }

    }
}

