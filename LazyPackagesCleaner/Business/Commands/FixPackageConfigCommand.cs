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
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using MrLuje.LazyPackagesCleaner.Model;
using MrLuje.LazyPackagesCleaner.Properties;

namespace MrLuje.LazyPackagesCleaner.Business.Commands
{
    public class FixPackageConfigCommand : BaseCommand
    {
        private readonly DTE2 _dte;
        private Workspace _workspace;

        private readonly Regex regexPackageConfigContent =
            new Regex(Settings.Default.RegexPackageConfigPackagePattern, RegexOptions.Compiled & RegexOptions.IgnoreCase);

        public FixPackageConfigCommand(DTE2 dte)
            : base(showAnimations: false)
        {
            _dte = dte;
        }

        protected override void ExecuteCommand()
        {
            if (!_dte.Solution.IsOpen) return;

            var packageConfigs = SolutionHelper.GetSolutionPackagesConfig(_dte);
            var referencedVersions = new NugetPackages();

            foreach (var packageConfig in packageConfigs)
            {
                var content = File.ReadAllText(packageConfig);
                foreach (Match match in regexPackageConfigContent.Matches(content))
                {
                    var name = match.Groups["name"].Value;
                    var version = match.Groups["version"].Value;
                    referencedVersions.AddVersion(name, version);
                }
            }

            if (!referencedVersions.HasConflictingVersions())
            {
                MessageBox.Show("Nothing to fix", "Yeah !");
                return;
            }

            var form = new ReferenceConflicts();
            form.Show();
            form.Confirmed += (selectedVersions) =>
            {
                var fixCount = 0;
                var targetCount = 0;
                foreach (var packageConfig in packageConfigs)
                {
                    var content = File.ReadAllText(packageConfig);
                    var orig = content;
                    foreach (var selectedVersion in selectedVersions)
                    {
                        var wrongValues = referencedVersions.GetVersions(selectedVersion.PackageName)
                                                            .Where(val => val != selectedVersion.PackageVersion);

                        foreach (var wrongValue in wrongValues)
                        {
                            content = content.Replace(String.Format("id=\"{0}\" version=\"{1}\"", selectedVersion.PackageName, wrongValue),
                                                      String.Format("id=\"{0}\" version=\"{1}\"", selectedVersion.PackageName, selectedVersion.PackageVersion));
                        }
                    }

                    if (orig != content)
                    {
                        targetCount++;

                        // Checkout file if currently mapped to a workspace
                        _workspace = _workspace ?? (_workspace = TfsHelper.GetWorkspace(packageConfig));
                        TfsHelper.CheckoutFile(packageConfig, _workspace);

                        try
                        {
                            File.WriteAllText(packageConfig, content);
                            fixCount++;
                        }
                        catch (Exception ex) { }
                    }
                }

                if (fixCount > 0)
                    MessageBox.Show(String.Format("{0}/{1} config fixed !", fixCount, targetCount));
            };
            form.SetValue(referencedVersions.GetConflictingVersions());
        }
    }
}
