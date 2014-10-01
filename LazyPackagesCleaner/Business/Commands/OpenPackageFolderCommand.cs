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

            var packageFolder = SolutionHelper.FindPackageFolder(_dte);

            if (Utils.CheckForRepositoryConfig(packageFolder))
                System.Diagnostics.Process.Start(packageFolder);
        }
    }
}
