namespace Backend.Models;

public class Post
{
    public long Id { get; set; }

    public long CircleId { get; set; }

    public Circle Circle { get; set; } = null!;

    public string Message { get; set; } = "";

    public string UserId { get; set; } = "";

    public User User { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
}
