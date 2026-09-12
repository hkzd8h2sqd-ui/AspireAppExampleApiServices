using AspireApp1.ServiceDefaults;
using AspireApp1.StateStore;
using AspireApp1.WorkerService4;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.Configure<ServiceSettings>(builder.Configuration.GetSection(ServiceSettings.SectionName));
builder.Services.AddHostedService<StatusMonitor>();
builder.Services.AddHttpClient("workerservice1", client =>
{
    client.BaseAddress = new Uri("https+http://workerservice1");
});
builder.Services.AddHttpClient("workerservice2", client =>
{
    client.BaseAddress = new Uri("https+http://workerservice2");
});
builder.Services.AddHttpClient("workerservice3", client =>
{
    client.BaseAddress = new Uri("https+http://workerservice3");
});
builder.Services.AddHttpClient("apiservice", client =>
{
    client.BaseAddress = new Uri("https+http://apiservice");
});
builder.Services.AddHttpClient("apiserviceforecast", client =>
{
    client.BaseAddress = new Uri("https+http://apiserviceforecast");
});
builder.Services.AddHttpClient("webfrontend", client =>
{
    client.BaseAddress = new Uri("https+http://webfrontend");
});

builder.Services.AddConfiguredStateStoreDbContext(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseTraceContextLogScope();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StateStoreDbContext>();
    await DatabaseInitializer.EnsureSchemaAsync(db);
}

app.MapDefaultEndpoints();

app.Run();
