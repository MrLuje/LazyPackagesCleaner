﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;

namespace MrLuje.LazyPackagesCleaner
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidLazyPackagesCleanerPkgString)]
    // Only load the package if there is a solution loaded
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    public sealed class LazyPackagesCleanerPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public LazyPackagesCleanerPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }



        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the menu item.
                var menuCommandDeleteVersionned = new CommandID(GuidList.guidLazyPackagesCleanerCmdSet, (int)PkgCmdIDList.cmdDeleteNonVersionned);
                var menuItem = new MenuCommand(MenuItemCallback_DeleteVersionned, menuCommandDeleteVersionned);
                mcs.AddCommand(menuItem);

                var menuCommandDeleteAll = new CommandID(GuidList.guidLazyPackagesCleanerCmdSet, (int)PkgCmdIDList.cmdDeleteAll);
                mcs.AddCommand(new MenuCommand(MenuItemCallback_DeleteAll, menuCommandDeleteAll));

                var menuCommandOpenPackages = new CommandID(GuidList.guidLazyPackagesCleanerCmdSet, (int)PkgCmdIDList.cmdOpenPackages);
                mcs.AddCommand(new MenuCommand(MenuItemCallback_OpenPackages, menuCommandOpenPackages));
            }
        }
        #endregion


        private IVsStatusbar bar;
        private IVsStatusbar StatusBar
        {
            get
            {
                return bar ?? (bar = GetService(typeof(SVsStatusbar)) as IVsStatusbar);
            }
        }

        #region Menu item callbacks

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback_DeleteVersionned(object sender, EventArgs e)
        {
            var dte = GetService(typeof(SDTE)) as DTE;

            var nugetConfigPath = String.Empty;
            var nugetProj = dte.Solution.Projects.OfType<Project>().FirstOrDefault(p => p.Name.Contains(".nuget"));
            ProjectItem nugetConfigFile = null;
            if (nugetProj != null)
                nugetConfigFile = nugetProj.ProjectItems.OfType<ProjectItem>().FirstOrDefault(pi => pi.Name.Contains("conf"));

            //nugetConfigPath = nugetConfigFile.Properties.Item("FullPath").Value.ToString();
            var solutionFolder = Path.GetDirectoryName(dte.Solution.FullName);

            var packageFolder = Utils.FindPackageFolder(nugetConfigPath, solutionFolder);

            DeleteNonVersionnedFolders(packageFolder);
            
        }

        private void MenuItemCallback_DeleteAll(object sender, EventArgs e)
        {
            var dte = GetService(typeof(SDTE)) as DTE;

            var nugetConfigPath = String.Empty;
            var nugetProj = dte.Solution.Projects.OfType<Project>().FirstOrDefault(p => p.Name.Contains(".nuget"));
            ProjectItem nugetConfigFile = null;
            if (nugetProj != null)
                nugetConfigFile = nugetProj.ProjectItems.OfType<ProjectItem>().FirstOrDefault(pi => pi.Name.Contains("conf"));

            //nugetConfigPath = nugetConfigFile.Properties.Item("FullPath").Value.ToString();
            var solutionFolder = Path.GetDirectoryName(dte.Solution.FullName);

            var packageFolder = Utils.FindPackageFolder(nugetConfigPath, solutionFolder);

            DeleteAllFolder(packageFolder);
        }

        private void MenuItemCallback_OpenPackages(object sender, EventArgs e)
        {
            var dte = GetService(typeof(SDTE)) as DTE;

            var nugetConfigPath = String.Empty;
            var nugetProj = dte.Solution.Projects.OfType<Project>().FirstOrDefault(p => p.Name.Contains(".nuget"));

            ProjectItem nugetConfigFile = null;
            if (nugetProj != null)
                nugetConfigFile = nugetProj.ProjectItems.OfType<ProjectItem>().FirstOrDefault(pi => pi.Name.Contains("conf"));

            //nugetConfigPath = nugetConfigFile.Properties.Item("FullPath").Value.ToString();
            var solutionFolder = Path.GetDirectoryName(dte.Solution.FullName);

            var packageFolder = Utils.FindPackageFolder(nugetConfigPath, solutionFolder);

            if (!string.IsNullOrEmpty(packageFolder))
                System.Diagnostics.Process.Start(packageFolder);
        }

        #endregion

        private void DeleteNonVersionnedFolders(string packageFolder)
        {
            if (!Utils.CheckForRepositoryConfig(packageFolder)) return;

            var regex = new Regex(@"\d+", RegexOptions.IgnoreCase);
            var folders = from dir in Directory.EnumerateDirectories(packageFolder)
                          where !regex.IsMatch(dir)
                          select dir;

            int frozenState;
            uint prog = 0;
            StatusBar.IsFrozen(out frozenState);
            int foldersCount = folders.Count();
            if (frozenState == 0)
            {
                StatusBar.SetText("Deleting non-versionned folders...");
                StatusBar.Progress(ref prog, 0, "", 0, Convert.ToUInt32(foldersCount));
            }

            object icon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Save;
            StatusBar.Animation(1, ref icon);

            uint cookie = 1;
            uint progress = 1;
            foreach (var folder in folders)
            {
                try
                {
                    if (Directory.Exists(folder))
                    {
                        Utils.RemoveReadOnly(folder);
                        Directory.Delete(folder, recursive: true);
                    }
                }
                catch (DirectoryNotFoundException) { }

                if (frozenState == 0)
                    StatusBar.Progress(ref cookie, 1, "", progress++, Convert.ToUInt32(foldersCount));
            }

            // Clear the progress bar.
            StatusBar.Progress(ref cookie, 0, "", 0, 0);
            StatusBar.Animation(0, ref icon);
            StatusBar.FreezeOutput(0);
        }

        private void DeleteAllFolder(string packageFolder)
        {
            if (!Utils.CheckForRepositoryConfig(packageFolder)) return;

            var regex = new Regex(@"\d+", RegexOptions.IgnoreCase);
            var folders = from dir in Directory.EnumerateDirectories(packageFolder)
                          select dir;

            int frozenState;
            uint prog = 0;
            StatusBar.IsFrozen(out frozenState);
            int foldersCount = folders.Count();
            if (frozenState == 0)
            {
                StatusBar.SetText("Deleting non-versionned folders...");
                StatusBar.Progress(ref prog, 0, "", 0, Convert.ToUInt32(foldersCount));
            }

            object icon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Save;
            StatusBar.Animation(1, ref icon);

            uint cookie = 1;
            uint progress = 1;
            foreach (var folder in folders)
            {
                try
                {
                    if (Directory.Exists(folder))
                    {
                        Utils.RemoveReadOnly(folder);
                        Directory.Delete(folder, recursive: true);
                    }
                }
                catch (DirectoryNotFoundException) { }

                if (frozenState == 0)
                    StatusBar.Progress(ref cookie, 1, "", progress++, Convert.ToUInt32(foldersCount));
            }

            // Clear the progress bar.
            StatusBar.Progress(ref cookie, 0, "", 0, 0);
            StatusBar.Animation(0, ref icon);
            StatusBar.FreezeOutput(0);
        }
    }
}