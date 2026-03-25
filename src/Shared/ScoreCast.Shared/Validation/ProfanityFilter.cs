using System.Reflection;
using System.Text.RegularExpressions;

namespace ScoreCast.Shared.Validation;

public static partial class ProfanityFilter
{
    private static readonly Lazy<HashSet<string>> Blocked = new(LoadBlockedWords);

    // Legitimate names and places that contain false positives
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "dickens", "dickson", "dickenson", "dickinson",
        "hancock", "hitchcock", "cockburn", "cockroft", "cockerill",
        "scunthorpe", "penistone", "shitterton", "clitheroe", "lightwater",
        "sussex", "essex", "middlesex", "wessex",
        "arsenal", "tit", "titov", "titus",
        "fanny", "fannie", "willy", "willie", "roger", "randy",
        "cox", "cummings", "cummins", "kuntz", "sexton", "hooker",
        "beaver", "ball", "balls", "butt", "butts", "wang", "dong",
        "gaylord", "gay", "dyke"
    };

    public static bool ContainsProfanity(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        var cleaned = CleanRegex().Replace(input.ToLowerInvariant(), "");

        // If the entire input is an allowed name, it's fine
        if (Allowed.Contains(cleaned)) return false;

        // Split into words and check each
        var words = WordSplitRegex().Split(cleaned);
        return words.Any(w => w.Length > 1 && Blocked.Value.Contains(w) && !Allowed.Contains(w));
    }

    private static HashSet<string> LoadBlockedWords()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("ScoreCast.Shared.Validation.profanity-en.txt");
        if (stream is null) return [];

        using var reader = new StreamReader(stream);
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 1) words.Add(trimmed);
        }
        return words;
    }

    [GeneratedRegex(@"[^a-z\s]")]
    private static partial Regex CleanRegex();

    [GeneratedRegex(@"[\s_\-]+")]
    private static partial Regex WordSplitRegex();
}
