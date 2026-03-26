namespace ScoreCast.Web.Components.Helpers;

public sealed class PageStateService
{
    private readonly Dictionary<string, Dictionary<string, object>> _state = [];

    public void Set(string pageKey, string key, object value)
    {
        if (!_state.ContainsKey(pageKey)) _state[pageKey] = [];
        _state[pageKey][key] = value;
    }

    public T? Get<T>(string pageKey, string key, T? defaultValue = default)
    {
        if (_state.TryGetValue(pageKey, out var page) && page.TryGetValue(key, out var value) && value is T typed)
            return typed;
        return defaultValue;
    }

    public void Clear(string pageKey) => _state.Remove(pageKey);
}
