using System;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;

namespace MrLuje.LazyPackagesCleaner.Business.Commands
{
    public class OpenPackageFolderCommand : BaseCommand
    {
        private readonly DTE2 _dte;

        public OpenPackageFolderCommand(DTE2 dte)
            : base(showAnimations: false)
        {
            _dte = dte;
        }

        protected override void ExecuteCommand()
        {
            if (!_dte.Solution.IsOpen) return;

            var nugetConfigFile = _dte.Solution.FindProjectItem("nuget.config");
            var nugetConfigPath = nugetConfigFile.FileNames[1];

            var solutionFolder = Path.GetDirectoryName(_dte.Solution.FullName);
            var packageFolder = Utils.FindPackageFolder(nugetConfigPath, solutionFolder);

            if (!string.IsNullOrEmpty(packageFolder))
                System.Diagnostics.Process.Start(packageFolder);
        }
    }
}
