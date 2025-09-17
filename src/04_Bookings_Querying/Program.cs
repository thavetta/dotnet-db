using Bookings.Contexts;
using Bookings.Infrastructure;
using Bookings.Models;
using Microsoft.EntityFrameworkCore;
using Bookings.Endpoints;
using Bookings.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("SqlServer")
         ?? "Server=.\\SQLEXPRESS;Database=03_Bookings_MappingAdvanced;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<BookingsDbContext>(o =>
    o.UseSqlServer(cs, sql => sql.EnableRetryOnFailure())
     .EnableSensitiveDataLogging());

var app = builder.Build();

app.UseSeeding();
app.MapLab4Endpoints();

// Dev: automatická migrace
if (app.Environment.IsDevelopment())
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        await db.Database.MigrateAsync();
    }

app.MapGet("/health", () => Results.Ok(new { ok = true }));


app.Run();
