using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var cfg = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
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

#region Model
public class Author
{
    public string Id { get; set; } = default!; // au_id
    public string FirstName { get; set; } = default!; // au_fname
    public string LastName  { get; set; } = default!; // au_lname
    public bool Contract { get; set; } // contract
    public ICollection<TitleAuthor> TitleAuthors { get; set; } = new List<TitleAuthor>();
}
public class Title
{
    public string Id { get; set; } = default!; // title_id
    public string Name { get; set; } = default!; // title
    public ICollection<TitleAuthor> TitleAuthors { get; set; } = new List<TitleAuthor>();
}
public class TitleAuthor
{
    public string AuthorId { get; set; } = default!; // au_id
    public string TitleId  { get; set; } = default!; // title_id
    public Author Author { get; set; } = default!;
    public Title  Title  { get; set; } = default!;
}
public class PubsContext : DbContext
{
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Title> Titles => Set<Title>();
    public DbSet<TitleAuthor> TitleAuthors => Set<TitleAuthor>();
    public PubsContext(DbContextOptions<PubsContext> options) : base(options) { }
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
}
#endregion
