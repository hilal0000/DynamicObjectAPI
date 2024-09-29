using DynamicObjectAPI.Models;
using DynamicObjectAPI.Request;
using DynamicObjectAPI.Response;
using System.Dynamic;

namespace DynamicObjectAPI.Interfaces
{
    public interface IObjectService
    {
        Task<ResponseViewModel<object>> CreateObjectAsync(CreateObjectRequest request);
        Task<ResponseViewModel<Dictionary<string, object>>> GetObjectAsync(string type, int id);
        Task<ResponseViewModel<object>> UpdateObjectAsync(int id, UpdateObjectRequest request);
        Task<ResponseViewModel<object>> DeleteObjectAsync(int id);

    }
}
