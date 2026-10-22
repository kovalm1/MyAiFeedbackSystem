using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using MyAiFeedbackSystem.WebApp.Client.SessionState;
using MyAiFeedbackSystem.WebApp.Client.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();
builder.Services.AddScoped<IClientSessionStateStore, BrowserSessionStateStore>();
builder.Services.AddScoped<AdminAuthClient>();
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
