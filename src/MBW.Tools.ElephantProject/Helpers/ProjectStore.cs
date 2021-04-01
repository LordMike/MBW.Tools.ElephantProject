using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;

namespace MBW.Tools.ElephantProject.Helpers
{
    class ProjectStore
    {
        private readonly ProjectCollection _projectCollection;

        public ProjectStore()
        {
            _projectCollection = new ProjectCollection();
        }

        public Project Load(FileInfo file)
        {
            List<Project> loaded = _projectCollection.GetLoadedProjects(file.FullName).ToList();
            if (loaded.Any())
                return loaded.First();

            Project project = Project.FromFile(file.FullName, new ProjectOptions
            {
                LoadSettings = ProjectLoadSettings.IgnoreMissingImports,
                ProjectCollection = _projectCollection
            });

            return project;
        }
    }
}