namespace ScoreCast.Shared.Responses;

public record ScoreCastResponse
{
    private protected ScoreCastResponse() { }
    public string? Message { get; init; }
    public required ScoreCastResultType ResultType { get; init; }
    public string? Code { get; init; }
    public Guid ReferenceId { get; init; } = Guid.NewGuid();
    public bool Success => ResultType is ScoreCastResultType.Ok;

    public static ScoreCastResponse Ok() =>
        new() { ResultType = ScoreCastResultType.Ok };

    public static ScoreCastResponse Ok(string message) =>
        new() { ResultType = ScoreCastResultType.Ok, Message = message };

    public static ScoreCastResponse Ok(string message, string code) =>
        new() { ResultType = ScoreCastResultType.Ok, Message = message, Code = code };

    public static ScoreCastResponse Error(string message) =>
        new() { ResultType = ScoreCastResultType.Error, Message = message };

    public static ScoreCastResponse Error(string message, string code) =>
        new() { ResultType = ScoreCastResultType.Error, Message = message, Code = code };

    public static ScoreCastResponse NotFound(string message) =>
        new() { ResultType = ScoreCastResultType.NotFound, Message = message };

    public static ScoreCastResponse Exception(string message) =>
        new() { ResultType = ScoreCastResultType.Exception, Message = message };
}

public record ScoreCastResponse<T> : ScoreCastResponse
{
    public T? Data { get; init; }

    public static ScoreCastResponse<T> Ok(T data) =>
        new() { ResultType = ScoreCastResultType.Ok, Data = data };

    public static ScoreCastResponse<T> Ok(T data, string message) =>
        new() { ResultType = ScoreCastResultType.Ok, Data = data, Message = message };

    public static ScoreCastResponse<T> Ok(T data, string message, string code) =>
        new() { ResultType = ScoreCastResultType.Ok, Data = data, Message = message, Code = code };

    public new static ScoreCastResponse<T> Error(string message) =>
        new() { ResultType = ScoreCastResultType.Error, Message = message };

    public new static ScoreCastResponse<T> Error(string message, string code) =>
        new() { ResultType = ScoreCastResultType.Error, Message = message, Code = code };

    public new static ScoreCastResponse<T> NotFound(string message) =>
        new() { ResultType = ScoreCastResultType.NotFound, Message = message };

    public new static ScoreCastResponse<T> Exception(string message) =>
        new() { ResultType = ScoreCastResultType.Exception, Message = message };
}
