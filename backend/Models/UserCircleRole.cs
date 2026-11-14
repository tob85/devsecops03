namespace Backend.Models;

public class UserCircleRole
{
    public string UserId { get; set; } = "";

    public User User { get; set; } = null!;

    public long CircleId { get; set; }

    public Circle Circle { get; set; } = null!;

    public int RoleId { get; set; }

    public CircleRole Role { get; set; } = null!;
}
