using System.Drawing;
using System.Drawing.Printing;

namespace SilentPrintBridge.Services;

public class PdfPrinterHelper
{
    public static bool IsPdfPrinter(string printerName)
    {
        return printerName.Contains("PDF", StringComparison.OrdinalIgnoreCase) ||
               printerName.Contains("XPS", StringComparison.OrdinalIgnoreCase);
    }

    public static bool PrintTextToPrinter(string printerName, string text, string jobName)
    {
        try
        {
            var printDocument = new PrintDocument
            {
                PrinterSettings = new PrinterSettings { PrinterName = printerName },
                DocumentName = jobName
            };

            var lines = text.Split('\n');
            var currentLine = 0;

            printDocument.PrintPage += (sender, e) =>
            {
                if (e.Graphics == null) return;

                var font = new Font("Courier New", 10);
                var brush = Brushes.Black;
                float yPos = e.MarginBounds.Top;
                float lineHeight = font.GetHeight(e.Graphics);

                while (currentLine < lines.Length && yPos + lineHeight < e.MarginBounds.Bottom)
                {
                    e.Graphics.DrawString(lines[currentLine], font, brush, e.MarginBounds.Left, yPos);
                    yPos += lineHeight;
                    currentLine++;
                }

                e.HasMorePages = currentLine < lines.Length;
            };

            printDocument.Print();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
