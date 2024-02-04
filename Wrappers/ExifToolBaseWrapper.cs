using System.Diagnostics;
using System.Text;

namespace Wrappers;

public class ExifToolBaseWrapper
{
    #region Fields
    private const string ToolsDirectory = "Tools";
    private const string ExifTool = "exiftool.exe";

    protected string ExifToolPath { get; private init; } = Path.Combine(ToolsDirectory, ExifTool);
    public string Version { get; private init; } = "0.0.0";
    #endregion

    #region Constructors
    public ExifToolBaseWrapper()
    {
        if (!File.Exists(ExifToolPath))
            throw new FileNotFoundException($"{ExifTool} is missing!");

        Version = InstantExecute("-ver").Trim();
    }
    #endregion

    #region Behavior
    public virtual string Execute(params string[] args) => InstantExecute(args);
    private string InstantExecute(params string[] args)
    {
        var p = Process.Start(new ProcessStartInfo
        {
            FileName = ExifToolPath,
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        });

        if (p is null) return string.Empty;

        var builder = new StringBuilder();
        while (!p.StandardOutput.EndOfStream)
        {
            var line = p.StandardOutput.ReadLine();
            builder.AppendLine(line);
        }

        return builder.ToString();
    }
    #endregion
}
