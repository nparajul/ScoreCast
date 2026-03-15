namespace ScoreCast.Web.Components;

public abstract class ScoreCastComponentBase : ComponentBase
{
    [CascadingParameter(Name = "IsMobile")] public bool IsMobile { get; set; }
}
