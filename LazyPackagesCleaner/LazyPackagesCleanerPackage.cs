using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.TeamFoundation.Client;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using MrLuje.LazyPackagesCleaner.Business;
using MrLuje.LazyPackagesCleaner.Business.Commands;
using MrLuje.LazyPackagesCleaner.GUI;
using MrLuje.LazyPackagesCleaner.Model;
using MrLuje.LazyPackagesCleaner.Properties;

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
    [ProvideOptionPage(typeof(SettingsTool), "Lazy Packages Cleaner", "General", 101, 106, true)]
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

        private SolutionEventDetector solutionEventDetector;

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                var menuCommandDeleteVersionned = new CommandID(GuidList.guidLazyPackagesCleanerCmdSet, (int)PkgCmdIDList.cmdDeleteNonVersionned);
                _menuCommandDeleteVersionned = new MenuCommand(MenuItemCallback_DeleteVersionned, menuCommandDeleteVersionned);
                mcs.AddCommand(_menuCommandDeleteVersionned);

                var menuCommandDeleteAll = new CommandID(GuidList.guidLazyPackagesCleanerCmdSet, (int)PkgCmdIDList.cmdDeleteAll);
                _menuCommandDeleteAll = new MenuCommand(MenuItemCallback_DeleteAll, menuCommandDeleteAll);
                mcs.AddCommand(_menuCommandDeleteAll);

                var menuCommandOpenPackages = new CommandID(GuidList.guidLazyPackagesCleanerCmdSet, (int)PkgCmdIDList.cmdOpenPackages);
                _menuCommandOpenPackages = new MenuCommand(MenuItemCallback_OpenPackages, menuCommandOpenPackages);
                mcs.AddCommand(_menuCommandOpenPackages);

                var menuCommandFixReferences = new CommandID(GuidList.guidLazyPackagesCleanerCmdSet, (int)PkgCmdIDList.cmdFixVersionnedReferences);
                _menuCommandFixReferences = new MenuCommand(MenuItemCallback_FixReferences, menuCommandFixReferences);
                mcs.AddCommand(_menuCommandFixReferences);

                var menuCommandFixPackagesConfig = new CommandID(GuidList.guidLazyPackagesCleanerCmdSet, (int)PkgCmdIDList.cmdFixPackagesConfig);
                _menuCommandFixPackagesConfig = new MenuCommand(MenuItemCallback_FixPackagesConfig, menuCommandFixPackagesConfig);
                mcs.AddCommand(_menuCommandFixPackagesConfig);

                _menuCommandDeleteAll.Enabled = false;
                _menuCommandDeleteVersionned.Enabled = false;
                _menuCommandFixPackagesConfig.Enabled = false;
                _menuCommandFixReferences.Enabled = false;
                _menuCommandOpenPackages.Enabled = false;
            }

            var dte = GetService(typeof(SDTE)) as DTE2;
            solutionEventDetector = new SolutionEventDetector(dte);
            solutionEventDetector.FirstBuild += SolutionEvents_FirstBuild;
            solutionEventDetector.OnSolutionOpened += SolutionEvents_Opened;
            solutionEventDetector.OnBuildBegin += SolutionEvents_BuildBegin;
            solutionEventDetector.OnBeforeClosing += SolutionEvents_BeforeClosing;
        }

        void SolutionEvents_Opened()
        {
            if (_menuCommandFixPackagesConfig != null) _menuCommandFixPackagesConfig.Enabled = true;
            if (_menuCommandFixReferences != null) _menuCommandFixReferences.Enabled = true;
            if (_menuCommandDeleteAll != null) _menuCommandDeleteAll.Enabled = true;
            if (_menuCommandDeleteVersionned != null) _menuCommandDeleteVersionned.Enabled = true;
            if (_menuCommandOpenPackages != null) _menuCommandOpenPackages.Enabled = true;
        }

        void SolutionEvents_BeforeClosing()
        {
            if (_menuCommandFixPackagesConfig != null) _menuCommandFixPackagesConfig.Enabled = false;
            if (_menuCommandFixReferences != null) _menuCommandFixReferences.Enabled = false;
            if (_menuCommandDeleteAll != null) _menuCommandDeleteAll.Enabled = false;
            if (_menuCommandDeleteVersionned != null) _menuCommandDeleteVersionned.Enabled = false;
            if (_menuCommandOpenPackages != null) _menuCommandOpenPackages.Enabled = false;
        }

        void SolutionEvents_BuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            var dte = GetService(typeof(SDTE)) as DTE2;

            if (Action == vsBuildAction.vsBuildActionBuild ||
                Action == vsBuildAction.vsBuildActionRebuildAll)
            {
                if (Settings.Default.DebugMode)
                    MessageBox.Show("Building: " + dte.Solution.FullName);

                solutionEventDetector.SolutionBuild(dte.Solution.FullName);
            }
            else if (Action == vsBuildAction.vsBuildActionClean && Settings.Default.EnableDeleteOnClean)
            {
                var deleteCommand = new DeleteVersionnedPackageCommand(dte);
                deleteCommand.Execute();
            }
        }

        void SolutionEvents_FirstBuild()
        {
            if (Settings.Default.DebugMode)
                MessageBox.Show("First build !");

            if (!Settings.Default.EnableDeleteOnFirstBuild) return;

            var dte = GetService(typeof(SDTE)) as DTE2;
            var deleteCommand = new DeleteVersionnedPackageCommand(dte);
            deleteCommand.Execute();
        }

        #endregion

        #region Menu item callbacks

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback_DeleteVersionned(object sender, EventArgs e)
        {
            var dte = GetService(typeof(SDTE)) as DTE2;
            var deleteCommand = new DeleteVersionnedPackageCommand(dte);
            deleteCommand.Execute();
        }

        private void MenuItemCallback_DeleteAll(object sender, EventArgs e)
        {
            var dte = GetService(typeof(SDTE)) as DTE2;
            var deleteCommand = new DeleteAllPackageCommand(dte);
            deleteCommand.Execute();
        }

        private void MenuItemCallback_OpenPackages(object sender, EventArgs e)
        {
            var dte = GetService(typeof(SDTE)) as DTE2;
            var openCommand = new OpenPackageFolderCommand(dte);
            openCommand.Execute();
        }

        private void MenuItemCallback_FixPackagesConfig(object sender, EventArgs e)
        {
            var dte = GetService(typeof(SDTE)) as DTE2;
            var fixPackageCommand = new FixPackageConfigCommand(dte);
            fixPackageCommand.Execute();

            if(Settings.Default.EnableFullPackageCleanOnFixConfig)
                new DeleteAllPackageCommand(dte).Execute();
        }

        private void MenuItemCallback_FixReferences(object sender, EventArgs e)
        {
            var dte = GetService(typeof(SDTE)) as DTE2;
            var fixReferenceCommand = new FixProjectReferenceCommand(dte);
            fixReferenceCommand.Execute();
        }

        #endregion

        #region Visual stuffs

        #endregion

        private MenuCommand _menuCommandDeleteVersionned;
        private MenuCommand _menuCommandDeleteAll;
        private MenuCommand _menuCommandOpenPackages;
        private MenuCommand _menuCommandFixReferences;
        private MenuCommand _menuCommandFixPackagesConfig;
    }
}
