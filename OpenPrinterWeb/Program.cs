using OpenPrinterWeb.Components;
using OpenPrinterWeb.Services;
using OpenPrinterWeb.Hubs;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddSignalR();
builder.Services.AddSingleton<IPrintService, CupsPrintService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddHostedService<PrinterStatusBackgroundService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseStaticFiles();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<PrinterHub>("/printerhub");

app.Run();