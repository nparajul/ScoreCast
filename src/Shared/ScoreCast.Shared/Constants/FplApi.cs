namespace ScoreCast.Shared.Constants;

public static class FplApi
{
    public const string BaseUrlKey = "ApiSettings:FplApi:BaseUrl";

    public static class Routes
    {
        public const string BootstrapStatic = "bootstrap-static/";
        public const string Fixtures = "fixtures/";
        public const string FixturesByEvent = "fixtures/?event={0}";
    }
}
