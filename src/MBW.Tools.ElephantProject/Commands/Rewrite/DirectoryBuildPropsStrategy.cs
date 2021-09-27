using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Serilog;

namespace MBW.Tools.ElephantProject.Commands.Rewrite
{
    class DirectoryBuildPropsStrategy
    {
        private const string GroupName = "Elephant Project";

        public void Perform(ReplacementTask info)
        {
            HashSet<string> packagesToReplace = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Determine all replacements
            foreach (FileInfo projectFile in info.TargetProjects)
            {
                Project project = info.ProjectStore.Load(projectFile);

                foreach (ProjectItem packageReference in project.GetItems("PackageReference"))
                {
                    string packageName = packageReference.EvaluatedInclude;

                    if (info.Replacement.ContainsKey(packageName))
                        packagesToReplace.Add(packageName);
                }
            }

            FileInfo buildPropsFile = new FileInfo(Path.Combine(info.RootDirectory.FullName, "Directory.Build.props"));

            {
                if (!buildPropsFile.Exists)
                {
                    using (FileStream fs = buildPropsFile.Create())
                    using (StreamWriter sw = new StreamWriter(fs, new UTF8Encoding(false)))
                    {
                        sw.WriteLine("<Project>");
                        sw.WriteLine("</Project>");
                    }

                    buildPropsFile.Refresh();
                }

                // Idea: https://github.com/dotnet/sdk/issues/1151#issuecomment-356136396
                // Item functions: https://docs.microsoft.com/en-us/visualstudio/msbuild/item-functions?view=vs-2019
                // 

                /* Example file:
                 *   <Target Name="ElephantProject" BeforeTargets="CollectPackageReferences">
                       <ItemGroup>
                         <!-- Map all projects to package names, when resolved, only those with corresponding PackageReference's are included -->
                         <ElephantProject Include="@(PackageReference->WithMetadataValue('Identity',    'MBW.Client.BlueRiiotAPI'))">
                           <ProjectFile>N:\Git\Personal\MBW.Client.BlueRiiotApi\src\MBW.Client.BlueRiiotApi   \MBW.Client.BlueRiiotApi.csproj</ProjectFile>
                         </ElephantProject>
                       
                         <!-- @(ElephantProject) now includes *all* packages that should be replaced -->
                         <PackageReference Remove="@(ElephantProject)"></PackageReference>
                         <ProjectReference Include="@(ElephantProject->Metadata('ProjectFile'))">
                           <Project></Project>
                         </ProjectReference>
                       </ItemGroup>
                       
                       <Message Importance="High" Text="ElephantProject: @(ElephantProject)" />
                       <Message Importance="High" Text="ElephantProject.ProjectFile: @(ElephantProject->Metadata('ProjectFile'))" />
                       <Message Importance="High" Text="ProjectReference: @(ProjectReference)" />
                       <Message Importance="High" Text="PackageReference: @(PackageReference)" />
                     </Target>
                 */

                Project project = info.ProjectStore.Load(buildPropsFile);

                foreach (ProjectTargetElement existingTarget in project.Xml.Targets.Where(s => s.Name == "ElephantProject").ToList())
                    project.Xml.RemoveChild(existingTarget);

                ProjectTargetElement targetItem = project.Xml.AddTarget("ElephantProject");
                targetItem.BeforeTargets = "CollectPackageReferences";

                ProjectItemGroupElement itemGroup = targetItem.AddItemGroup();
                itemGroup.Label = GroupName;

                foreach (string packageName in packagesToReplace)
                {
                    FileInfo projectFile = info.Replacement[packageName];

                    ProjectItemElement item = itemGroup.AddItem("ElephantProject", "@(PackageReference->WithMetadataValue('Identity', '" + packageName + "'))");
                    item.AddMetadata("ProjectFile", projectFile.FullName);
                }

                ProjectItemElement pacakgeReference = itemGroup.AddItem("PackageReference", "test");
                pacakgeReference.Include = null;
                pacakgeReference.Remove = "@(ElephantProject)";

                ProjectItemElement projectReference = itemGroup.AddItem("ProjectReference", "@(ElephantProject->Metadata('ProjectFile'))");
                projectReference.AddMetadata("Project", string.Empty);
                
                project.Save();
            }

            Log.Information("Altered {File}, replaced {Count:N0} packages with projects", buildPropsFile.Name, packagesToReplace.Count);
        }
    }
}