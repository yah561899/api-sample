using SampleApi.Utils.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureWebHost();
builder.Services.AddServiceCollection();

var app = builder.Build();

string serviceBaseUrl = "http://*:8082";
app.AddWebApplication(serviceBaseUrl);