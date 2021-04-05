using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MBW.Tools.ElephantProject.Helpers;
using Microsoft.Build.Construction;
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
            HashSet<string> uniqueProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<(FileInfo file, string assemblyName)> lookupProjects = matchedProjects
                .Concat(matchedProjects.SelectMany(projectFile => ProjectUtility.GetProjectDependencies(_projectStore, projectFile)))
                .Where(s => uniqueProjects.Add(s.FullName))
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

                duplicateAssemblyNames.Add(assemblyName, new List<FileInfo> { file, lookup[assemblyName] });
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

            foreach (FileInfo projectFile in matchedProjects)
            {
                Project project = _projectStore.Load(projectFile);

                List<string> toReplace = project.GetItems("PackageReference")
                    .Select(s => s.EvaluatedInclude)
                    .Where(s => lookup.ContainsKey(s))
                    .ToList();

                if (!toReplace.Any())
                {
                    Log.Debug("Skipping {File}, nothing to do", projectFile.FullName);
                    continue;
                }

                ProjectItemGroupElement itemGroup = project.Xml.AddItemGroup();
                itemGroup.Label = "Elephant Project";

                foreach (string packageName in toReplace)
                {
                    ProjectItemElement removePackage = project.Xml.CreateItemElement("PackageReference");
                    removePackage.Remove = packageName;

                    itemGroup.AppendChild(removePackage);

                    ProjectItemElement includeReference = project.Xml.CreateItemElement("ProjectReference");
                    includeReference.Include = lookup[packageName].FullName;

                    itemGroup.AppendChild(includeReference);
                }

                Log.Information("Altered {File}, replaced {Count:N0} packages with projects", Path.GetRelativePath(_options.RootDir.FullName, projectFile.FullName), toReplace.Count);
                project.Save();
            }

            return Task.FromResult(0);
        }
    }
}