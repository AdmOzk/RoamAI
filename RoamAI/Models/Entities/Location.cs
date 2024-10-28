using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoamAI.Models.Entities
{
    public class Location: Base
    {

        [Key]
        [Required]
        public int Id { get; set; }

        public string? LocationName { get; set; }

        public string? Coordinates { get; set; }

        public int? tripId { get; set; }

        [JsonIgnore]
        public Trip? Trip { get; set; }


    }
}
