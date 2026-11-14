namespace Backend.Models;

public class CircleRole
{
    public const string Owner = "Owner";
    public const string Moderator = "Moderator";
    public const string Member = "Member";

    public int Id { get; set; }

    public string Name { get; set; } = "";
}
