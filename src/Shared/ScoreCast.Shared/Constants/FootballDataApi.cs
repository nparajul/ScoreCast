namespace ScoreCast.Shared.Constants;

public static class FootballDataApi
{
    public const string AuthHeader = "X-Auth-Token";
    public const string BaseUrlKey = "ApiSettings:FootballDataApi:BaseUrl";
    public const string ApiKeyKey = "ApiSettings:FootballDataApi:ApiKey";

    public static class Routes
    {
        public const string Competition = "competitions/{0}";
        public const string Teams = "competitions/{0}/teams";
        public const string Matches = "competitions/{0}/matches?season={1}";
    }

    public static class Status
    {
        public const string Finished = "FINISHED";
        public const string InPlay = "IN_PLAY";
        public const string Paused = "PAUSED";
        public const string Live = "LIVE";
        public const string Postponed = "POSTPONED";
        public const string Suspended = "SUSPENDED";
        public const string Cancelled = "CANCELLED";
    }
}
