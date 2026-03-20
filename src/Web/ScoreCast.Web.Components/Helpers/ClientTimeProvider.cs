using Microsoft.JSInterop;

namespace ScoreCast.Web.Components.Helpers;

public interface IClientTimeProvider
{
    DateTimeOffset Now { get; }
    DateOnly Today { get; }
    DateTime ToLocal(DateTime utc);
    bool IsInitialized { get; }
    Task InitializeAsync();
}

public sealed class ClientTimeProvider(IJSRuntime js) : IClientTimeProvider
{
    private TimeSpan _offset;

    public bool IsInitialized { get; private set; }

    public DateTimeOffset Now => DateTimeOffset.UtcNow.ToOffset(_offset);

    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);

    public DateTime ToLocal(DateTime utc) => utc.Add(_offset);

    public async Task InitializeAsync()
    {
        if (IsInitialized) return;
        var offsetMinutes = await js.InvokeAsync<int>("eval", "new Date().getTimezoneOffset()");
        _offset = TimeSpan.FromMinutes(-offsetMinutes);
        IsInitialized = true;
    }
}
