using Microsoft.AspNetCore.Components;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;

namespace ScoreCast.Web.Components.Shared;

public static class MatchEventHelpers
{
    public const string YellowCardHtml = "<span style=\"display:inline-block;width:8px;height:11px;background:#fdd835;border-radius:1px;vertical-align:middle;\"></span>";
    public const string RedCardHtml = "<span style=\"display:inline-block;width:8px;height:11px;background:#d32f2f;border-radius:1px;vertical-align:middle;\"></span>";
    public const string AssistHtml = "👟";
    public const string PenaltyMissedHtml = "<svg style=\"display:inline-block;vertical-align:middle;\" width=\"14\" height=\"14\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\"><rect x=\"3\" y=\"4\" width=\"18\" height=\"14\" rx=\"0\" fill=\"none\"/><line x1=\"9\" y1=\"8\" x2=\"15\" y2=\"14\"/><line x1=\"15\" y1=\"8\" x2=\"9\" y2=\"14\"/></svg>";

    public static string FormatEvent(MatchEventDetail e) => e.EventType switch
    {
        EventTypes.Goal => e.Value > 1 ? $"⚽ x{e.Value}" : "⚽",
        EventTypes.PenaltyGoal => "⚽ (P)",
        EventTypes.OwnGoal => "⚽ (OG)",
        EventTypes.Assist => AssistHtml,
        EventTypes.YellowCard => YellowCardHtml,
        EventTypes.RedCard => RedCardHtml,
        EventTypes.PenaltySaved => "🧤",
        EventTypes.PenaltyMissed => PenaltyMissedHtml,
        _ => ""
    };

    public record DisplayLine(MarkupString Markup, string? Minute, double SortKey, bool Bold);

    public static List<DisplayLine> GetDisplayLines(List<MatchEventDetail> events, bool isHome, bool includeSubs = true)
    {
        var lines = new List<DisplayLine>();
        foreach (var e in events.Where(e => e.IsHome == isHome && e.EventType is not EventTypes.SubIn and not EventTypes.SubOut))
        {
            var isGoal = e.EventType is EventTypes.Goal or EventTypes.PenaltyGoal or EventTypes.OwnGoal;
            var text = isHome ? $"{e.PlayerName} {FormatEvent(e)}" : $"{FormatEvent(e)} {e.PlayerName}";
            lines.Add(new DisplayLine(new MarkupString(text), e.Minute, ParseMinute(e.Minute), isGoal));
        }
        if (includeSubs)
            foreach (var s in GetSubPairs(events, isHome))
                lines.Add(new DisplayLine(
                    new MarkupString($"<span style=\"color:#4caf50;\">▲</span> {s.PlayerOn} <span style=\"color:#f44336;\">▼</span> {s.PlayerOff}"),
                    s.Minute, ParseMinute(s.Minute), false));
        return lines.OrderBy(l => l.SortKey).ToList();
    }

    public static double ParseMinute(string? minute)
    {
        if (minute is null) return 999;
        var clean = minute.Replace("'", "").Replace(" ", "");
        var parts = clean.Split('+');
        if (double.TryParse(parts[0], out var main))
            return parts.Length > 1 && double.TryParse(parts[1], out var added) ? main + added * 0.01 : main;
        return 999;
    }

    private record SubPair(string PlayerOn, string PlayerOff, string? Minute);

    private static List<SubPair> GetSubPairs(List<MatchEventDetail> events, bool isHome)
    {
        var subs = events.Where(e => e.IsHome == isHome).ToList();
        var subIns = subs.Where(e => e.EventType == EventTypes.SubIn).ToList();
        var subOffs = subs.Where(e => e.EventType == EventTypes.SubOut).ToList();
        return subIns.Select(si =>
        {
            var off = subOffs.FirstOrDefault(so => so.Minute == si.Minute);
            if (off is not null) subOffs.Remove(off);
            return new SubPair(si.PlayerName, off?.PlayerName ?? "", si.Minute);
        }).ToList();
    }
}
