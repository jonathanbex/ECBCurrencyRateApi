using ECBCurrencyRates.Cache;
using ECBCurrencyRates.ECBIntegration;
using ECBCurrencyRates.Middleware;
using Microsoft.OpenApi.Models;
using static ECBCurrencyRates.ECBIntegration.Models.ECBSeriesModel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddSwaggerGen(options =>
{
  var contact = new OpenApiContact { Name = "Jonathan Soderberg" };
  options.SwaggerDoc("v1", new OpenApiInfo { Title = "JSO Currency ECB Rate API", Version = "v1", Description = "Used to Query ECB For currency rates simplified REST", Contact = contact });

  //var xmlDocPath = string.Format(@"{0}/ECBCurrencyRates.xml", System.AppDomain.CurrentDomain.BaseDirectory);

  //options.IncludeXmlComments(xmlDocPath);

});
builder.Services.AddSingleton<ECBApplicationLayer>();
builder.Services.AddSingleton<ICacheProvider, CacheResolver>();
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
  c.SwaggerEndpoint("/swagger/v1/swagger.json", "JSO Currency ECB Rate API");
  c.RoutePrefix = ""; // Serve Swagger UI at the app's root URL
  c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
});

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseHttpsRedirection();


app.MapGet("/GetExchangeRates", async (string currencyCode, string? currenciesToCheck, ECBApplicationLayer ecbService) =>
{
  // Call your service method to get exchange rate data
  var currenciesToCheckArray = currenciesToCheck?.Split(',');

  var result = await ecbService.RelayCurrencyRequest(currencyCode, currenciesToCheckArray);

  // Serialize result to JSON and return
  return result;
})
.WithName("GetExchangeRates")
.WithDescription("Get Currency rates, Params : currencyCode 3 letters ISO 4217. currenciesToCheck array like SEK,EUR,USD with comma also  ISO 4217")
.WithOpenApi();


app.MapGet("/GetExchangeRatesForSpecificDay", async (string currencyCode, DateTime day, string? currenciesToCheck, ECBApplicationLayer ecbService) =>
{
  // Call your service method to get exchange rate data
  var currenciesToCheckArray = currenciesToCheck?.Split(',');

  var result = await ecbService.RelayCurrencyRequest(currencyCode, currenciesToCheckArray,day);

  // Serialize result to JSON and return
  return result;
})
.WithName("GetExchangeRatesForSpecificDay")
.WithDescription("Get Currency rates, Params : currencyCode 3 letters ISO 4217. Date to fetch set rates for a specific day (time doesnt matter), currenciesToCheck array like SEK,EUR,USD with comma also  ISO 4217")
.WithOpenApi();

app.Run();

