using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.TeamFoundation.VersionControl.Client;
using MrLuje.LazyPackagesCleaner.Properties;

namespace MrLuje.LazyPackagesCleaner.Business.Commands
{
    public class FixProjectReferenceCommand : BaseCommand
    {
        private readonly DTE2 _dte;
        private Workspace _workspace;

        private readonly Regex regexVersionnedReferences =
            new Regex(Settings.Default.RegexProjectReferencePattern, RegexOptions.Compiled & RegexOptions.IgnoreCase);

        public FixProjectReferenceCommand(DTE2 dte)
            : base(showAnimations: true)
        {
            _dte = dte;

            AnimationStartText = Resources.FixingReferencesStart;
            AnimationEndText= Resources.FixingReferencesEnd;
        }

        protected override void ExecuteCommand()
        {
            if (!_dte.Solution.IsOpen) return;

            var projectsFixed = new List<String>();
            var projectsFailed = new List<String>();

            SetStatusBarText(Resources.ListingSolutionProjects);

            var solutionProjects = SolutionHelper.GetSolutionProjects(_dte);
            var projectCount = solutionProjects.Count();
            var projectIndex = 1;

            foreach (var proj in solutionProjects)
            {
                var fullpath = proj.FullName;

                var projectContent = File.ReadAllText(fullpath);
                if (regexVersionnedReferences.IsMatch(projectContent))
                {
                    // Checkout file if currently mapped to a workspace
                    _workspace = _workspace ?? (_workspace = TfsHelper.GetWorkspace(fullpath));
                    TfsHelper.CheckoutFile(fullpath, _workspace);

                    var cleanContent = regexVersionnedReferences.Replace(projectContent, Settings.Default.RegexProjectReferenceReplace);
                    try
                    {
                        File.WriteAllText(fullpath, cleanContent);
                        projectsFixed.Add(Path.GetFileNameWithoutExtension(fullpath));
                    }
                    catch (Exception ex)
                    {
                        projectsFailed.Add(Path.GetFileNameWithoutExtension(fullpath));
                    }
                }

                SetStatusBarText(String.Format(Resources.FixingReferenceProgress, projectIndex++, projectCount));
            }

            if (projectsFixed.Any() || projectsFailed.Any())
            {
                MessageBox.Show(
                    projectsFixed.Aggregate("", (all, value) => all + String.Format("{0} fixed\n", value)) +
                    projectsFailed.Aggregate("", (all, value) => all + String.Format("{0} failed\n", value))
                );
            }
            else
            {
                MessageBox.Show("Nothing to fix", "Yeah !");
            }

            _workspace = null;
        }
    }
}
