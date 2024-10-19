using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoamAI.Context;
using RoamAI.Models;
using RoamAI.Models.Entities;
using RoamAI.Services;
using System.Collections;
using System.Security.Cryptography;

namespace RoamAI.Controllers
{
    public class TripController : Controller
    {

        private readonly ApplicationDbContext  _db;
        private readonly ClaudeService _claudeService;

        public TripController(ApplicationDbContext db)
        {
            _db = db;


        }

        public TripController(ClaudeService claudeService)
        {
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



        // GET: TripController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: TripController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TripController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Trip trip)
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

            foreach(Location in newTrip.Locations) { }

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



        [HttpPost]
        public TravelRequestModel GetRecommendations(string country, string city, DateTime startDate, int culturalPercentage, int entertainmantPercentage, int foodPercentage)
        {
            // ClaudeService'e dinamik olarak kullanıcıdan alınan yüzdeleri gönderiyoruz.
            var result =  _claudeService.GetTravelRecommendations(country, city, startDate, culturalPercentage, entertainmantPercentage, foodPercentage);

            // Sonuçları modele ekliyoruz
            var model = new TravelRequestModel
            {
                Country = country,
                City = city,
                StartDate = startDate,
                
                CulturalPercentage = culturalPercentage,
                EntertainmantPercentage = entertainmantPercentage,
                FoodPercentage = foodPercentage,
                Recommendations = result.Split(new[] { Environment.NewLine },StringSplitOptions.None).ToList()
            };

            // Index view'ını modeliyle birlikte döndürüyoruz
            return  model;
        }
    }
}
