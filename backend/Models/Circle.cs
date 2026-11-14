namespace Backend.Models;

public class Circle
{
    public long Id { get; set; }

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public string CreatedByUserId { get; set; } = "";

    public User CreatedByUser { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public bool IsPrivate { get; set; }

    public ICollection<UserCircleRole> Members { get; set; } = [];

    public ICollection<Post> Posts { get; set; } = [];
}
