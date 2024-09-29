using DynamicObjectAPI.Data;
using DynamicObjectAPI.Interfaces;
using DynamicObjectAPI.Models;
using DynamicObjectAPI.Request;
using DynamicObjectAPI.Response;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Dynamic;
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
        { "Payment", new List<string> { "Amount", "PaymentMethod", "PaymentDate", "Status" }},
        { "Customer", new List<string> { "CustomerId", "Name", "City" } },
        { "Address", new List<string> { "City" }},
        { "Category", new List<string> { "Name", "Description" }},
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
                    // Required fields control for the master object
                    var masterCheck = CheckRequiredFields(request.MasterObject.Type, request.MasterObject.Fields);
                    if (!masterCheck.IsSuccess)
                    {
                        return masterCheck;
                    }

                    // Checking the DynamicObjectTypes table for the master object type 
                    // All types are stored in this table
                    var type = _context.DynamicObjectTypes
                        .FirstOrDefault(t => t.Type == request.MasterObject.Type);

                    int masterTypeId;
                    if (type == null)
                    {
                        // If the type does not exist in the table, adding it to the DynamicObjectTypes table
                        var newType = new DynamicObjectTypes
                        {
                            Type = request.MasterObject.Type,
                            CreateDate = DateTime.UtcNow
                        };

                        _context.DynamicObjectTypes.Add(newType);
                        await _context.SaveChangesAsync();

                        masterTypeId = newType.Id; 

                        // Creating a new dynamic table for new type
                        await CreateDynamicTableAsync(request.MasterObject.Type, request.MasterObject.Fields);
                    }
                    else
                    {
                        masterTypeId = type.Id; 
                    }

                    // Adding the incoming master object data to dynamic table
                    await InsertDataIntoDynamicTableAsync(request.MasterObject.Type, request.MasterObject.Fields, masterTypeId);

                    // Adding subObjects
                    if (request.RelatedSubObjects != null && request.RelatedSubObjects.Any())
                    {
                        await AddSubObjectsAsync(request.RelatedSubObjects, masterTypeId); 
                    }

                    transaction.Complete();

                    return new ResponseViewModel<object>
                    {
                        IsSuccess = true,
                        Message = "Dynamic object type created and data inserted successfully."
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

        private async Task AddSubObjectsAsync(List<SubObjectRequest> subObjects, int masterObjectId)
        {
            foreach (var subObject in subObjects)
            {
                // Checking for required fields
                var subObjectCheck = CheckRequiredFields(subObject.Type, subObject.Fields);
                if (!subObjectCheck.IsSuccess)
                {
                    throw new Exception($"Missing required fields for sub-object type {subObject.Type}");
                }

                // Checking the DynamicObjectTypes table for sub-object type 
                var existingType = _context.DynamicObjectTypes.FirstOrDefault(t => t.Type == subObject.Type);
                if (existingType == null)
                {
                    var newType = new DynamicObjectTypes
                    {
                        Type = subObject.Type,
                        CreateDate = DateTime.UtcNow
                    };

                    _context.DynamicObjectTypes.Add(newType);
                    await _context.SaveChangesAsync();

                    // Creating dynamic table for sub-object
                    await CreateDynamicTableAsync(subObject.Type, subObject.Fields);
                }

                // Inserting to the dynamic table
                await InsertDataIntoDynamicTableAsync(subObject.Type, subObject.Fields, masterObjectId);

                // Checking subObject has its own sub-objects, repeat the same process if there is
                if (subObject.RelatedSubObjects != null && subObject.RelatedSubObjects.Any())
                {
                    await AddSubObjectsAsync(subObject.RelatedSubObjects, masterObjectId);
                }
            }
        }

        private async Task CreateDynamicTableAsync(string tableName, Dictionary<string, object> fields)
        {
            //If table does not exist, create dynamic table
            var tableCheckQuery = $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}') " +
                                  $"BEGIN CREATE TABLE [{tableName}] (Id INT PRIMARY KEY IDENTITY(1,1)) END";

            await _context.Database.ExecuteSqlRawAsync(tableCheckQuery);

            // Adding columns based on the fields
            foreach (var field in fields)
            {
                var columnCheckQuery = $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{field.Key}') " +
                                       $"BEGIN ALTER TABLE [{tableName}] ADD [{field.Key}] NVARCHAR(MAX) END";

                await _context.Database.ExecuteSqlRawAsync(columnCheckQuery);
            }
        }

        private async Task InsertDataIntoDynamicTableAsync(string tableName, Dictionary<string, object> fields, int masterObjectId)
        {
            var masterColumnCheckQuery = $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = 'MasterObjectId') " +
                                         $"BEGIN ALTER TABLE [{tableName}] ADD [MasterObjectId] INT END";
            await _context.Database.ExecuteSqlRawAsync(masterColumnCheckQuery);

            foreach (var field in fields)
            {
                var columnCheckQuery = $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{field.Key}') " +
                                       $"BEGIN ALTER TABLE [{tableName}] ADD [{field.Key}] NVARCHAR(MAX) END";
                await _context.Database.ExecuteSqlRawAsync(columnCheckQuery);
            }

            var columns = string.Join(", ", fields.Keys.Append("MasterObjectId"));
            var values = string.Join(", ", fields.Values.Select(v => $"'{v}'").Append(masterObjectId.ToString()));

            var insertQuery = $"INSERT INTO [{tableName}] ({columns}) VALUES ({values})";
            await _context.Database.ExecuteSqlRawAsync(insertQuery);
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

        public async Task<ResponseViewModel<Dictionary<string, object>>> GetObjectAsync(string type, int id)
        {
            try
            {
                // Creating a dynamic SQL query, from the "type" table based on the Id
                // Here I used hard code SQL because it is not clear what we will query from which table, so it will be a very dynamic query
                // This way allows us for greater flexibility in specifying table names and structures

                var query = $"SELECT * FROM [{type}] WHERE Id = @id";

                var parameters = new[] { new SqlParameter("@id", id) };

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.AddRange(parameters);
                    await _context.Database.OpenConnectionAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Converting the result into a dictionary structure
                            var result = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var fieldName = reader.GetName(i);
                                var fieldValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                result.Add(fieldName, fieldValue);
                            }

                            return new ResponseViewModel<Dictionary<string, object>>
                            {
                                IsSuccess = true,
                                Message = "Object found successfully.",
                                Data = result
                            };
                        }
                        else
                        {
                            return new ResponseViewModel<Dictionary<string, object>>
                            {
                                IsSuccess = false,
                                ErrorMessage = "Object not found."
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResponseViewModel<Dictionary<string, object>>
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
