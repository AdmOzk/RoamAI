using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace RoamAI.Models.Entities
{
    public class UserTrip
    {
        [Key]
        [Required]
        public int UserTripId { get; set; }
        

        public IdentityUser User { get; set; }

        public int TripId { get; set; }

        public Trip? Trip { get; set; }


    }
}
