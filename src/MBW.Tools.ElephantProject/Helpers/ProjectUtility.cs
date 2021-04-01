using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNet.Globbing;
using Microsoft.Build.Evaluation;
using Serilog;

namespace MBW.Tools.ElephantProject.Helpers
{
    static class ProjectUtility
    {
        public static IEnumerable<FileInfo> GetProjectDependencies(ProjectStore projectStore, FileInfo projectFile)
        {
            Project project = projectStore.Load(projectFile);

            Log.Debug("Loaded {Project}, {Count:N0} items", projectFile, project.Items.Count);

            string fileDir = projectFile.DirectoryName;
            foreach (ProjectItem projectItem in project.GetItems("ProjectReference"))
            {
                string fullPath = Path.GetFullPath(projectItem.EvaluatedInclude, fileDir);
                Log.Debug("Project {Project} references {Target}, full path: {TargetFull}", projectFile, projectItem.EvaluatedInclude, fullPath);

                yield return new FileInfo(fullPath);
            }
        }

        public static IEnumerable<FileInfo> GetAllMatchedProjects(DirectoryInfo rootDirectory, IEnumerable<string> extensions, IEnumerable<string> includes, IEnumerable<string> excludes)
        {
            HashSet<string> fileTypes = extensions.ToHashSet(StringComparer.OrdinalIgnoreCase);

            GlobOptions globOptions = new GlobOptions
            {
                Evaluation = new EvaluationOptions
                {
                    CaseInsensitive = true
                }
            };

            List<Glob> includeGlobs = includes.Select(s => Glob.Parse("**/" + s, globOptions)).ToList();
            List<Glob> excludeGlobs = excludes.Select(s => Glob.Parse("**/" + s, globOptions)).ToList();

            IEnumerable<FileInfo> includeFiles = rootDirectory.EnumerateFiles("*", new EnumerationOptions
            {
                RecurseSubdirectories = true
            });

            if (excludeGlobs.Any())
                includeFiles = includeFiles.Where(s => !excludeGlobs.Any(x => x.IsMatch(s.FullName)));

            includeFiles = includeFiles
                .Where(s => fileTypes.Contains(s.Extension))
                .Where(s => includeGlobs.Any(x => x.IsMatch(s.FullName)));

            foreach (FileInfo file in includeFiles)
                yield return file;
        }
    }
}