using System.CommandLine;
using System.CommandLine.IO;
using System.Text.Json;

namespace GitForest.Cli;

public sealed class Output
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IStandardStreamWriter _out;
    private readonly IStandardStreamWriter _error;

    public bool Json { get; }

    private Output(IStandardStreamWriter @out, IStandardStreamWriter error, bool json)
    {
        _out = @out;
        _error = error;
        Json = json;
    }

    public static Output From(IConsole console, bool json)
    {
        return new Output(console.Out, console.Error, json);
    }

    public void WriteLine(string message)
    {
        _out.Write(message);
        _out.Write(Environment.NewLine);
    }

    public void WriteErrorLine(string message)
    {
        _error.Write(message);
        _error.Write(Environment.NewLine);
    }

    public void WriteJson<T>(T value)
    {
        _out.Write(JsonSerializer.Serialize(value, JsonOptions));
        _out.Write(Environment.NewLine);
    }

    public void WriteJsonError(string code, string message, object? details = null)
    {
        WriteJson(new
        {
            error = new
            {
                code,
                message,
                details
            }
        });
    }
}


