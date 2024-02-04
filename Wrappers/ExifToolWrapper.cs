using System.Diagnostics;
using System.Text;

namespace Wrappers;

public sealed class ExifToolWrapper : ExifToolBaseWrapper
{
    #region Fields
    private const string ExifReadyStatement = "{ready}";
    private const string ExifArgsExecuteTag = "-execute";
    private const string ExifStartupArgs = @"-stay_open true -@ - -common_args -charset UTF8 -G1 -args";
    private const string ExifShutdownArgs = "-stay_open\nfalse";
    private const int ExifExitTimeout = 10_000;

    private readonly Encoding encoding = new UTF8Encoding(false);
    private readonly Process process;
    private readonly StreamWriter writer;
    private readonly StreamReader reader;
    #endregion

    #region Constructors
    public ExifToolWrapper() : base()
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

    #region Behavior-Instance
    public override string Execute(params string[] args) => ContinuousExecute(args);
    private string ContinuousExecute(params string[] args)
    {
        WriteArguments(args);
        return ReadOutput();
    }

    private (Process, StreamWriter, StreamReader) CreateProcess()
    {
        var p = Process.Start(new ProcessStartInfo
        {
            FileName = ExifToolPath,
            Arguments = ExifStartupArgs,
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

        writer.WriteLine(ExifArgsExecuteTag);
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
                || line.StartsWith(ExifReadyStatement, StringComparison.Ordinal))
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
            WriteArguments(ExifShutdownArgs);

            if (!process.WaitForExit(ExifExitTimeout))
                process.Kill();

            reader.Dispose();
            writer.Dispose();
        }

        disposed = true;
    }
    #endregion
}
