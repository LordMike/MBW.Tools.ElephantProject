using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MBW.Tools.ElephantProject.Helpers;
using Microsoft.Build.Evaluation;
using Serilog;

namespace MBW.Tools.ElephantProject.Commands.Rewrite
{
    class RewriteCsprojCommand : CommandBase
    {
        private readonly RewriteCsprojCommandOptions _options;
        private readonly ProjectStore _projectStore;

        public RewriteCsprojCommand(RewriteCsprojCommandOptions options, ProjectStore projectStore)
        {
            _options = options;
            _projectStore = projectStore;
        }

        private string GetAssemblyName(FileInfo projectFile)
        {
            Project project = _projectStore.Load(projectFile);

            string id = project.GetPropertyValue("PackageId");
            if (string.IsNullOrEmpty(id))
                id = project.GetPropertyValue("AssemblyName");

            if (string.IsNullOrEmpty(id))
                id = Path.GetFileNameWithoutExtension(projectFile.Name);

            return id;
        }

        private Dictionary<string, FileInfo> GetProjectsLookup(List<FileInfo> matchedProjects)
        {
            List<(FileInfo file, string assemblyName)> lookupProjects = matchedProjects
                .Concat(matchedProjects.SelectMany(projectFile => ProjectUtility.GetProjectDependencies(_projectStore, projectFile))).Distinct(FileInfoComparer.Instance)
                .Select(s => (file: s, assemblyName: GetAssemblyName(s)))
                .ToList();

            Dictionary<string, List<FileInfo>> duplicateAssemblyNames = new Dictionary<string, List<FileInfo>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, FileInfo> lookup = new Dictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);

            foreach ((FileInfo file, string assemblyName) in lookupProjects)
            {
                if (duplicateAssemblyNames.TryGetValue(assemblyName, out List<FileInfo> dupeList))
                {
                    dupeList.Add(file);
                    continue;
                }

                if (lookup.TryAdd(assemblyName, file))
                    continue;

                duplicateAssemblyNames.Add(assemblyName, new List<FileInfo> { lookup[assemblyName], file });
                lookup.Remove(assemblyName);
            }

            foreach ((string duplicateName, List<FileInfo> files) in duplicateAssemblyNames)
                Log.Warning("Identified {Count} duplicate assemblies for package {Name}: {Files}. Skipping", files.Count, duplicateName, files.Select(s => s.FullName));

            return lookup;
        }

        public override Task<int> Execute()
        {
            List<FileInfo> matchedProjects = ProjectUtility.GetAllMatchedProjects(_options.RootDir, new[] { ".csproj" }, _options.Include, _options.Exclude).ToList();

            Dictionary<string, FileInfo> lookup = GetProjectsLookup(matchedProjects);

            ReplacementTask replacementTask = new ReplacementTask
            {
                ProjectStore = _projectStore,
                TargetProjects = matchedProjects,
                Replacement = lookup,
                RootDirectory = _options.RootDir
            };

            switch (_options.Strategy)
            {
                case RewriteCsprojStrategy.ItemGroup:
                    {
                        RewriteItemGroupStrategy strategy = new RewriteItemGroupStrategy();
                        strategy.Perform(replacementTask);
                        break;
                    }
                case RewriteCsprojStrategy.BuildProps:
                    {
                        DirectoryBuildPropsStrategy strategy = new DirectoryBuildPropsStrategy();
                        strategy.Perform(replacementTask);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Task.FromResult(0);
        }
    }
}