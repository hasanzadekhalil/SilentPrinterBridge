using System.Runtime.InteropServices;

namespace SilentPrintBridge.Services;

public class Win32RawPrinter
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DOCINFOW
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pDocName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pDataType;
    }

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool OpenPrinterW(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int StartDocPrinterW(IntPtr hPrinter, int level, ref DOCINFOW pDocInfo);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBuf, int cbBuf, out int pcWritten);

    private readonly ILogger<Win32RawPrinter> _logger;

    public Win32RawPrinter(ILogger<Win32RawPrinter> logger)
    {
        _logger = logger;
    }

    public class PrintResult
    {
        public bool Success { get; set; }
        public int BytesWritten { get; set; }
        public int WindowsErrorCode { get; set; }
        public string Message { get; set; } = "";
    }

    public PrintResult SendBytesToPrinter(string printerName, byte[] data, string jobName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            return new PrintResult
            {
                Success = false,
                Message = "Printer name is empty"
            };
        }

        if (data == null || data.Length == 0)
        {
            return new PrintResult
            {
                Success = false,
                Message = "Data is empty"
            };
        }

        IntPtr hPrinter = IntPtr.Zero;
        IntPtr pUnmanagedBytes = IntPtr.Zero;

        try
        {
            // Open printer
            if (!OpenPrinterW(printerName, out hPrinter, IntPtr.Zero))
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogError("OpenPrinter failed for '{PrinterName}'. Error code: {ErrorCode}", printerName, error);
                return new PrintResult
                {
                    Success = false,
                    WindowsErrorCode = error,
                    Message = $"Failed to open printer '{printerName}'. Error code: {error}"
                };
            }

            // Start document
            var docInfo = new DOCINFOW
            {
                pDocName = jobName,
                pOutputFile = null,
                pDataType = "RAW"
            };

            int jobId = StartDocPrinterW(hPrinter, 1, ref docInfo);
            if (jobId == 0)
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogError("StartDocPrinter failed. Error code: {ErrorCode}", error);
                return new PrintResult
                {
                    Success = false,
                    WindowsErrorCode = error,
                    Message = $"Failed to start print job. Error code: {error}"
                };
            }

            // Start page
            if (!StartPagePrinter(hPrinter))
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogError("StartPagePrinter failed. Error code: {ErrorCode}", error);
                EndDocPrinter(hPrinter);
                return new PrintResult
                {
                    Success = false,
                    WindowsErrorCode = error,
                    Message = $"Failed to start page. Error code: {error}"
                };
            }

            // Write data
            pUnmanagedBytes = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, pUnmanagedBytes, data.Length);

            int bytesWritten = 0;
            if (!WritePrinter(hPrinter, pUnmanagedBytes, data.Length, out bytesWritten))
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogError("WritePrinter failed. Error code: {ErrorCode}", error);
                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
                return new PrintResult
                {
                    Success = false,
                    WindowsErrorCode = error,
                    Message = $"Failed to write data to printer. Error code: {error}"
                };
            }

            if (bytesWritten != data.Length)
            {
                _logger.LogWarning("WritePrinter wrote {BytesWritten} bytes but expected {ExpectedBytes}", bytesWritten, data.Length);
            }

            // End page
            if (!EndPagePrinter(hPrinter))
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogWarning("EndPagePrinter failed. Error code: {ErrorCode}", error);
            }

            // End document
            if (!EndDocPrinter(hPrinter))
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogWarning("EndDocPrinter failed. Error code: {ErrorCode}", error);
            }

            _logger.LogInformation("Successfully sent {ByteCount} bytes to printer '{PrinterName}' (Job: {JobName})", bytesWritten, printerName, jobName);

            return new PrintResult
            {
                Success = true,
                BytesWritten = bytesWritten,
                Message = $"Successfully printed {bytesWritten} bytes"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while printing to '{PrinterName}'", printerName);
            return new PrintResult
            {
                Success = false,
                Message = $"Exception: {ex.Message}"
            };
        }
        finally
        {
            if (pUnmanagedBytes != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pUnmanagedBytes);
            }

            if (hPrinter != IntPtr.Zero)
            {
                ClosePrinter(hPrinter);
            }
        }
    }
}
