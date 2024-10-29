using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoamAI.Models.Entities
{
    public class Trip : Base
    {
        [Key]
        [Required]
        public int Id { get; set; }

        public int CulturalPercentage { get; set; }

        public int EntertainmantPercentage { get; set; }

        public int FoodPercentage { get; set; }

        public int DayCountToStay { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int? Score { get; set; }

        public bool IsDone { get; set; }

        public string? Description { get; set; }

        public string? Country { get; set; }

        public string? City { get; set; }

        public string? IdentityUserId { get; set; }

        public bool IsConfirmed { get; set; } = false;

        [ForeignKey("IdentityUserId")]
        public virtual IdentityUser? IdentityUser { get; set; }

        public List<Location>? Locations { get; set; }



    }
}
