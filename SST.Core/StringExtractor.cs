using System.Text;

namespace SST.Core;

public static class StringExtractor
{
    public const int DefaultMaxResults = 5000;

    /// <summary>
    /// Extracts printable UTF-8 runs and UTF-16 LE (2-byte aligned) runs of at least <paramref name="minLength"/> characters.
    /// </summary>
    public static IReadOnlyList<ExtractedString> Extract(
        ReadOnlySpan<byte> data,
        int minLength = 4,
        int maxResults = DefaultMaxResults)
    {
        var results = new List<ExtractedString>();
        ExtractUtf8(data, minLength, maxResults, results);
        if (results.Count >= maxResults)
            return results;

        ExtractUtf16LeAligned(data, minLength, maxResults, results);
        results.Sort((a, b) => a.Offset.CompareTo(b.Offset));
        return results.Count <= maxResults ? results : results.Take(maxResults).ToList();
    }

    private static void ExtractUtf8(ReadOnlySpan<byte> data, int minLength, int maxResults, List<ExtractedString> results)
    {
        var runStart = -1;
        for (var i = 0; i < data.Length && results.Count < maxResults; i++)
        {
            var b = data[i];
            var printable = b is (>= 32 and <= 126) or 9 or 10 or 13;

            if (printable)
            {
                runStart = runStart < 0 ? i : runStart;
            }
            else if (runStart >= 0)
            {
                var len = i - runStart;
                if (len >= minLength)
                {
                    var text = Encoding.UTF8.GetString(data.Slice(runStart, len));
                    results.Add(new ExtractedString(runStart, "UTF-8", text));
                }

                runStart = -1;
            }
        }

        if (runStart >= 0 && data.Length - runStart >= minLength && results.Count < maxResults)
        {
            var text = Encoding.UTF8.GetString(data.Slice(runStart));
            results.Add(new ExtractedString(runStart, "UTF-8", text));
        }
    }

    private static void ExtractUtf16LeAligned(
        ReadOnlySpan<byte> data,
        int minLength,
        int maxResults,
        List<ExtractedString> results)
    {
        for (var o = 0; o + 1 < data.Length && results.Count < maxResults; o += 2)
        {
            var charCount = 0;
            for (var j = o; j + 1 < data.Length; j += 2)
            {
                var c = (char)(data[j] | (data[j + 1] << 8));
                if (!IsPrintableUtf16Bmp(c))
                    break;
                charCount++;
            }

            if (charCount < minLength)
                continue;

            var byteLen = charCount * 2;
            TryAddUtf16Le(data, o, byteLen, results);
            // Skip past this run to reduce duplicates from overlapping scans.
            o += byteLen - 2;
        }
    }

    private static bool IsPrintableUtf16Bmp(char c)
    {
        if (c == '\0')
            return false;
        if (char.IsControl(c))
            return false;
        if (c is '\uFFFE' or '\uFFFF')
            return false;
        return true;
    }

    private static void TryAddUtf16Le(ReadOnlySpan<byte> data, int offset, int byteLength, List<ExtractedString> results)
    {
        try
        {
            var text = Encoding.Unicode.GetString(data.Slice(offset, byteLength)).Trim('\0');
            if (text.Length < 4)
                return;

            results.Add(new ExtractedString(offset, "UTF-16LE", text));
        }
        catch
        {
            // ignore invalid slices
        }
    }
}

public sealed record ExtractedString(int Offset, string EncodingName, string Text);
