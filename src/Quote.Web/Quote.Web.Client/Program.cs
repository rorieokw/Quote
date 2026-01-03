using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Quote.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure HttpClient to connect to the API
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5102/")
});

// Register services
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<TradieService>();
builder.Services.AddScoped<MessagesService>();
builder.Services.AddScoped<DisputeService>();
builder.Services.AddScoped<SignalRService>();

await builder.Build().RunAsync();
