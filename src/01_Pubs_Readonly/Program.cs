using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using PubsReadOnly.Contexts;
using PubsReadOnly.Models;

var cfg = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var cs = cfg.GetConnectionString("Pubs") ?? throw new Exception("Missing ConnectionStrings:Pubs");

var options = new DbContextOptionsBuilder<PubsContext>()
    .UseSqlServer(cs)
    .LogTo(Console.WriteLine)
    .EnableSensitiveDataLogging()
    .Options;

using var db = new PubsContext(options);

var data = await db.Authors
    .Select(a => new { Name = a.FirstName + " " + a.LastName, Titles = a.TitleAuthors.Count })
    .OrderByDescending(x => x.Titles)
    .ToListAsync();

Console.WriteLine("Autor — počet titulů");
foreach (var row in data)
    Console.WriteLine($"{row.Name} — {row.Titles}");

