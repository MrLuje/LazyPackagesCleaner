using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;

namespace MrLuje.LazyPackagesCleaner.Business
{
    public static class SolutionHelper
    {
        static List<Project> FindProjectRecurse(Project proj)
        {
            var projects = new List<Project>();
            IEnumerable<ProjectItem> projectItems = proj.ProjectItems
                .OfType<ProjectItem>()
                .Where(p => p.Kind == EnvDTE.Constants.vsProjectItemKindSolutionItems ||
                            p.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                .ToList();

            if (proj.UniqueName.EndsWith("proj"))
                projects.Add(proj);

            foreach (var item in projectItems)
            {
                if (item.SubProject != null && item.SubProject.UniqueName.EndsWith("proj"))
                {
                    projects.Add(item.SubProject);
                }
                else if (item.SubProject != null)
                {
                    projects.AddRange(FindProjectRecurse(item.SubProject));
                }
            }

            return projects;
        }

        public static IEnumerable<Project> GetSolutionProjects(DTE2 dte)
        {
            var projectFiles = dte.Solution.Projects.OfType<Project>();
            return projectFiles.SelectMany(SolutionHelper.FindProjectRecurse);
        }

        public static IEnumerable<String> GetSolutionPackagesConfig(DTE2 dte)
        {
            foreach (Project proj in SolutionHelper.GetSolutionProjects(dte))
            {
                foreach (ProjectItem item in proj.ProjectItems.OfType<ProjectItem>())
                {
                    if (item.Name.ToLower() == "packages.config")
                        yield return Path.Combine(Path.GetDirectoryName(proj.FullName), item.Name);
                }
            }
        }

        public static string FindPackageFolder(DTE2 dte)
        {
            var nugetConfigFile = dte.Solution.FindProjectItem("nuget.config");
            var nugetConfigPath = nugetConfigFile != null ? nugetConfigFile.FileNames[1] : string.Empty;

            var solutionFolder = Path.GetDirectoryName(dte.Solution.FullName);
            return Utils.FindPackageFolder(nugetConfigPath, solutionFolder);
        }
    }
}
