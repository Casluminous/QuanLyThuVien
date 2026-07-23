using System.Text.RegularExpressions;

namespace QuanLyThuVien.ChatApi;

public static partial class CatalogQueryPlanner
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ai", "bao", "có", "còn", "cho", "cuốn", "của", "đang", "giá", "giúp", "gợi",
        "hãy", "kiểm", "kho", "là", "loại", "mình", "nào", "nhiêu", "quyển", "sách",
        "thể", "tìm", "tôi", "tra", "trong", "về", "xin", "ý"
    };

    public static IReadOnlyList<string> BuildQueries(string message)
    {
        string original = (message ?? string.Empty).Trim();
        if (original.Length == 0) return [];

        var queries = new List<string> { original };
        string cleaned = IntentWordsRegex().Replace(original, " ");
        cleaned = WhiteSpaceRegex().Replace(cleaned, " ").Trim(' ', '?', '.', ',', '!', ':', ';');
        AddUnique(queries, cleaned);

        foreach (string token in TokenSeparatorRegex().Split(original))
        {
            string candidate = token.Trim();
            if (candidate.Length >= 3 && !StopWords.Contains(candidate))
                AddUnique(queries, candidate);
            if (queries.Count >= 6) break;
        }

        return queries;
    }

    public static bool TryGetBookId(string message, out int maSach)
    {
        maSach = 0;
        Match match = BookIdRegex().Match(message ?? string.Empty);
        return match.Success && int.TryParse(match.Groups["id"].Value, out maSach) && maSach > 0;
    }

    private static void AddUnique(List<string> queries, string value)
    {
        if (value.Length > 0 && !queries.Contains(value, StringComparer.OrdinalIgnoreCase))
            queries.Add(value);
    }

    [GeneratedRegex(@"\b(?:gợi\s*ý|giúp\s*(?:tôi|mình)|tìm|kiểm\s*tra|sách|cuốn|quyển|còn\s*(?:trong\s*)?kho|cho\s*(?:tôi|mình)|của|về|xin|hãy)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IntentWordsRegex();

    [GeneratedRegex(@"[^\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenSeparatorRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhiteSpaceRegex();

    [GeneratedRegex(@"(?:mã\s*sách|ma\s*sach|masach)\s*[:#-]?\s*(?<id>\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BookIdRegex();
}
