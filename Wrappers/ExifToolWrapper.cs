﻿using System.Diagnostics;
using System.Globalization;
using System.Text;
using Wrappers.DTO;

namespace Wrappers;

public sealed partial class ExifToolWrapper : IDisposable
{
    #region Fields
    private const string ToolName = "exiftool.exe";
    private const string ToolPath = $"Tools\\{ToolName}";

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

    private readonly bool OverwriteOriginalFile;
    private readonly bool AttemptToFixIncorrectOffsets;
    private readonly bool IgnoreMinorErrorsAndWarnings;
    #endregion

    #region Constructors
    static ExifToolWrapper()
    {
        if (!File.Exists(ToolPath))
            throw new FileNotFoundException($"{ToolName} is missing!");
    }
    public ExifToolWrapper(
        bool overwriteOriginalFile = false,
        bool attemptToFixIncorrectOffsets = false,
        bool ignoreMinorErrorsAndWarnings = false)
    {
        OverwriteOriginalFile = overwriteOriginalFile;
        AttemptToFixIncorrectOffsets = attemptToFixIncorrectOffsets;
        IgnoreMinorErrorsAndWarnings = ignoreMinorErrorsAndWarnings;

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
    private static ExifResult BuildResult(string output)
    {
        #region Constants
        const char LineSeparator = '\n';
        const char ExifTagStart = '-';
        const char EqualsChar = '=';
        const char TagSeparator = ':';
        const char StatusSeparator = ' ';

        const string updateMessage = "image files updated";
        const string errorMessage = "files weren't updated due to errors";
        const string couldReadMessage = "image files read";
        const string couldNotReadMessage = "files could not be read";
        const string unchanged = "image files unchanged";

        const StringComparison comp = StringComparison.OrdinalIgnoreCase;
        #endregion

        var updates = 0;
        var errors = 0;

        var tags = new Dictionary<string, string>();
        foreach (var line in output.Split(LineSeparator))
            if (line.StartsWith(ExifTagStart))
            {
                var equal = line.IndexOf(EqualsChar);
                var keyStart = line.IndexOf(TagSeparator);
                var key = line.Substring(keyStart + 1, equal - keyStart - 1);

                tags.Add(key, line.Substring(equal + 1).Trim());
            }

            else if (line.Contains(StatusSeparator, comp))
            {
                var index = line.IndexOf(StatusSeparator);
                var value = line.Substring(0, index + 1);

                if (int.TryParse(value, out int number))
                {
                    var message = line.Substring(index + 1);
                    switch (message)
                    {
                        case couldReadMessage: updates += number; break;
                        case couldNotReadMessage: errors += number; break;
                        case updateMessage: updates += number; break;
                        case errorMessage: errors += number; break;
                        case unchanged: errors += number; break;
                        default: break;
                    }
                }
            }
#if DEBUG
            else
            {

            }
#endif

        return new ExifResult(output, tags, updates, errors);
    }
    #endregion

    #region Exif
    public static ExifDeleteOriginalResult DeleteOriginal(params string[] pathes)
    {
        if (pathes is null) return default;

        const string command = "-overwrite_original -delete_original! -r";
        var outputs = pathes.Select(path => InstaExecute(command, path));

        const string directoryMessage = "directories scanned";
        const string filesMessage = "image files found";
        const string originalsMessage = "original files deleted";

        long directories = 0, images = 0, originals = 0;
        var separators = new char[] { '\r', '\n' };

        foreach (var output in outputs)
            foreach (var line in output.Split(separators, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(' ');
                if (parts.Length == default
                    || !int.TryParse(parts[0], out int value))
                    continue;

                var message = line.Remove(0, parts[0].Length + 1);
                switch (message)
                {
                    case directoryMessage: directories += value; break;
                    case filesMessage: images += value; break;
                    case originalsMessage: originals += value; break;
                }
            }

        return new ExifDeleteOriginalResult(directories, images, originals);
    }
    #endregion

    #region Behavior-Instance
    public string Execute(params string[] args)
    {
        WriteArguments(args);
        return ReadOutput();
    }
    public Dictionary<string, string> ReadMetadata(string path)
    {
        return Read(new List<string> { path }).Tags;
    }
    public bool TryWriteMetadata(string path, Dictionary<string, string> tags)
    {
        var args = new List<string> { path };
        args.AddRange(tags.Select(i => $"-{i.Key}={i.Value}"));

        return TryWrite(args);
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

    private ExifResult Read(List<string> args)
    {
        AppendArgumentsBySettings(ref args, false);

        return BuildResult(Execute(args.ToArray()));
    }
    private bool TryWrite(List<string> args)
    {
        AppendArgumentsBySettings(ref args, true);

        var r = BuildResult(Execute(args.ToArray()));
        return r.Updates > 0 && r.Errors == default;
    }
    private void AppendArgumentsBySettings(ref List<string> args, bool isWrite)
    {
        if (IgnoreMinorErrorsAndWarnings) args.Add("-m");

        if (isWrite)
        {
            if (OverwriteOriginalFile) args.Add("-overwrite_original");
            if (AttemptToFixIncorrectOffsets) args.Add("-F");
        }
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
