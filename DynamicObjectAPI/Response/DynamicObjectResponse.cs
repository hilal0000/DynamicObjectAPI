namespace DynamicObjectAPI.Response
{
    public class DynamicObjectResponse
    {
        public int Id { get; set; }
        public string Type { get; set; } 
        public Dictionary<string, object> Data { get; set; } 
        public DateTime CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }
        public bool IsDeleted { get; set; } 
        public int? MasterObjectId { get; set; }
    }
}
