namespace ScoreCast.Models.V1.Requests;

public record ScoreCastRequest
{
    public string? AppName { get; set; }
    public string? UserId { get; set; }
    public Guid ReferenceId { get; set; } = Guid.NewGuid();
}
