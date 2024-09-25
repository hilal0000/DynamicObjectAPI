using DynamicObjectAPI.Data;
using DynamicObjectAPI.Interfaces;
using DynamicObjectAPI.Models;
using DynamicObjectAPI.Request;
using DynamicObjectAPI.Response;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Transactions;

namespace DynamicObjectAPI.Services
{
    public class ObjectService : IObjectService
    {
        private readonly ApplicationDbContext _context;

        public ObjectService(ApplicationDbContext context)
        {
            _context = context;
        }

        private readonly Dictionary<string, List<string>> requiredFields = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
{
    { "Product", new List<string> { "Name", "Price" } },
    { "Order", new List<string> { "CustomerId", "ProductList" } },
};


        private ResponseViewModel<object> CheckRequiredFields(string objectType, Dictionary<string, object> fields)
        {
            if (requiredFields.TryGetValue(objectType, out var requiredFieldList))
            {
                foreach (var requiredField in requiredFieldList)
                {
                    if (!fields.Keys.Any(k => k.Equals(requiredField, StringComparison.OrdinalIgnoreCase)))
                    {
                        return new ResponseViewModel<object>
                        {
                            IsSuccess = false,
                            ErrorMessage = $"Missing required field: {requiredField} for object type: {objectType}."
                        };
                    }
                }
            }

            return new ResponseViewModel<object> { IsSuccess = true }; 
        }

        public async Task<ResponseViewModel<object>> CreateObjectAsync(CreateObjectRequest request)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    //Required fields control for the master object
                    var masterCheck = CheckRequiredFields(request.MasterObject.Type, request.MasterObject.Fields);
                    if (!masterCheck.IsSuccess)
                    {
                        return masterCheck;
                    }

                    var masterObject = new DynamicObject
                    {
                        Type = request.MasterObject.Type,
                        Data = JsonConvert.SerializeObject(CleanFields(request.MasterObject.Fields)),
                        CreateDate = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    _context.DynamicObjects.Add(masterObject);
                    await _context.SaveChangesAsync();

                    //Required fields control for sub-objects
                    if (request.RelatedSubObjects != null && request.RelatedSubObjects.Any())
                    {
                        foreach (var subObject in request.RelatedSubObjects)
                        {
                            var subObjectCheck = CheckRequiredFields(subObject.Type, subObject.Fields);
                            if (!subObjectCheck.IsSuccess)
                            {
                                return subObjectCheck;
                            }

                            var relatedObject = new DynamicObject
                            {
                                Type = subObject.Type,
                                Data = JsonConvert.SerializeObject(CleanFields(subObject.Fields)),
                                CreateDate = DateTime.UtcNow,
                                IsDeleted = false,
                                MasterObjectId = masterObject.Id
                            };

                            _context.DynamicObjects.Add(relatedObject);
                        }
                    }

                    await _context.SaveChangesAsync();
                    transaction.Complete();

                    return new ResponseViewModel<object>
                    {
                        IsSuccess = true,
                        Message = "Dynamic object created successfully."
                    };
                }
                catch (DbUpdateException ex)
                {
                    return new ResponseViewModel<object>
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Database error: {ex.Message}"
                    };
                }
                catch (Exception ex)
                {
                    return new ResponseViewModel<object>
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Transaction failed: {ex.Message}"
                    };
                }
            }
        }

        public Dictionary<string, object> CleanFields(Dictionary<string, object> fields)
        {
            var cleanedFields = new Dictionary<string, object>();

            foreach (var field in fields)
            {
                if (field.Value is JsonElement jsonElement)
                {
                    switch (jsonElement.ValueKind)
                    {
                        case JsonValueKind.String:
                            cleanedFields.Add(field.Key, jsonElement.GetString());
                            break;
                        case JsonValueKind.Number:
                            cleanedFields.Add(field.Key, jsonElement.GetInt32()); 
                            break;
                        case JsonValueKind.Object:
                            cleanedFields.Add(field.Key, jsonElement.ToString()); 
                            break;
                        default:
                            cleanedFields.Add(field.Key, jsonElement.ToString());
                            break;
                    }
                }
                else
                {
                    cleanedFields.Add(field.Key, field.Value);
                }
            }

            return cleanedFields;
        }

        public async Task<ResponseViewModel<DynamicObjectResponse>> GetObjectAsync(string type, int id)
        {
            try
            {
                var obj = await _context.DynamicObjects
                    .Where(o => o.Type == type && o.Id == id && !o.IsDeleted)
                    .FirstOrDefaultAsync();

                if (obj == null)
                {
                    return new ResponseViewModel<DynamicObjectResponse>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Object not found."
                    };
                }

                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(obj.Data);

                var dynamicObjectResponse = new DynamicObjectResponse
                {
                    Id = obj.Id,
                    Type = obj.Type,
                    Data = data,
                    CreateDate = obj.CreateDate,
                    ModifyDate = obj.ModifyDate,
                    IsDeleted = obj.IsDeleted,
                    MasterObjectId = obj.MasterObjectId
                };

                return new ResponseViewModel<DynamicObjectResponse>
                {
                    IsSuccess = true,
                    Message = "Object found successfully.",
                    Data = dynamicObjectResponse
                };
            }
            catch (DbUpdateException dbEx)
            {
                return new ResponseViewModel<DynamicObjectResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = $"Database error: {dbEx.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ResponseViewModel<DynamicObjectResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = $"An unexpected error occurred: {ex.Message}"
                };
            }
        }

        public async Task<ResponseViewModel<object>> UpdateObjectAsync(int id, UpdateObjectRequest request)
        {
            try
            {
                var dynamicObject = await _context.DynamicObjects.FindAsync(id);

                if (dynamicObject == null)
                {
                    return new ResponseViewModel<object>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Object not found."
                    };
                }

                var checkFieldsResult = CheckRequiredFields(dynamicObject.Type, request.Fields);
                if (!checkFieldsResult.IsSuccess)
                {
                    return checkFieldsResult;
                }

                var newData = JsonConvert.SerializeObject(request.Fields);
                dynamicObject.Data = newData;
                dynamicObject.ModifyDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new ResponseViewModel<object>
                {
                    IsSuccess = true,
                    Message = "Object updated successfully."
                };
            }
            catch (DbUpdateException dbEx)
            {
                return new ResponseViewModel<object>
                {
                    IsSuccess = false,
                    ErrorMessage = $"Database update failed: {dbEx.Message}"
                };
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                return new ResponseViewModel<object>
                {
                    IsSuccess = false,
                    ErrorMessage = $"Invalid data structure: {jsonEx.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ResponseViewModel<object>
                {
                    IsSuccess = false,
                    ErrorMessage = $"An unexpected error occurred: {ex.Message}"
                };
            }
        }


        public async Task<ResponseViewModel<object>> DeleteObjectAsync(int id)
        {
            try
            {
                var obj = await _context.DynamicObjects.FindAsync(id);

                if (obj == null)
                {
                    return new ResponseViewModel<object>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Object not found."
                    };
                }

                //Soft delete the master object
                obj.IsDeleted = true;

                //Find the relevant sub-objects and soft delete
                var relatedSubObjects = await _context.DynamicObjects
                    .Where(o => o.MasterObjectId == id && !o.IsDeleted)
                    .ToListAsync();

                foreach (var subObject in relatedSubObjects)
                {
                    subObject.IsDeleted = true;
                }

                await _context.SaveChangesAsync();

                return new ResponseViewModel<object>
                {
                    IsSuccess = true,
                    Message = "Object deleted successfully."
                };
            }
            catch (DbUpdateException dbEx)
            {
                return new ResponseViewModel<object>
                {
                    IsSuccess = false,
                    ErrorMessage = $"Database update failed: {dbEx.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ResponseViewModel<object>
                {
                    IsSuccess = false,
                    ErrorMessage = $"An unexpected error occurred: {ex.Message}"
                };
            }
        }
    }
}
