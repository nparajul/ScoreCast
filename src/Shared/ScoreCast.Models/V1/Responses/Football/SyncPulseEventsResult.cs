namespace ScoreCast.Models.V1.Responses.Football;

public record SyncPulseEventsResult(int Processed, int Total, int EventsAdded, bool Complete);
