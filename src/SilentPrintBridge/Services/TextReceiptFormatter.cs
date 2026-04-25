using System.Text;

namespace SilentPrintBridge.Services;

public class TextReceiptFormatter
{
    private readonly ILogger<TextReceiptFormatter> _logger;

    public TextReceiptFormatter(ILogger<TextReceiptFormatter> logger)
    {
        _logger = logger;
    }

    public byte[] FormatTextToEscPos(string text, int charsPerLine, bool appendCut, int feedLines, string encoding = "CP437")
    {
        if (string.IsNullOrEmpty(text))
        {
            _logger.LogWarning("Empty text provided for formatting");
            return Array.Empty<byte>();
        }

        // Normalize line endings
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        // Wrap lines
        var wrappedLines = WrapText(text, charsPerLine);

        // Build ESC/POS
        var builder = new EscPosBuilder();
        builder.Initialize();

        foreach (var line in wrappedLines)
        {
            builder.AppendText(line, encoding).NewLine();
        }

        // Add feed lines before cut
        if (feedLines > 0)
        {
            builder.FeedLines(feedLines);
        }

        // Add cut command if requested
        if (appendCut)
        {
            builder.FullCut();
        }

        return builder.Build();
    }

    private List<string> WrapText(string text, int maxCharsPerLine)
    {
        var result = new List<string>();
        var lines = text.Split('\n');

        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line))
            {
                result.Add("");
                continue;
            }

            if (line.Length <= maxCharsPerLine)
            {
                result.Add(line);
                continue;
            }

            // Wrap long lines
            int startIndex = 0;
            while (startIndex < line.Length)
            {
                int length = Math.Min(maxCharsPerLine, line.Length - startIndex);
                result.Add(line.Substring(startIndex, length));
                startIndex += length;
            }
        }

        return result;
    }

    public string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Remove dangerous control characters but keep newlines and tabs
        var sb = new StringBuilder();
        foreach (char c in text)
        {
            if (c == '\n' || c == '\r' || c == '\t')
            {
                sb.Append(c);
            }
            else if (c >= 32 && c <= 126) // Printable ASCII
            {
                sb.Append(c);
            }
            else if (c >= 160 && c <= 255) // Extended ASCII
            {
                sb.Append(c);
            }
            // Skip other control characters
        }

        return sb.ToString();
    }
}
