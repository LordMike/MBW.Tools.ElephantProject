using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using System.Threading.Tasks;
using MBW.Tools.ElephantProject.Attributes;
using MBW.Tools.ElephantProject.Commands;
using MBW.Tools.ElephantProject.Commands.Rewrite;
using MBW.Tools.ElephantProject.Commands.UndoRewrite;
using MBW.Tools.ElephantProject.Commands.UpdateSolution;
using MBW.Tools.ElephantProject.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MBW.Tools.ElephantProject
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            RootCommand rootCommand = new RootCommand();
            rootCommand.AddGlobalOption(new Option(new[] { "-v", "--verbose" }, "Enable verbosity"));

            AddCommand<RewriteCsprojCommandOptions>(rootCommand, RewriteCsprojCommandOptions.GetCommand());
            AddCommand<UndoRewiteCsprojCommandOptions>(rootCommand, UndoRewiteCsprojCommandOptions.GetCommand());
            AddCommand<UpdateSolutionCommandOptions>(rootCommand, UpdateSolutionCommandOptions.GetCommand());

            await rootCommand.InvokeAsync(args);

            return 0;
        }

        private static void AddCommand<TModel>(RootCommand rootCommand, Command newCommand) where TModel : CommandOptionsBase
        {
            newCommand.Handler = CommandHandler.Create(async (TModel model) => await Run(model));
            rootCommand.AddCommand(newCommand);
        }

        private static async Task<int> Run(CommandOptionsBase options)
        {
            Type commandType = options.GetType().GetCustomAttribute<TargetCommandAttribute>().CommandType;

            if (options.Verbose)
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .MinimumLevel.Debug()
                    .CreateLogger();
            }

            IHost host = new HostBuilder()
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.AddSerilog(Log.Logger);
                })
                .ConfigureServices(services =>
                {
                    services
                        .AddSingleton<ProjectStore>()
                        .AddSingleton(options.GetType(), _ => options)
                        .AddSingleton(typeof(CommandBase), commandType);
                })
                .Build();

            CommandBase command = host.Services.GetRequiredService<CommandBase>();

            return await command.Execute();
        }
    }
}
