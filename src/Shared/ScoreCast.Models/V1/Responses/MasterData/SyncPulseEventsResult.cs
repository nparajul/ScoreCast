namespace ScoreCast.Models.V1.Responses.MasterData;

public record SyncPulseEventsResult(int Processed, int Total, int EventsAdded, bool Complete);
