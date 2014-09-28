using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using MrLuje.LazyPackagesCleaner.Properties;

namespace MrLuje.LazyPackagesCleaner.Business.Commands
{
    public class DeleteAllPackageCommand : BaseCommand
    {
        private readonly DTE2 _dte;

        public DeleteAllPackageCommand(DTE2 dte)
            : base(showAnimations: true)
        {
            _dte = dte;

            AnimationStartText = Resources.DeletionAllFoldersStart;
            AnimationEndText = Resources.DeletionAllFoldersEnd;
            Icon = (short) Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Save;
        }

        protected override void ExecuteCommand()
        {
            if (!_dte.Solution.IsOpen) return;

            var nugetConfigFile = _dte.Solution.FindProjectItem("nuget.config");
            var nugetConfigPath = nugetConfigFile.FileNames[1];

            var solutionFolder = Path.GetDirectoryName(_dte.Solution.FullName);
            var packageFolder = Utils.FindPackageFolder(nugetConfigPath, solutionFolder);

            if (!Utils.CheckForRepositoryConfig(packageFolder)) return;

            var folders = from dir in Directory.EnumerateDirectories(packageFolder)
                          select dir;

            var folderDeletor = new FolderDeletor();
            folderDeletor.DeletionProgress += base.AnimationProgress;
            folderDeletor.DeleteFolders(folders);
        }
    }
}
