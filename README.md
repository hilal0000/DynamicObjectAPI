***DYNAMIC OBJECT CREATION API***

**OVERVIEW**

This project is a Dynamic Object Creation and Transaction Management API challenge developed in .NET Core using Entity Framework. 
It allows users to dynamically create, read, update, and delete objects (such as products, orders, customers, etc.) through a unified API.

**GETTING STARTED**

1- Clone or download this repository to your local machine.
2- Build the solution to restore NuGet packages and compile the code.
3. Configure the Database: Open the appsettings.json file and configure the connection string for your SQL Server instance.
4- Apply Database Migrations:  
   Open the **Package Manager Console** in Visual Studio or use the **CLI**.  
   And Run the following command to apply the existing migrations and create the necessary database tables:

   ```bash
   Update-Database
 ``` 
5- Run the Application

**PREREQUISITES**

    .NET 6 SDK
    SQL Server
    Entity Framework Core 7.0.20
    Entity Framework Core Tools (for running migrations)
		
**FEATURES**

1- Dynamic Object Creation:

The API allows users to create dynamic objects with flexible structures, such as orders, products, or customers. Objects are stored in a single database table, and related sub-objects can optionally be added. Key features include:

    Dynamic Fields: Objects can have different fields depending on the type. For some object types (e.g., Order, Product, Payment), specific required fields must be provided. For instance:
        Product requires Name, Price.
        Payment requires Amount, PaymentMethod, PaymentDate, Status.
    Field Validation: Required fields for each object type are checked before saving. If any required field is missing, the creation process is halted with an error message.
    Optional Sub-Objects: Users can add related sub-objects to a master object. Sub-objects are optional but will also undergo required field validation if present.
    Transaction Support: Object creation is wrapped in a transaction to ensure all or nothing operations. If an error occurs, all changes are rolled back.
	
2- CRUD Operations:

    Create: Add new records dynamically for objects.
    Read: Retrieve objects based on type and ID.
    Update: Modify existing records while handling dynamic object structures.
    Delete: Soft delete objects, including related sub-objects when applicable.

3- Transaction Management:

    Handles transactions involving master and related sub-objects.
    Ensures atomicity: if any part of the transaction fails, no objects are created.
	
4- Error Handling:

Error Handling and Unified Response Structure:

    The API provides descriptive error messages for various scenarios, such as missing fields, invalid object structures, database connection issues, and validation failures.
    Unified Response Structure: All API responses follow a consistent, structured format through a unified response model (ResponseViewModel). 
	This ensures that both successful operations and errors are returned in a predictable format, making it easier for consumers to handle responses uniformly.

5- Data Validation:

    Validates required fields based on the object type.
    Dynamic validation logic depending on the object type.
	
**SOLID Principles**

In the design of this API, I have adhered to the following SOLID principles to ensure that the code is maintainable, scalable, and easy to understand:

The Single Responsibility Principle (SRP) was applied to ensure that each class and method has a specific responsibility. 
This approach enhances code manageability and makes testing easier, as changes in one part of the system are less likely to impact other parts.

The Open/Closed Principle (OCP) guided the system's design to be open for extension but closed for modification. 
This means that new object types and fields can be introduced without altering existing code, allowing the API to evolve while maintaining stability.
	
	
