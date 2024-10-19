namespace RoamAI.Models.Entities
{
    public class Base
    {
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; } = DateTime.Now;
        public int? UpdateBy { get; set; }
        public DateTime? UpdateOn { get; set; } = new DateTime();
        public bool? IsDeleted { get; set; } = false;
    }
}
