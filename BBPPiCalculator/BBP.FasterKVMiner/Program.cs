
namespace BBP.FasterKVMiner
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Spectre.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Spectre.Console;
    using Spectre.Console.Cli;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading.Tasks;

    class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var app = new CommandApp<DefaultCommand>();
            app.Configure(config =>
            {
                config.SetApplicationName("dotnet example");
            });

            return await app
                .RunAsync(args)
                .ConfigureAwait(false);
        }
    }

    public sealed class DefaultCommand : AsyncCommand<DefaultCommand.Settings>
    {
        private readonly IAnsiConsole _console;
        private readonly IFileSystem _fileSystem;
        private readonly IEnvironment _environment;
        private readonly IGlobber _globber;

        public sealed class Settings : CommandSettings
        {
            [CommandArgument(0, "[EXAMPLE]")]
            [Description("The example to run.\nIf none is specified, all examples will be listed")]
            public string Name { get; set; }
        }

        public DefaultCommand(IAnsiConsole console)
        {
            _console = console;
            _fileSystem = new FileSystem();
            _environment = new Spectre.IO.Environment();
            _globber = new Globber(_fileSystem, _environment);
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            _console.Clear();
            _console.WriteLine("Hello world");
            return 0;
        }
    }
}
