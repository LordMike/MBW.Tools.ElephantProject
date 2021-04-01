using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MBW.Tools.ElephantProject.Helpers;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Serilog;

namespace MBW.Tools.ElephantProject.Commands.UndoRewrite
{
    class UndoRewiteCsprojCommand : CommandBase
    {
        private readonly UndoRewiteCsprojCommandOptions _options;
        private readonly ProjectStore _projectStore;

        public UndoRewiteCsprojCommand(UndoRewiteCsprojCommandOptions options, ProjectStore projectStore)
        {
            _options = options;
            _projectStore = projectStore;
        }

        public override async Task<int> Execute()
        {
            List<FileInfo> matchedProjects = ProjectUtility.GetAllMatchedProjects(_options.RootDir, new[] { ".csproj" }, _options.Include, _options.Exclude).ToList();

            foreach (FileInfo projectFile in matchedProjects)
            {
                Project project = _projectStore.Load(projectFile);

                List<ProjectItemGroupElement> groups = project.Xml.ItemGroups.Where(s => s.Label == "Elephant Project").ToList();

                if (!groups.Any())
                {
                    Log.Debug("Skipping {File}, nothing to do", projectFile.FullName);
                    continue;
                }

                // Remove groups
                foreach (ProjectItemGroupElement groupElement in groups)
                    project.Xml.RemoveChild(groupElement);

                Log.Information("Altered {File}, removed all project reference groups", Path.GetRelativePath(_options.RootDir.FullName, projectFile.FullName));
                project.Save();
            }

            return 0;
        }
    }
}