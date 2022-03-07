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
            config.SetApplicationName(name: "dotnet example");
        });

        return await app
            .RunAsync(args: args)
            .ConfigureAwait(continueOnCapturedContext: false);
    }
}

public sealed class DefaultCommand : AsyncCommand<DefaultCommand.Settings>
{
    private readonly IAnsiConsole _console;
    private readonly IEnvironment _environment;
    private readonly IFileSystem _fileSystem;
    private readonly IGlobber _globber;

    public DefaultCommand(IAnsiConsole console)
    {
        _console = console;
        _fileSystem = new FileSystem();
        _environment = new Environment();
        _globber = new Globber(fileSystem: _fileSystem,
            environment: _environment);
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        _console.Clear();
        _console.WriteLine(text: "Hello world");
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
