using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using MBW.Tools.ElephantProject.Attributes;

namespace MBW.Tools.ElephantProject.Commands.Rewrite
{
    [TargetCommand(typeof(RewriteCsprojCommand))]
    class RewriteCsprojCommandOptions : CommandOptionsBase
    {
        public DirectoryInfo RootDir { get; set; } = new(Directory.GetCurrentDirectory());
        public IList<string> Include { get; set; } = new List<string> { "**" };
        public IList<string> Exclude { get; set; } = Array.Empty<string>();
        public RewriteCsprojStrategy Strategy { get; set; } = RewriteCsprojStrategy.ItemGroup;

        public static Command GetCommand()
        {
            Command cmd = new Command("rewrite", "Rewrite project files, replacing package references");

            cmd.AddOption(new Option(new[] { "-d", "--root-dir" }, "Root directory to search in, defaults to the working directory")
            {
                Argument = new Argument<DirectoryInfo>
                {
                    Arity = ArgumentArity.ExactlyOne
                }.ExistingOnly()
            });

            cmd.AddOption(new Option(new[] { "-i", "--include" })
            {
                Argument = new Argument
                {
                    Arity = ArgumentArity.OneOrMore
                },
                Description = "Rewrite all projects that match these glob patterns, can be specified multiple times"
            });

            cmd.AddOption(new Option(new[] { "-x", "--exclude" })
            {
                Argument = new Argument
                {
                    Arity = ArgumentArity.OneOrMore
                },
                Description = "Exclude all projects matching these glob patterns, can be specified multiple times"
            });

            cmd.AddOption(new Option(new[] { "-s", "--strategy" })
            {
                Argument = new Argument
                {
                    Arity = ArgumentArity.ZeroOrOne
                },
                Description = "Rewriting strategy. '" + nameof(RewriteCsprojStrategy.ItemGroup) + "' adds an ItemGroup to each csproj file, with changes. '" + nameof(RewriteCsprojStrategy.BuildProps) + "' constructs or changes a Directory.Build.props in the root directory."
            });

            return cmd;
        }
    }
}