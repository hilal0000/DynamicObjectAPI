using DynamicObjectAPI.Data;
using DynamicObjectAPI.Interfaces;
using DynamicObjectAPI.Models;
using DynamicObjectAPI.Request;
using DynamicObjectAPI.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DynamicObjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DynamicObjectController : ControllerBase
    {
        private readonly IObjectService _objectService;

        public DynamicObjectController(IObjectService objectService)
        {
            _objectService = objectService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateObject([FromBody] CreateObjectRequest request)
        {
            var response = await _objectService.CreateObjectAsync(request);

            if (response.IsSuccess)
            {
                return Ok(response); 
            }
            else
            {
                return BadRequest(response); 
            }
        }

        [HttpGet("{objectType}/{id}")]
        public async Task<IActionResult> GetObject(string objectType, int id)
        {
            var response = await _objectService.GetObjectAsync(objectType, id);

            if (!response.IsSuccess)
                return NotFound(new ResponseViewModel<DynamicObjectResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = response.ErrorMessage
                });

            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateObject(int id, [FromBody] UpdateObjectRequest request)
        {
            var response= await _objectService.UpdateObjectAsync(id, request);
            if (!response.IsSuccess)
            {
                return BadRequest(response); 
            }

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteObject(int id)
        {
            var response = await _objectService.DeleteObjectAsync(id);

            if (!response.IsSuccess)
            {
                return BadRequest(response); 
            }

            return Ok(response); 
        }
    }
}
