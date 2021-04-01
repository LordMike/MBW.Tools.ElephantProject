﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MBW.Tools.ElephantProject.Helpers;
using Microsoft.Build.Construction;
using Serilog;

namespace MBW.Tools.ElephantProject.Commands.UpdateSolution
{
    class UpdateSolutionCommand : CommandBase
    {
        private readonly UpdateSolutionCommandOptions _options;
        private readonly ProjectStore _projectStore;

        public UpdateSolutionCommand(UpdateSolutionCommandOptions options, ProjectStore projectStore)
        {
            _options = options;
            _projectStore = projectStore;
        }

        private static ICollection<FileInfo> LoadCurrentProjects(FileInfo filePath)
        {
            if (!filePath.Exists)
                return Array.Empty<FileInfo>();

            SolutionFile slnFile = SolutionFile.Parse(filePath.FullName);

            return slnFile.ProjectsInOrder
                .Where(s => s.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
                .Select(s => new FileInfo(s.AbsolutePath))
                .ToHashSet();
        }

        public override async Task<int> Execute()
        {
            // Identify all roots
            ICollection<FileInfo> projectFiles = ProjectUtility.GetAllMatchedProjects(_options.RootDir, _options.FileTypes, _options.Include, _options.Exclude).ToList();

            // Add all dependencies
            projectFiles = projectFiles
                .Concat(projectFiles.SelectMany(projectFile => ProjectUtility.GetProjectDependencies(_projectStore, projectFile)))
                .ToHashSet();

            FileInfo slnFilePath = new FileInfo(Path.GetFullPath(_options.SolutionFile, _options.RootDir.FullName));
            ICollection<FileInfo> currentProjects = LoadCurrentProjects(slnFilePath);

            List<FileInfo> toRemove = currentProjects.Except(projectFiles).ToList();
            List<FileInfo> toAdd = projectFiles.Except(currentProjects).ToList();

            foreach (FileInfo path in toRemove)
                Log.Debug("Will remove {Project}", Path.GetRelativePath(_options.RootDir.FullName, path.FullName));

            foreach (FileInfo path in toAdd)
                Log.Debug("Will add {Project}", Path.GetRelativePath(_options.RootDir.FullName, path.FullName));

            Log.Information("Will add {CountAdd:N0} and remove {CountRemove:N0} projects", toAdd.Count, toRemove.Count);

            // Build commandlines
            List<string[]> argumentSets = new List<string[]>();

            if (!slnFilePath.Exists)
            {
                // Create sln file
                argumentSets.Add(new[] { "new", "sln", "-o", slnFilePath.DirectoryName, "-n", Path.GetFileNameWithoutExtension(slnFilePath.Name) });
            }

            if (toRemove.Any())
                argumentSets.AddRange(ProcessUtility.CreateArguments(new[] { "sln", slnFilePath.Name, "remove" }, toRemove.Select(s => Path.GetRelativePath(slnFilePath.DirectoryName, s.FullName))));

            if (toAdd.Any())
                argumentSets.AddRange(ProcessUtility.CreateArguments(new[] { "sln", slnFilePath.Name, "add" }, toAdd.Select(s => Path.GetRelativePath(slnFilePath.DirectoryName, s.FullName))));

            // Execute
            Log.Debug("Executing {Count} commands to bring {SlnFile} up to date", argumentSets.Count, slnFilePath);

            foreach (string[] arguments in argumentSets)
            {
                int exitCode = await ProcessUtility.Execute("dotnet", slnFilePath.DirectoryName, arguments);

                if (exitCode != 0)
                {
                    Log.Error("Unable to modify sln file, exit code: {ExitCode}", exitCode);

                    return 10;
                }
            }

            return 0;
        }
    }
}