using CollectorService.Contracts;
using CollectorService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

builder.Services.AddScoped<IElectricityPriceService, ElectricityPriceService>();

builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapMcp("/api/mcp");

app.MapGet("/api/electricityprices", async (IElectricityPriceService electricityPriceService) =>
{
    var res = await electricityPriceService.GetElectricityPricesAsync();

    if (!res.IsSuccess)
    {
        return Results.BadRequest(res?.ErrorMessage);
    }

    return Results.Ok(res.Value);
})
.WithName("GetElectricityPrices");

await app.RunAsync();
