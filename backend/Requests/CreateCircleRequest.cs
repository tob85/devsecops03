namespace Backend.Requests;

public record CreateCircleRequest(string Name, string? Description, bool IsPrivate);
