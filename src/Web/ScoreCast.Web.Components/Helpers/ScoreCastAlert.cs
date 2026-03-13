using OneOf;

namespace ScoreCast.Web.Components.Helpers;

public class ScoreCastAlert
{
    public OneOf<MarkupString, RenderFragment> Content { get; set; }
    public Severity Severity { get; set; }
    public required string Key { get; set; }
}
