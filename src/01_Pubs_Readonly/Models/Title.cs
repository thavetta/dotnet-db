namespace PubsReadOnly.Models;

public class Title
{
    public string Id { get; set; } = default!; // title_id
    public string Name { get; set; } = default!; // title
    public ICollection<TitleAuthor> TitleAuthors { get; set; } = new List<TitleAuthor>();
}