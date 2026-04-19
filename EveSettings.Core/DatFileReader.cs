namespace EveSettings.Core;

public static class DatFileReader
{
    public const long DefaultMaxReadBytes = 16 * 1024 * 1024;

    public static byte[] ReadFile(string path, long maxBytes = DefaultMaxReadBytes)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        if (fs.Length > maxBytes)
            throw new IOException($"File is {fs.Length} bytes; max supported read is {maxBytes} bytes.");

        var buffer = new byte[fs.Length];
        var read = fs.Read(buffer, 0, buffer.Length);
        if (read != buffer.Length)
            throw new IOException("Could not read the full file.");

        return buffer;
    }
}
