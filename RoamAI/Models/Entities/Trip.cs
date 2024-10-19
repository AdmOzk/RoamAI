using System.ComponentModel.DataAnnotations;

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

        public int Score { get; set; }

        public bool IsDone { get; set; }

        public string? Description { get; set; }

        public ICollection<Location>? Locations { get; set; }

        public ICollection<UserTrip>? UserTrips { get; set; }



    }
}
