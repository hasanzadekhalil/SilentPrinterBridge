using System.Text;

namespace SilentPrintBridge.Utils;

public static class EncodingHelper
{
    public static Encoding GetEncoding(string encodingName)
    {
        try
        {
            return encodingName.ToUpper() switch
            {
                "CP437" => Encoding.GetEncoding(437),
                "UTF8" or "UTF-8" => Encoding.UTF8,
                "ASCII" => Encoding.ASCII,
                "CP850" => Encoding.GetEncoding(850),
                "CP852" => Encoding.GetEncoding(852),
                "CP858" => Encoding.GetEncoding(858),
                "CP1252" => Encoding.GetEncoding(1252),
                _ => Encoding.GetEncoding(437) // Default to CP437
            };
        }
        catch
        {
            return Encoding.ASCII; // Fallback
        }
    }

    public static bool IsValidBase64(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
            return false;

        try
        {
            Convert.FromBase64String(base64String);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
