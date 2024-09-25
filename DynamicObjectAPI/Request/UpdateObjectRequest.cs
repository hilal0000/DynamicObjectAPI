using System.ComponentModel.DataAnnotations;

namespace DynamicObjectAPI.Request
{
    public class UpdateObjectRequest
    {
        public Dictionary<string, object> Fields { get; set; }
    }
}
