using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Serilog;

namespace MBW.Tools.ElephantProject.Commands.Rewrite
{
    class RewriteItemGroupStrategy
    {
        private const string GroupName = "Elephant Project";

        public void Perform(ReplacementTask info)
        {
            foreach (FileInfo projectFile in info.TargetProjects)
            {
                Project project = info.ProjectStore.Load(projectFile);

                List<string> toReplace = project.GetItems("PackageReference")
                    .Select(s => s.EvaluatedInclude)
                    .Where(s => info.Replacement.ContainsKey(s))
                    .ToList();

                if (!toReplace.Any())
                {
                    Log.Debug("Skipping {File}, nothing to do", projectFile.FullName);
                    continue;
                }

                ProjectItemGroupElement itemGroup = project.Xml.AddItemGroup();
                itemGroup.Label = GroupName;

                foreach (string packageName in toReplace)
                {
                    ProjectItemElement removePackage = project.Xml.CreateItemElement("PackageReference");
                    removePackage.Remove = packageName;

                    ProjectItemElement includeReference = project.Xml.CreateItemElement("ProjectReference");
                    includeReference.Include = info.Replacement[packageName].FullName;

                    itemGroup.AppendChild(removePackage);
                    itemGroup.AppendChild(includeReference);
                }

                project.Save();

                Log.Information("Altered {File}, replaced {Count:N0} packages with projects", Path.GetRelativePath(info.RootDirectory.FullName, projectFile.FullName), toReplace.Count);
            }
        }
    }
}