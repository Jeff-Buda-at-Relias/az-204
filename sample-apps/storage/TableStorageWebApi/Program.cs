using Azure;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var builder = WebApplication.CreateBuilder(args);

// Add Azure App Configuration
builder.Configuration.AddAzureAppConfiguration((options) =>
{
    options.Connect(builder.Configuration[AppConfigKeys.AppConfigUrl])
           .ConfigureKeyVault(kv =>
           {
               kv.SetCredential(new DefaultAzureCredential());
           });

});
var tableServiceUri = builder.Configuration[AppConfigKeys.TableStorageUrl]!;

// Add Azure Table Service Client
builder.Services.AddAzureClients(builder =>
{
    builder.AddTableServiceClient(new Uri(tableServiceUri));
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapGet("/", () => "Jeff's AZ-204 demo app");

app.MapGet("/partition/{partitionKey}/item/{itemKey}", async (string partitionKey, string itemKey, TableServiceClient tableServiceClient) =>
{
    var tableClient = tableServiceClient.GetTableClient(app.Configuration[AppConfigKeys.TableStorageTableName]);
    var response = await tableClient.GetEntityAsync<TableEntity>(partitionKey, itemKey);
    
    return Results.Ok(response.Value);
});

app.MapPost("/table/{tableName}", async (string tableName, TableEntity entity, TableServiceClient tableServiceClient) =>
{
    var tableClient = tableServiceClient.GetTableClient(tableName);
    await tableClient.AddEntityAsync(entity);
    return Results.Created($"/table/{tableName}/{entity.RowKey}", entity);
});

app.MapPut("/table/{tableName}/{rowKey}", async (string tableName, string rowKey, TableEntity entity, TableServiceClient tableServiceClient) =>
{
    var tableClient = tableServiceClient.GetTableClient(tableName);
    await tableClient.UpdateEntityAsync(entity, ETag.All);
    return Results.NoContent();
});

app.MapDelete("/table/{tableName}/{rowKey}", async (string tableName, string rowKey, TableServiceClient tableServiceClient) =>
{
    //var tableClient = tableServiceClient.GetTableClient(tableName);
    //await tableClient.DeleteEntityAsync(rowKey, entity.PartitionKey);
    return Results.NoContent();
});

app.Run();

public static class AppConfigKeys
{
    public const string AppConfigUrl = "AppConfigUrl";
    public const string TableStorageUrl = "TableStorageUrl";
    public const string TableStorageTableName = "ItemTable";

}