namespace ScoreCast.Shared.Requests;

public record ScoreCastRequest
{
    public required string AppName { get; set; }
    public string? UserId { get; set; }
    public Guid ReferenceId { get; set; } = Guid.NewGuid();
}
