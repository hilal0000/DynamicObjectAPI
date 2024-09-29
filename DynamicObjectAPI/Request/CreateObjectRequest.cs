using System.ComponentModel.DataAnnotations;

namespace DynamicObjectAPI.Request
{
    public class CreateObjectRequest
    {
        [Required]
        public MasterObjectRequest MasterObject { get; set; }
        public List<SubObjectRequest>? RelatedSubObjects { get; set; }
    }

    public class MasterObjectRequest
    {
        public string Type { get; set; }
        [Required]
        public Dictionary<string, object> Fields { get; set; }
    }

    public class SubObjectRequest
    {
        public string Type { get; set; }
        [Required]
        public Dictionary<string, object> Fields { get; set; }
        public List<SubObjectRequest>? RelatedSubObjects { get; set; }
    }
}
