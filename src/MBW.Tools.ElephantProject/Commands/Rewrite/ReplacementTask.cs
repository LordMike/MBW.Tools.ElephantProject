using System.Collections.Generic;
using System.IO;
using MBW.Tools.ElephantProject.Helpers;

namespace MBW.Tools.ElephantProject.Commands.Rewrite
{
    class ReplacementTask
    {
        public ProjectStore ProjectStore { get; set; }

        public List<FileInfo> TargetProjects { get; set; }

        /// <summary>
        /// PackageName => Local project
        /// </summary>
        public Dictionary<string, FileInfo> Replacement { get; set; }

        public DirectoryInfo RootDirectory { get; set; }
    }
}