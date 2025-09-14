namespace PubsReadOnly.Models;

public class TitleAuthor
{
    public string AuthorId { get; set; } = default!; // au_id
    public string TitleId  { get; set; } = default!; // title_id
    public Author Author { get; set; } = default!;
    public Title  Title  { get; set; } = default!;
}