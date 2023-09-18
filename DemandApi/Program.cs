using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Text.Json.JsonSerializer;


Func<string, IResult> BadRequest = (x) => Results.Problem(detail: $"{x} is invalid", statusCode: StatusCodes.Status400BadRequest, title: "Bad Request");

Predicate<string> CEmp = x => string.IsNullOrEmpty(x);

var conn = new SqlConnection("Server=10.10.1.138; Database=BBIDemand; User ID=sa; Password='=*fj9*N*uLBRNZV'; MultipleActiveResultSets = true");

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true);

builder.Services.AddDbContext<DemandDb>(options =>
{
    // Get the connection string from configuration
    string connectionString = builder.Configuration.GetConnectionString(name: "DemandApiContext");

    options.UseSqlServer(connectionString);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

app.MapPost("/SavePaymentCollection", async (HttpContext context) =>
{
    string query;
    try
    {
        // Define the SQL query
        int i;
        dynamic requestData = null;

        if (context.Request.ContentType == "application/x-www-form-urlencoded")
            requestData = await context.Request.ReadFormAsync();

        if (requestData is null) return Results.BadRequest("Request is invalid");

        string subCompanyId = requestData["subCompanyId"];
        string SalesPersonCode = requestData["userId"];
        string DDate = requestData["date"];
        string ReceiptId = requestData["ReceiptId"];
        string Mode = requestData["collectionMode"];
        string CustomerId = requestData["CustomerId"];
        string ReceivedAmount = requestData["cashReceiveAmt"];
        string Description = requestData["remark"];
        string Lat = requestData["latitude"];
        string Long = requestData["longitude"];

        if (CEmp(subCompanyId)) return BadRequest("SubcompanyId");
        if (CEmp(DDate)) return BadRequest("Date");
        if (CEmp(ReceiptId)) return BadRequest("ReceiptId");
        if (!int.TryParse(Mode, out int mode)) return BadRequest("CollectionMode");
        if (CEmp(CustomerId)) return BadRequest("CustomerId");
        if (CEmp(SalesPersonCode)) return BadRequest("SalesPersonCode");

        if (!DateTime.TryParse(DDate.ToString(), out DateTime Date))
            return BadRequest("Date");

        string numericPattern = @"^\d{1,15}(\.\d{2})?$";

        if (!Regex.IsMatch(ReceivedAmount, numericPattern)) return BadRequest("CashReceiveAmt");

        string sqlDate = $"{Date.Year}-{Date.Month}-{Date.Day}";

        // Create a command and set parameters
        using (SqlCommand cmd = new("[dbo].[SP_SavePaymentCollection]", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 50;
            cmd.Parameters.AddRange(
                new SqlParameter[] {
                    new SqlParameter("@SubcompanyId",subCompanyId),
                    new SqlParameter("@SalesPersonCode",SalesPersonCode),
                    new SqlParameter("@ReceiptId",ReceiptId),
                    new SqlParameter("@DDate",sqlDate),
                    new SqlParameter("@Mode",mode),
                    new SqlParameter("@CustomerId",CustomerId),
                    new SqlParameter("@ReceivedAmount",ReceivedAmount),
                    new SqlParameter("@Description",Description),
                    new SqlParameter("@Latitude",Lat),
                    new SqlParameter("@Longitude",Long),
                }
            );
            await conn.OpenAsync();
            i = Convert.ToInt32(cmd.ExecuteScalar());
        };
        // Return a response
        return i > 0 ? Results.Created(i.ToString(), "Inserted") : Results.Problem(title: "Not Inserted", statusCode: StatusCodes.Status500InternalServerError, detail: "Internal Server Error");
    }
    catch (Exception ex)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = $"Internal Server Error : {ex.Message}",
            Detail = ex.ToString()
        };
        return Results.Problem(problemDetails);
    }
    finally
    {
        conn.Close();
    }
});

app.MapPost("/itemlist", async (HttpContext context) =>
{
    DataTable dt = new();
    Dictionary<string, object> cln;
    dynamic? data = null;
    try
    {
        dynamic requestData = null;

        if (context.Request.ContentType == "application/x-www-form-urlencoded")
            requestData = await context.Request.ReadFormAsync();

        // Extract data from request
        //if(context.Request.ContentType == "") 

        if (requestData is null) return Results.BadRequest("Request is invalid");

        string subCompanyId = requestData["subCompanyId"];
        string itemGroupId = requestData["itemGroupId"];
        string CustomerId = requestData["CustomerId"];

        if (CEmp(subCompanyId)) return BadRequest("SubcompanyId");
        if (CEmp(itemGroupId)) return BadRequest("ItemGroupId");
        if (CEmp(CustomerId)) return BadRequest("CustomerId");

        using (SqlCommand cmd = new("[SP_GetItemList]", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 50;
            cmd.Parameters.AddRange(
                new SqlParameter[]
                {
                    new SqlParameter("@SubcompanyId", subCompanyId),
                    new SqlParameter("@itemGroupId", itemGroupId),
                    new SqlParameter("@CustomerId", CustomerId)
                }
            );
            using (SqlDataAdapter adptr = new(cmd))
            {
                await conn.OpenAsync();
                adptr.Fill(dt);
            };
        };
        var rows = dt != null && dt.Rows.Count > 0 ? dt.Rows : null;

        data = rows?.OfType<DataRow>()
                       .Select(row => dt.Columns.OfType<DataColumn>()
                       .ToDictionary(col => col.ColumnName, col => row[col]));
        cln = new Dictionary<string, object>()
        {
            { "Status", data is null ? "Item not found" : "Success"},
            { "StatusCode", data is null ? StatusCodes.Status204NoContent: StatusCodes.Status200OK },
            { "Data", data }
        };
        return Results.Ok(cln);
    }
    catch (Exception ex)
    {
        // Return internal server error response
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = $"Internal Server Error : {ex.Message}",
            Detail = ex.ToString()
        };
        return Results.Problem(problemDetails);
    }
    finally
    {
        conn.Close();
        dt?.Dispose();
    }
});

app.MapPost("/customerlist", async (HttpContext context) =>
{
    DataTable dt = new();
    Dictionary<string, object> cln;
    dynamic? data = null;
    try
    { // Extract data from request
        //var requestData = await context.Request.ReadFromJsonAsync<Demand>();
        dynamic requestData = null;

        if (context.Request.ContentType == "application/x-www-form-urlencoded")
            requestData = await context.Request.ReadFormAsync();

        if (requestData is null) return Results.BadRequest("Request is invalid");

        string subCompanyId = requestData["subCompanyId"];
        string supervisorCode = requestData["supervisorCode"];
        string invoiceDate = requestData["invoiceDate"];

        if (CEmp(subCompanyId)) return BadRequest("SubcompanyId");
        if (CEmp(supervisorCode)) return BadRequest("supervisorCode");
        if (CEmp(invoiceDate)) return BadRequest("invoiceDate");

        using (SqlCommand cmd = new("[SP_GetCustomerList]", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddRange(
                new SqlParameter[]
                {
                    new SqlParameter("@SubcompanyId", subCompanyId),
                    new SqlParameter("@SupervisorCode", supervisorCode),
                    new SqlParameter("@InvoiceDate", invoiceDate)
                }
            );
            cmd.CommandTimeout = 50;
            using (SqlDataAdapter adptr = new(cmd))
            {
                await conn.OpenAsync();
                adptr.Fill(dt);
            };
        };

        var rows = dt != null && dt.Rows.Count > 0 ? dt.Rows : null;

        data = rows?.OfType<DataRow>()
                       .Select(row => dt.Columns.OfType<DataColumn>()
                       .ToDictionary(col => col.ColumnName, col => row[col]));
        cln = new()
        {
            { "Status", data is null ? "Customer not found" : "Success"},
            { "StatusCode", data is null ? StatusCodes.Status204NoContent: StatusCodes.Status200OK },
            { "Data", data }
        };
        return Results.Ok(cln);
    }
    catch (Exception ex)
    {
        // Return internal server error response
        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = ex.ToString()
        };
        return Results.Problem(problemDetails);
    }
    finally
    {
        conn.Close();
        dt?.Dispose();
    }
});

app.MapPut("/updateDemand", async (HttpContext context) =>
{
    try
    {
        //var requestData = await context.Request.ReadFromJsonAsync<Demand>();
        dynamic requestData = null;
        dynamic msg;

        if (context.Request.ContentType == "application/x-www-form-urlencoded")
            requestData = await context.Request.ReadFormAsync();

        if (requestData is null) return Results.BadRequest("Request is invalid");

        string CustomerId = requestData["CustomerId"];
        var clnStr = requestData["Collection"][0];
        string orderNo = requestData["OrderNo"];


        if (CEmp(CustomerId)) return BadRequest("CustomerId");
        if (CEmp(orderNo)) return BadRequest("OrderNo");
        if (CEmp(clnStr)) return BadRequest("ItemDetails");

        string arr = Deserialize<Dictionary<string, object>>(clnStr)["ItemnQtyList"].ToString();

        using (SqlCommand cmd = new("[SP_UpdateDemand]", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 50;
            cmd.Parameters.AddRange(
                new SqlParameter[]
                {
                    new SqlParameter("@CustomerId", CustomerId),
                    new SqlParameter("@Collection", arr.ToUpper()),
                    new SqlParameter("@OrderNo", orderNo)
                }
            );
            await conn.OpenAsync();
            msg = await cmd.ExecuteScalarAsync();
            //msg = msg is null ? null : msg.ToString();
        };
        return msg is null ? Results.Ok("Demand updated successfully") : Results.BadRequest(msg.ToString());
    }
    catch (Exception ex)
    {
        // Return internal server error response
        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = $"Internal Server Error : {ex.Message}",
            Detail = ex.ToString()
        };
        return Results.Problem(problemDetails);
    }
    finally
    {
        conn.Close();
    }
});

app.MapPost("/orderList", async (HttpContext context) =>
{
    DataTable dt = new();
    Dictionary<string, object> cln;
    dynamic? data = null;
    try
    {
        //var requestData = await context.Request.ReadFromJsonAsync<Demand>();
        dynamic requestData = null;

        if (context.Request.ContentType == "application/x-www-form-urlencoded")
            requestData = await context.Request.ReadFormAsync();

        if (requestData is null) return Results.BadRequest("Request is invalid");

        string CustomerId = requestData["CustomerId"];
        string date = requestData["date"];

        if (CEmp(CustomerId)) return BadRequest("CustomerId");
        //if (CEmp(date)) return BadRequest("Date");
        using (SqlCommand cmd = new("[SP_GetOrderList]", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 50;
            cmd.Parameters.AddRange(
                new SqlParameter[]
                {
                    new SqlParameter("@CustomerId", CustomerId)
                }
            );
            using (SqlDataAdapter adptr = new(cmd))
            {
                await conn.OpenAsync();
                adptr.Fill(dt);
            }
        }
        var rows = dt != null && dt.Rows.Count > 0 ? dt.Rows : null;

        data = rows?.OfType<DataRow>()
                       .Select(row => dt.Columns.OfType<DataColumn>()
                       .ToDictionary(col => col.ColumnName, col => row[col]));
        cln = new()
        {
            { "Status", data is null ? "Order not found" : "Success"},
            { "StatusCode", data is null ? StatusCodes.Status204NoContent: StatusCodes.Status200OK },
            { "Data", data }
        };
        return Results.Ok(cln);
    }
    catch (Exception ex)
    {
        // Return internal server error response
        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = $"Internal Server Error : {ex.Message}",
            Detail = ex.ToString()
        };
        return Results.Problem(problemDetails);
    }
    finally
    {
        conn.Close();
    }
});

app.MapPost("/appVersion", async (HttpContext context) =>
{
    string latestAppVrsn;
    try
    {
        dynamic requestData = null;

        if (context.Request.ContentType == "application/x-www-form-urlencoded")
            requestData = await context.Request.ReadFormAsync();

        if (requestData is null) return Results.BadRequest("Request is invalid");

        string AppVersion = requestData["AppVersion"];

        if (CEmp(AppVersion)) return BadRequest("AppVersion");

        using (SqlCommand cmd = new("[SP_GetAppVersion]", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 50;
            conn.Open();
            latestAppVrsn = cmd.ExecuteScalar() as string;
        }
        return Results.Ok(AppVersion.Equals(latestAppVrsn) ? "Updated" : "Not Updated");
    }
    catch (Exception ex)
    {
        // Return internal server error response
        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = $"Internal Server Error : {ex.Message}",
            Detail = ex.ToString()
        };
        return Results.Problem(problemDetails);
    }
    finally
    {
        conn.Close();
    }
});
app.UseHttpsRedirection();
app.Run();

//app.MapGet("/todoitems/complete", async (TodoDb db) =>
//await db.Todos.Where(t => t.IsComplete).ToListAsync());

//app.MapGet("/todoitems/{id}", async (int id, TodoDb db) =>
//    await db.Todos.FindAsync(id)
//        is Todo todo
//            ? Results.Ok(todo)
//            : Results.NotFound());
//app.MapPost("/todoitems", async (Todo todo, TodoDb db) =>
//{
//    db.Todos.Add(todo);
//    await db.SaveChangesAsync();

//    return Results.Created($"/todoitems/{todo.Id}", todo);
//});

//app.MapPut("/todoitems/{id}", async (int id, Todo inputTodo, TodoDb db) =>
//{
//    var todo = await db.Todos.FindAsync(id);

//    if (todo is null) return Results.NotFound();

//    todo.Name = inputTodo.Name;
//    todo.IsComplete = inputTodo.IsComplete;

//    await db.SaveChangesAsync();

//    return Results.NoContent();
//});

//app.MapDelete("/todoitems/{id}", async (int id, DemandDb db) =>
//{
//    if (await db.Demands.FindAsync(id) is Demand Demand)
//    {
//        db.Demands.Remove(Demand);
//        await db.SaveChangesAsync();
//        return Results.Ok(Demand);
//    }

//    return Results.NotFound();
//});