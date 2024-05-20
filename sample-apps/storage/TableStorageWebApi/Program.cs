using Azure;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Queues;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add Azure App Configuration
builder.Configuration.AddAzureAppConfiguration((options) =>
{
    options.Connect(builder.Configuration[AppConfigKeyNames.AppConfigUrl])
           .ConfigureKeyVault(kv =>
           {
               kv.SetCredential(new DefaultAzureCredential());
           });


});

// Add Azure Table Service Client
builder.Services.AddAzureClients(b =>
{
    b.AddTableServiceClient(builder.Configuration.GetConnectionString("TableStorage"));
    b.AddQueueServiceClient(builder.Configuration.GetConnectionString("QueueStorage"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapGet("/", () => "Jeff's AZ-204 demo app");

app.MapGet("/partition/{partitionKey}/item/{itemKey}", async (string partitionKey, string itemKey, TableServiceClient tableServiceClient) =>
{
    var tableClient = tableServiceClient.GetTableClient(app.Configuration[AppConfigKeyNames.TableStorageTableName]);
    var response = await tableClient.GetEntityAsync<TableEntity>(partitionKey, itemKey);

    return Results.Ok(response.Value);
});

app.MapPost(
    "/partition/{partitionKey}/item/{itemKey}",
async (
    string partitionKey,
    string itemKey,
    [FromBody] TableEntity entity,
    TableServiceClient tableServiceClient,
    QueueServiceClient queueServiceClient) =>
{
    // write to Azure Table Storage
    var tableClient = tableServiceClient.GetTableClient(app.Configuration[AppConfigKeyNames.TableStorageTableName]);
    entity.PartitionKey = partitionKey;
    entity.RowKey = itemKey;
    await tableClient.AddEntityAsync(entity);

    // write to Azure Queue Storage
    var queueClient = queueServiceClient.GetQueueClient(app.Configuration[AppConfigKeyNames.QueueStorageQueueName]);
    await queueClient.SendMessageAsync(JsonSerializer.Serialize(
        new { PartitionKey = partitionKey, RowKey = entity.RowKey }));

    return Results.Created();
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

public static class AppConfigKeyNames
{
    public const string AppConfigUrl = nameof(AppConfigKeyNames.AppConfigUrl);
    public const string TableStorageUrl = nameof(AppConfigKeyNames.TableStorageUrl);
    public const string TableStorageTableName = nameof(AppConfigKeyNames.TableStorageTableName);
    public const string QueueStorageUrl = nameof(AppConfigKeyNames.QueueStorageUrl);
    public const string QueueStorageQueueName = nameof(AppConfigKeyNames.QueueStorageQueueName);

}