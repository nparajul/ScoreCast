namespace ScoreCast.Web.Pages;

public partial class Home
{
    [CascadingParameter(Name = "IsDarkMode")]
    private bool IsDarkMode { get; set; }
}
