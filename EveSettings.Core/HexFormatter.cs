using System.Globalization;
using System.Text;

namespace EveSettings.Core;

public static class HexFormatter
{
    public const int DefaultBytesPerLine = 16;
    public const long DefaultMaxBytes = 4 * 1024 * 1024;

    /// <summary>
    /// Formats up to <paramref name="maxBytes"/> of <paramref name="data"/> as hex lines.
    /// </summary>
    public static IReadOnlyList<string> FormatLines(ReadOnlySpan<byte> data, int bytesPerLine = DefaultBytesPerLine,
        long maxBytes = DefaultMaxBytes)
    {
        var lines = new List<string>();
        var len = (int)Math.Min(data.Length, maxBytes);
        var sb = new StringBuilder(bytesPerLine * 3 + 32);
        for (var offset = 0; offset < len; offset += bytesPerLine)
        {
            sb.Clear();
            var rowLen = Math.Min(bytesPerLine, len - offset);
            var slice = data.Slice(offset, rowLen);

            sb.Append(offset.ToString("X8", CultureInfo.InvariantCulture));
            sb.Append("  ");

            for (var i = 0; i < rowLen; i++)
            {
                sb.Append(slice[i].ToString("X2", CultureInfo.InvariantCulture));
                if (i + 1 < rowLen)
                    sb.Append(' ');
            }

            if (rowLen < bytesPerLine)
                sb.Append(' ', (bytesPerLine - rowLen) * 3);

            sb.Append("  |");
            for (var i = 0; i < rowLen; i++)
            {
                var b = slice[i];
                sb.Append(b is >= 32 and <= 126 ? (char)b : '.');
            }

            sb.Append('|');
            lines.Add(sb.ToString());
        }

        return lines;
    }
}
