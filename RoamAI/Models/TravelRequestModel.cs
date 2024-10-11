using System.Collections.Generic;

namespace RoamAI.Models
{
    public class TravelRequestModel
    {
        public string Country { get; set; }
        public string City { get; set; }
        public string TravelDate { get; set; }
        public int CulturalPercentage { get; set; }
        public int ModernPercentage { get; set; }
        public int FoodPercentage { get; set; }
        public List<string> Recommendations { get; set; }
    }

}
