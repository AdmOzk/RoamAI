using Microsoft.AspNetCore.Mvc;
using RoamAI.Context;
using RoamAI.Models.Entities;

namespace RoamAI.Controllers
{
    public class LocationController : Controller
    {
        private readonly ApplicationDbContext _db;

        public LocationController(ApplicationDbContext db)
        {
            _db = db;


        }


        public void createLocation(KeyValuePair<string,string> pair, int tripId)
        {
            var newLocation = new Location()
            {
                LocationName = pair.Key,
                Coordinates = pair.Value,
                tripId = tripId,
            };

            _db.Locations.Add(newLocation);
            _db.SaveChanges();

        }

        public List<Location> getLocationsBytripId(int tripId)
        {
            var locationList = _db.Locations.Where(x => x.tripId == tripId).ToList();

            return locationList;
           
        }


        public IActionResult Index()
        {
            return View();
        }
    }
}
