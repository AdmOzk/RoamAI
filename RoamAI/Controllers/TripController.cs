﻿using Microsoft.AspNetCore.Authorization;
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

        [Authorize]
        public async Task<ActionResult> CurrentTrip()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            var trip = await _db.Trips
               .Include(t => t.Locations)
               .FirstOrDefaultAsync(x => x.IdentityUserId == userId && x.IsDone == false && x.IsConfirmed == true);

            if (trip != null)
            {
                ViewBag.TripId = trip.Id; // Set the TripId in ViewBag
            }

            return View(trip);
        }

        [Authorize]
        public async Task<ActionResult> TripDetail(int id)
        {
            var trip = await _db.Trips
                .Include(t => t.Locations)
                .FirstOrDefaultAsync(x => x.Id == id);

            ViewBag.TripId = id; // Set the TripId in ViewBag

            return View(trip);
        }

        [Authorize]
        public async Task<ActionResult> MyTrips()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            var trips = await getTripsByUserIdAsync(userId);
            return View(trips);
        }

        [Authorize]
        // GET: TripController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            return View();
        }

        [Authorize]
        // GET: TripController/Create
        public async Task<ActionResult> CreateTrip()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;

            // Check if there's an active trip for the user
            var existingTrip = await _db.Trips
                .FirstOrDefaultAsync(x => x.IdentityUserId == userId && x.IsDone == false && x.IsConfirmed == true);

            if (existingTrip != null)
            {
                // If there’s an active trip, set ViewBag flag
                ViewBag.HasActiveTrip = true;
                ViewBag.CurrentTripId = existingTrip.Id; // Optional: if you need the trip ID
            }
            else
            {
                ViewBag.HasActiveTrip = false;
            }

            return View();
        }


        [Authorize]
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

        [Authorize]
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

        [Authorize]
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

        [Authorize]
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

        [Authorize]
        public async Task<List<Trip>> getTripsByUserIdAsync(string id)
        {
            return await _db.Trips.Where(x => x.IdentityUserId == id && x.IsDeleted == false && x.IsConfirmed==true).ToListAsync();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> GetRecommendations(string country, string city, DateTime StartDate, DateTime EndDate, int culturalPercentage, int entertainmantPercentage, int foodPercentage)
        {
            var travelRecommendations = await _claudeService.GetTravelRecommendations(country, city, StartDate, EndDate, culturalPercentage, entertainmantPercentage, foodPercentage);
            var cityInformation = await _claudeService.GetCityInformation(country, city,StartDate,EndDate);

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

        [Authorize]
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

        public async Task<IActionResult> SynthesizeText(string text,int tripId)
        {
            // Modelden gelen veya view'dan gelen metin
            string audioUrl = await _claudeService.SynthesizeSpeechAsync(text,tripId);

            // View'e ses dosyasının yolunu gönder
            ViewBag.AudioUrl = audioUrl;
            return View();
        }



        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SynthesizeDescription(int id, string viewName = "RecommendationResult")
        {
            var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == id);
            if (trip == null || string.IsNullOrWhiteSpace(trip.Description))
            {
                return Json(new { success = false, message = "Açıklama bulunamadı veya mevcut değil." });
            }

            // Açıklamayı seslendirin ve tripId kullanarak benzersiz dosya adı oluşturun
            string audioUrl = await _claudeService.SynthesizeSpeechAsync(trip.Description, trip.Id);

            if (audioUrl == null)
            {
                return Json(new { success = false, message = "Seslendirme sırasında bir hata oluştu." });
            }

            // Ses dosyasının URL'sini JSON olarak döndürün
            return Json(new { success = true, audioUrl });
        }

        [Authorize]
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
