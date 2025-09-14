namespace PubsReadOnly.Models;

public class Author
{
    public string Id { get; set; } = default!; // au_id
    public string FirstName { get; set; } = default!; // au_fname
    public string LastName  { get; set; } = default!; // au_lname
    public bool Contract { get; set; } // contract
    public ICollection<TitleAuthor> TitleAuthors { get; set; } = new List<TitleAuthor>();
}