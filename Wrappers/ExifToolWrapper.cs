using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Wrappers;

public class ExifToolWrapper : IDisposable
{
    #region Fields
    private const string Directory = "Tools";
    private const string ToolName = "exiftool.exe";
    private const string ToolPath = $"{Directory}\\{ToolName}";

    private const string ReadyStatement = "{ready}";
    private const string ExecuteArgument = "-execute";
    private const string StartupArgs = @"-stay_open true -@ - -common_args -charset UTF8 -G1 -args";
    private const string ShutdownArgs = "-stay_open\nfalse";
    private const int ExitTimeout = 10_000;

    private const string DateFormat = "yyyy:MM:dd HH:mm:sszzz";
    private const string DateFormatWithoutTZ = "yyyy:MM:dd HH:mm:ss";

    private readonly Encoding encoding = new UTF8Encoding(false);
    private readonly Process process;
    private readonly StreamWriter writer;
    private readonly StreamReader reader;
    #endregion

    #region Constructors
    static ExifToolWrapper()
    {
        if (!File.Exists(ToolPath))
            throw new FileNotFoundException($"{ToolName} is missing!");
    }
    public ExifToolWrapper()
    {
        (process, writer, reader) = CreateProcess();
    }
    #endregion

    #region Destructor
    ~ExifToolWrapper()
    {
        Dispose(false);
    }
    #endregion

    #region Behavior-Static
    public static string InstaExecute(params string[] args)
    {
        var p = Process.Start(new ProcessStartInfo
        {
            FileName = ToolPath,
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        });

        if (p is null) return string.Empty;

        return p.StandardOutput.ReadToEnd();
    }
    public static string GetVersion()
    {
        return InstaExecute("-ver").Trim();
    }

    public static string FormatDateTime(DateTime dateTime)
    {
        return dateTime.ToString(DateFormat);
    }
    public static bool TryParseDateTime(string text, out DateTime dateTime)
    {
        if (TryParseDateTime(text, DateFormat, out dateTime)
            || TryParseDateTime(text, DateFormatWithoutTZ, out dateTime))
            return true;

        dateTime = default;
        return false;
    }

    private static bool TryParseDateTime(string s, string format, out DateTime result)
    {
        return DateTime.TryParseExact(s, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }
    #endregion

    #region Behavior-Instance
    public string Execute(params string[] args)
    {
        WriteArguments(args);
        return ReadOutput();
    }

    private (Process, StreamWriter, StreamReader) CreateProcess()
    {
        var p = Process.Start(new ProcessStartInfo
        {
            FileName = ToolPath,
            Arguments = StartupArgs,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            StandardOutputEncoding = encoding,
        });

        if (p is null || p.HasExited)
            throw new ApplicationException("Failed to start ExifTool");

        return (p, new StreamWriter(p.StandardInput.BaseStream, encoding), p.StandardOutput);
    }
    private void WriteArguments(params string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        foreach (string arg in args)
            writer.WriteLine(arg);

        writer.WriteLine(ExecuteArgument);
        writer.Flush();
    }
    private string ReadOutput()
    {
        if (process.HasExited) return string.Empty;

        var sb = new StringBuilder();
        while (true)
        {
            var line = reader.ReadLine();

            if (string.IsNullOrWhiteSpace(line)
                || line.StartsWith(ReadyStatement, StringComparison.Ordinal))
                break;

            sb.AppendLine(line);
        }

        return sb.ToString().Trim();
    }
    #endregion

    #region Dispose
    private bool disposed;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    private void Dispose(bool disposing)
    {
        if (disposed) return;
        if (disposing)
        {
            WriteArguments(ShutdownArgs);

            if (!process.WaitForExit(ExitTimeout))
                process.Kill();

            reader.Dispose();
            writer.Dispose();
        }

        disposed = true;
    }
    #endregion
}
