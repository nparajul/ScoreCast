using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ScoreCast.ApiClient.V1.Apis;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class Highlights : ScoreCastComponentBase, IAsyncDisposable
{
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IClientTimeProvider ClientTime { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private readonly List<HighlightItem> _items = [];
    private bool _hasMore = true;
    private bool _loadingMore;
    private int _currentIndex;
    private DotNetObjectReference<Highlights>? _dotnetRef;
    private ElementReference _containerRef;
    private bool _jsReady;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ClientTime.InitializeAsync();
            await Loading.While(async () =>
            {
                var res = await Api.GetAllHighlightsAsync(0, 10, CancellationToken.None);
                if (res is { Success: true, Data.Items.Count: > 0 })
                {
                    _items.AddRange(res.Data.Items);
                    _hasMore = res.Data.HasMore;
                }
            });
            StateHasChanged();
            return;
        }

        if (!_jsReady && _items.Count > 0)
        {
            _jsReady = true;
            _dotnetRef = DotNetObjectReference.Create(this);
            await Js.InvokeVoidAsync("highlightReels.init", _containerRef, _dotnetRef);
        }
    }

    [JSInvokable]
    public async Task OnScrollSnap(int index)
    {
        if (index == _currentIndex) return;
        _currentIndex = index;
        StateHasChanged();
        if (_hasMore && !_loadingMore && index >= _items.Count - 3)
        {
            _loadingMore = true;
            var res = await Api.GetAllHighlightsAsync(_items.Count, 10, CancellationToken.None);
            if (res is { Success: true, Data.Items.Count: > 0 })
            {
                _items.AddRange(res.Data.Items);
                _hasMore = res.Data.HasMore;
                StateHasChanged();
                await Js.InvokeVoidAsync("highlightReels.observe", _containerRef);
            }
            _loadingMore = false;
        }
    }

    private string FormatLocal(DateTime utc, string format) =>
        ClientTime.ToLocal(utc).ToString(format);

    private void OnClose() => Nav.NavigateTo("/scores");

    public async ValueTask DisposeAsync()
    {
        try { await Js.InvokeVoidAsync("highlightReels.destroy"); } catch { }
        _dotnetRef?.Dispose();
    }
}
