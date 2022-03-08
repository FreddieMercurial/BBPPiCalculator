using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.IO;
using Environment = Spectre.IO.Environment;

namespace BBP.FasterKVMiner;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        var app = new CommandApp<DefaultCommand>();
        app.Configure(configuration: config =>
        {
            config.SetApplicationName(name: "Bailey-Borwein-Plouffe Pi Miner");
        });

        return await app
            .RunAsync(args: args)
            .ConfigureAwait(continueOnCapturedContext: false);
    }
}

public sealed class DefaultCommand : AsyncCommand<DefaultCommand.Settings>, IDisposable
{
    private readonly IEnvironment _environment;
    private readonly IFileSystem _fileSystem;
    private readonly Tracker _tracker;

    public DefaultCommand(IAnsiConsole console)
    {
        _fileSystem = new FileSystem();
        _environment = new Environment();

        _tracker = new Tracker(baseDirectory: System.Environment.CurrentDirectory);
    }

    public void Dispose()
    {
        _tracker.Dispose();
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        Console.Clear();
        Console.WriteLine(value: "Beginning execution...");
        await foreach (var workBlock in _tracker.Run())
        {
            Console.WriteLine(value: $"Block emitted: @{workBlock.StartingOffset}");
        }

        return 0;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(position: 0,
            template: "[EXAMPLE]")]
        [Description(description: "The example to run.\nIf none is specified, all examples will be listed")]
        public string Name { get; set; }
    }
}
