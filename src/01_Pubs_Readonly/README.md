# 01 – PUBS (read‑only)

**Cíl:** Připojit EF Core ke stávající DB `pubs`, nakonfigurovat `DbContext`, vypsat autory a počty jejich titulů.

## Kroky pro studenty

1. Vytvořte nový projekt  **01_Pubs_ReadOnly** buď ve VS 2022 nebo VS Code jako **Command Line App** pro **.NET 9**
1. Přidejte do projektu tyto nuget balíčky:

    - Microsoft.EntityFrameworkCore
    - Microsoft.EntityFrameworkCore.SqlServer
    - Microsoft.Extensions.Configuration.Json
    - Microsoft.Extensions.Configuration.EnvironmentVariables
    - Microsoft.Extensions.Configuration.CommandLine
    - Microsoft.EntityFrameworkCore.Tools

1. Přidejte do projektu soubor `appsettings.json` a přidejte sekci obsahující ConnectionString a sekci pro logování. Nezapomeňte nastavit, že se má soubor kopírovat při buildu do cílové složky.

    ```json
    {
        "ConnectionStrings": {
            "Pubs": "Server=.;Database=pubs;Trusted_Connection=True;TrustServerCertificate=True;"
        },
        "Logging": {
            "LogLevel": {
            "Default": "Information",
            "Microsoft": "Warning",
            "Microsoft.EntityFrameworkCore.Database.Command": "Information"
            }
        }
    }
    ```

1. Pomocí SQL management Studia zkontrolujte zda máte na SQL serveru databázi pubs a pokud ne, vytvořte si ji pomocí instalačního skriptu (na GOPAS počítači na disku E:, na vlastním stáhnout z [GitHubu](https://github.com/microsoft/sql-server-samples/blob/master/samples/databases/northwind-pubs/instpubs.sql))
1. Přidejte do projektu složky *Models* a *Contexts*
1. Do složky *Models* přidejte class **Author**, pak **Title** a ještě **TitleAuthor**. Tyto třídy namapujeme na tabulky authors, titles a titleauthor v DB.
1. Do souboru Author.cs přidejte kód definující property:

    ```cs
    namespace PubsReadOnly.Models;

    public class Author
    {
        public string Id { get; set; } = default!; // au_id
        public string FirstName { get; set; } = default!; // au_fname
        public string LastName  { get; set; } = default!; // au_lname
        public bool Contract { get; set; } // contract
        public ICollection<TitleAuthor> TitleAuthors { get; set; } = new List<TitleAuthor>();
    }
    ```

1. Do Title.cs přidejte:

    ```cs
    namespace PubsReadOnly.Models;

    public class Title
    {
        public string Id { get; set; } = default!; // title_id
        public string Name { get; set; } = default!; // title
        public ICollection<TitleAuthor> TitleAuthors { get; set; } = new List<TitleAuthor>();
    }
    ```

1. A jako poslední vytvořte propojovací třídu TitleAuthor v TitleAuthor.cs:

    ```cs
    namespace PubsReadOnly.Models;

    public class TitleAuthor
    {
        public string AuthorId { get; set; } = default!; // au_id
        public string TitleId  { get; set; } = default!; // title_id
        public Author Author { get; set; } = default!;
        public Title  Title  { get; set; } = default!;
    }
    ```

1. Teď je nutno vytvořit třídu, která bude potomek DbContext a zajistí funkčnost EF Core. Do Contexts přidejte soubor PubsContext.cs.
1. Do třídy nejdřív přidejte info o kolekcích odpovídajících našim třem tabulkám a standardní konstruktor umožňující zadat *options*.

    ```cs
    using Microsoft.EntityFrameworkCore;
    using PubsReadOnly.Models;

    namespace PubsReadOnly.Contexts;

    public class PubsContext : DbContext
    {
        public DbSet<Author> Authors => Set<Author>();
        public DbSet<Title> Titles => Set<Title>();
        public DbSet<TitleAuthor> TitleAuthors => Set<TitleAuthor>();
        public PubsContext(DbContextOptions<PubsContext> options) : base(options) { }
    }
    ```

1. Dále do třídy přidejte **override** metodu OnModelCreating, která zajistí mapování našich hezkých názvů vlastností a typů na názvy sloupců a tabulek v DB a zároveň definuje cizí klíče, aby fungovali vazby.

    ```cs
    protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Author>(e =>
            {
                e.ToTable("authors");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("au_id");
                e.Property(x => x.FirstName).HasColumnName("au_fname").HasMaxLength(20);
                e.Property(x => x.LastName).HasColumnName("au_lname").HasMaxLength(40);
                e.Property(x => x.Contract).HasColumnName("contract");
            });
            b.Entity<Title>(e =>
            {
                e.ToTable("titles");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("title_id");
                e.Property(x => x.Name).HasColumnName("title").HasMaxLength(80);
            });
            b.Entity<TitleAuthor>(e =>
            {
                e.ToTable("titleauthor");
                e.HasKey(x => new { x.AuthorId, x.TitleId });
                e.Property(x => x.AuthorId).HasColumnName("au_id");
                e.Property(x => x.TitleId).HasColumnName("title_id");
                e.HasOne(x => x.Author).WithMany(a => a.TitleAuthors).HasForeignKey(x => x.AuthorId);
                e.HasOne(x => x.Title).WithMany(t => t.TitleAuthors).HasForeignKey(x => x.TitleId);
            });
        }
    ```

1. V Main metodě v Program.cs udělejte nejdřív kód pro načtení konfigurace a přípravy EF:

    ```cs
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
    ```

    1. Následně přidejte kód, který vybere z DB požadované informace a zobrazí je.

    ```cs
    var data = await db.Authors
        .Select(a => new { Name = a.FirstName + " " + a.LastName, Titles = a.TitleAuthors.Count })
        .OrderByDescending(x => x.Titles)
        .ToListAsync();

    Console.WriteLine("Autor — počet titulů");
    foreach (var row in data)
        Console.WriteLine($"{row.Name} — {row.Titles}");

    ```

1. Spusť projekt a sledujte SQL v konzoli (zapnuté logování).
