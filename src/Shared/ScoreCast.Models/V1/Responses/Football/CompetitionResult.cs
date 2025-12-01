namespace ScoreCast.Models.V1.Responses.Football;

public record CompetitionResult(
    long Id, string Name, string Code, string? LogoUrl,
    string CountryName, string? CountryFlagUrl);
