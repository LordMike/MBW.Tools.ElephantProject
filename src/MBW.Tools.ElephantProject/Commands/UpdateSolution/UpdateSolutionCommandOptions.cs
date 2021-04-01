using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using MBW.Tools.ElephantProject.Attributes;

namespace MBW.Tools.ElephantProject.Commands.UpdateSolution
{
    [TargetCommand(typeof(UpdateSolutionCommand))]
    class UpdateSolutionCommandOptions : CommandOptionsBase
    {
        public DirectoryInfo RootDir { get; set; } = new(Directory.GetCurrentDirectory());
        public IList<string> FileTypes { get; set; } = new List<string> { ".csproj" };
        public IList<string> Include { get; set; } = new List<string> { "**" };
        public IList<string> Exclude { get; set; } = Array.Empty<string>();
        public string SolutionFile { get; set; }

        public static Command GetCommand()
        {
            Command cmd = new Command("sln", "Maintain solution files");

            cmd.AddOption(new Option(new[] { "-d", "--root-dir" }, "Alters the root directory for globbing operations, defaults to the working directory")
            {
                Argument = new Argument<DirectoryInfo>().ExistingOnly()
            });

            cmd.AddOption(new Option("--file-types")
            {
                Argument = new Argument
                {
                    Arity = ArgumentArity.OneOrMore
                },
                Description = "File types to include in the solution, example: '.csproj,.fsproj'"
            });

            cmd.AddOption(new Option(new[] { "-i", "--include" })
            {
                Argument = new Argument
                {
                    Arity = ArgumentArity.OneOrMore
                },
                Description = "Include all projects that match these glob patterns, can be specified multiple times"
            });

            cmd.AddOption(new Option(new[] { "-x", "--exclude" })
            {
                Argument = new Argument
                {
                    Arity = ArgumentArity.OneOrMore
                },
                Description = "Exclude all projects matching these glob patterns, can be specified multiple times"
            });

            cmd.AddArgument(new Argument("solution-file")
            {
                Arity = ArgumentArity.ExactlyOne,
                Description = "Solution file to operate on"
            }.LegalFilePathsOnly());

            return cmd;
        }
    }
}