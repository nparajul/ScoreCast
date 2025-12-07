using ScoreCast.Shared.Enums;

namespace ScoreCast.Models.V1.Responses.Football;

public record PointsTableResult(
    CompetitionFormat Format,
    List<PointsTableGroup> Groups);

public record PointsTableGroup(
    string? GroupName,
    List<PointsTableRow> Rows);
