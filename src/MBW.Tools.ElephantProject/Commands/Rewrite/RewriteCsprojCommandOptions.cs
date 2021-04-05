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

            return cmd;
        }
    }
}