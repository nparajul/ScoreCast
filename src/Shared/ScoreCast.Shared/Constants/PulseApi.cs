namespace ScoreCast.Shared.Constants;

public static class PulseApi
{
    public const string BaseUrlKey = "ApiSettings:PulseApi:BaseUrl";

    public static class Routes
    {
        public const string Fixture = "football/fixtures/{0}";
        public const string FixturesByStatus = "football/fixtures?comps={0}&pageSize=20&sort=desc&statuses={1}";
        public const string FixturesByCompSeason = "football/fixtures?compSeasons={0}&pageSize=400&sort=asc";
        public const string Standings = "football/standings?compSeasons={0}&live=true";
        public const string TeamsByCompSeason = "football/compseasons/{0}/teams";
    }

    public static class Status
    {
        public const string Live = "L";
        public const string Complete = "C";
        public const string Upcoming = "U";
    }

    public static class Phase
    {
        public const string HalfTime = "H";
        public const string FirstHalf = "1";
        public const string SecondHalf = "2";
    }

    public static class DisplayLabels
    {
        public const string HalfTime = "HT";
        public const string FullTime = "FT";
    }
}
