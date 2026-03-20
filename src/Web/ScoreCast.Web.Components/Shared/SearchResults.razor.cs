using static ScoreCast.Web.Components.Shared.GlobalSearch;

namespace ScoreCast.Web.Components.Shared;

public partial class SearchResults
{
    [Parameter] public bool Loading { get; set; }
    [Parameter] public List<SearchItem> Filtered { get; set; } = [];
    [Parameter] public List<SearchItem> Suggestions { get; set; } = [];
    [Parameter] public string? Query { get; set; }
    [Parameter] public EventCallback<SearchItem> OnNavigate { get; set; }
}
