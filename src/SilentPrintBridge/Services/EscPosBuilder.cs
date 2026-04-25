using System.Text;

namespace SilentPrintBridge.Services;

public class EscPosBuilder
{
    private readonly List<byte> _buffer = new();

    // ESC/POS Commands
    private static readonly byte ESC = 0x1B;
    private static readonly byte GS = 0x1D;
    private static readonly byte LF = 0x0A;

    public EscPosBuilder Initialize()
    {
        _buffer.Add(ESC);
        _buffer.Add(0x40); // ESC @ - Initialize printer
        return this;
    }

    public EscPosBuilder AppendText(string text, string encoding = "CP437")
    {
        if (string.IsNullOrEmpty(text))
            return this;

        try
        {
            Encoding enc = encoding.ToUpper() switch
            {
                "CP437" => Encoding.GetEncoding(437),
                "UTF8" or "UTF-8" => Encoding.UTF8,
                "ASCII" => Encoding.ASCII,
                _ => Encoding.GetEncoding(437)
            };

            byte[] bytes = enc.GetBytes(text);
            _buffer.AddRange(bytes);
        }
        catch
        {
            // Fallback to ASCII
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            _buffer.AddRange(bytes);
        }

        return this;
    }

    public EscPosBuilder NewLine()
    {
        _buffer.Add(LF);
        return this;
    }

    public EscPosBuilder FeedLines(int lines)
    {
        for (int i = 0; i < lines; i++)
        {
            _buffer.Add(LF);
        }
        return this;
    }

    public EscPosBuilder AlignLeft()
    {
        _buffer.Add(ESC);
        _buffer.Add(0x61); // ESC a
        _buffer.Add(0x00); // Left
        return this;
    }

    public EscPosBuilder AlignCenter()
    {
        _buffer.Add(ESC);
        _buffer.Add(0x61); // ESC a
        _buffer.Add(0x01); // Center
        return this;
    }

    public EscPosBuilder AlignRight()
    {
        _buffer.Add(ESC);
        _buffer.Add(0x61); // ESC a
        _buffer.Add(0x02); // Right
        return this;
    }

    public EscPosBuilder BoldOn()
    {
        _buffer.Add(ESC);
        _buffer.Add(0x45); // ESC E
        _buffer.Add(0x01); // Bold on
        return this;
    }

    public EscPosBuilder BoldOff()
    {
        _buffer.Add(ESC);
        _buffer.Add(0x45); // ESC E
        _buffer.Add(0x00); // Bold off
        return this;
    }

    public EscPosBuilder FullCut()
    {
        _buffer.Add(GS);
        _buffer.Add(0x56); // GS V
        _buffer.Add(0x00); // Full cut
        return this;
    }

    public EscPosBuilder PartialCut()
    {
        _buffer.Add(GS);
        _buffer.Add(0x56); // GS V
        _buffer.Add(0x01); // Partial cut
        return this;
    }

    public EscPosBuilder OpenDrawer()
    {
        _buffer.Add(ESC);
        _buffer.Add(0x70); // ESC p
        _buffer.Add(0x00); // Drawer pin 2
        _buffer.Add(0x19); // On time
        _buffer.Add(0x19); // Off time
        return this;
    }

    public byte[] Build()
    {
        return _buffer.ToArray();
    }

    public static byte[] BuildTestReceipt(string serviceName, string printerName, int widthMm, string encoding = "CP437")
    {
        var builder = new EscPosBuilder();
        builder.Initialize();

        builder.AlignCenter()
               .BoldOn()
               .AppendText(serviceName, encoding)
               .BoldOff()
               .NewLine()
               .NewLine();

        builder.AlignLeft()
               .AppendText("Test Receipt", encoding)
               .NewLine()
               .AppendText($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", encoding)
               .NewLine()
               .AppendText($"Printer: {printerName}", encoding)
               .NewLine()
               .AppendText($"Width: {widthMm}mm", encoding)
               .NewLine()
               .NewLine();

        builder.AppendText("--------------------------------", encoding)
               .NewLine();

        builder.AppendText("Item 1", encoding)
               .NewLine()
               .AppendText("Item 2", encoding)
               .NewLine()
               .AppendText("Item 3", encoding)
               .NewLine();

        builder.AppendText("--------------------------------", encoding)
               .NewLine();

        builder.AlignRight()
               .BoldOn()
               .AppendText("Total: $0.00", encoding)
               .BoldOff()
               .NewLine();

        builder.AlignCenter()
               .NewLine()
               .AppendText("Thank you!", encoding)
               .NewLine();

        return builder.Build();
    }
}
