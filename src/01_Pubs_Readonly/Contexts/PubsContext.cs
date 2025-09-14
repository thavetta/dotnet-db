using Microsoft.EntityFrameworkCore;
using PubsReadOnly.Models;

namespace PubsReadOnly.Contexts;

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