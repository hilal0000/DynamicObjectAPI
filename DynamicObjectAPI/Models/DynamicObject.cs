namespace DynamicObjectAPI.Models
{
    public class DynamicObject
    {
        public int Id { get; set; }
        public string Type { get; set; } 
        public string Data { get; set; } 
        public DateTime CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }
        public bool IsDeleted { get; set; } 
        public int? MasterObjectId { get; set; }
    }
}
