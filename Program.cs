using Bsmarter.Controllers;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Allow frontend access (adjust policy for production)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();

// Serve wwwroot
app.UseStaticFiles();

// Serve physical uploads folder (outside wwwroot) at /uploads
var uploadsRoot = app.Configuration["Uploads:RootPath"];
if (!string.IsNullOrWhiteSpace(uploadsRoot))
{
    Directory.CreateDirectory(uploadsRoot);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsRoot),
        RequestPath = "/uploads"
    });
}

// Swagger UI (acts like your Scalar 'api docs' in this project)
app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/api", () => Results.Redirect("/swagger"));

// Minimal API endpoints
app.MapUploadController();

// Blazor
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();