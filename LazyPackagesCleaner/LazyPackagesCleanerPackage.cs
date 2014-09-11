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
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
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

        private SolutionBuildDetector solutionBuildDetector;

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

                _menuCommandFixPackagesConfig.Enabled = false;
                _menuCommandFixReferences.Enabled = false;
            }

            solutionBuildDetector = new SolutionBuildDetector();
            solutionBuildDetector.FirstBuild += solutionBuildDetector_FirstBuild;
            var dte = GetService(typeof(SDTE)) as DTE;
            dte.Events.BuildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
            dte.Events.SolutionEvents.BeforeClosing += SolutionEvents_BeforeClosing;
            dte.Events.SolutionEvents.Opened += SolutionEvents_Opened;
        }

        void SolutionEvents_Opened()
        {
            var dte = GetService(typeof(SDTE)) as DTE;
            if (Settings.Default.DebugMode)
                MessageBox.Show("Opened: " + dte.Solution.FullName);
            solutionBuildDetector.SolutionLoad(dte.Solution.FullName);

            if (_menuCommandFixPackagesConfig != null) _menuCommandFixPackagesConfig.Enabled = true;
            if (_menuCommandFixReferences != null) _menuCommandFixReferences.Enabled = true;
        }

        void SolutionEvents_BeforeClosing()
        {
            var dte = GetService(typeof(SDTE)) as DTE;
            solutionBuildDetector.SolutionClose(dte.Solution.FullName);

            if (_menuCommandFixPackagesConfig != null) _menuCommandFixPackagesConfig.Enabled = false;
            if (_menuCommandFixReferences != null) _menuCommandFixReferences.Enabled = false;
        }

        void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            var dte = GetService(typeof(SDTE)) as DTE;

            if (Action == vsBuildAction.vsBuildActionBuild ||
                Action == vsBuildAction.vsBuildActionRebuildAll)
            {
                if (Settings.Default.DebugMode)
                    MessageBox.Show("Building: " + dte.Solution.FullName);

                solutionBuildDetector.SolutionBuild(dte.Solution.FullName);
            }
            else if (Action == vsBuildAction.vsBuildActionClean && Settings.Default.EnableDeleteOnClean)
            {
                var solutionFolder = Path.GetDirectoryName(dte.Solution.FullName);
                var packageFolder = Utils.FindPackageFolder(String.Empty, solutionFolder);

                DeleteNonVersionnedFolders(packageFolder);
            }
        }

        void solutionBuildDetector_FirstBuild()
        {
            if (Settings.Default.DebugMode)
                MessageBox.Show("First build !");

            if (!Settings.Default.EnableDeleteOnFirstBuild) return;

            var dte = GetService(typeof(SDTE)) as DTE;
            var solutionFolder = Path.GetDirectoryName(dte.Solution.FullName);
            var packageFolder = Utils.FindPackageFolder(String.Empty, solutionFolder);

            DeleteNonVersionnedFolders(packageFolder);
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
            var dte = GetService(typeof(SDTE)) as DTE;
            if (!dte.Solution.IsOpen) return;

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
            if (!dte.Solution.IsOpen) return;

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
            if (!dte.Solution.IsOpen) return;

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

        private void MenuItemCallback_FixPackagesConfig(object sender, EventArgs e)
        {
            var dte = GetService(typeof(SDTE)) as DTE2;
            if (!dte.Solution.IsOpen) return;

            var packageConfigs = GetSolutionPackagesConfig(dte);
            var referencedVersions = new PackageVersions();

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
            form.Confirmed += (result) =>
            {
                var fixCount = 0;
                var targetCount = 0;
                foreach (var packageConfig in packageConfigs)
                {
                    var content = File.ReadAllText(packageConfig);
                    var orig = content;
                    foreach (var tuple in result)
                    {
                        var wrongValues = referencedVersions.GetVersions(tuple.Item1)
                                                            .Where(val => val != tuple.Item2);
                        foreach (var wrongValue in wrongValues)
                        {
                            content = content.Replace(String.Format("id=\"{0}\" version=\"{1}\"", tuple.Item1, wrongValue),
                                                      String.Format("id=\"{0}\" version=\"{1}\"", tuple.Item1, tuple.Item2));
                        }
                    }

                    if (orig != content)
                    {
                        targetCount++;

                        // Checkout file if currently mapped to a workspace
                        var workspace = GetWorkspace(packageConfig);
                        if (workspace != null) workspace.PendEdit(packageConfig);

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

        private void MenuItemCallback_FixReferences(object sender, EventArgs e)
        {
            var dte = GetService(typeof(SDTE)) as DTE2;
            if (!dte.Solution.IsOpen) return;

            var projectsFixed = new List<String>();
            var projectsFailed = new List<String>();

            foreach (var proj in GetSolutionProjects(dte))
            {
                var fullpath = proj.FullName;

                var projectContent = File.ReadAllText(fullpath);
                if (regexVersionnedReferences.IsMatch(projectContent))
                {
                    // Checkout file if currently mapped to a workspace
                    var workspace = GetWorkspace(fullpath);
                    if (workspace != null) workspace.PendEdit(fullpath);

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

        #endregion

        #region Visual stuffs

        void InitVisualDeletion(string startText)
        {
            StatusBar.IsFrozen(out frozenState);

            if (frozenState == 0)
            {
                StatusBar.SetText(startText);

                StatusBar.Animation(1, ref icon);
            }
        }

        void ProgressVisualDeletion(uint current, uint total)
        {
            if (frozenState == 0)
                StatusBar.Progress(ref cookie, 1, "", current, total);
        }

        void EndVisualDeletion(string endText)
        {
            // Clear the progress bar.
            StatusBar.Progress(ref cookie, 0, "", 0, 0);
            StatusBar.Animation(0, ref icon);
            StatusBar.SetText(endText);
            StatusBar.FreezeOutput(0);
        }

        private IVsStatusbar bar;
        private IVsStatusbar StatusBar
        {
            get
            {
                return bar ?? (bar = GetService(typeof(SVsStatusbar)) as IVsStatusbar);
            }
        }

        object icon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Save;
        int frozenState;
        uint cookie = 1;

        #endregion

        private readonly Regex regexVersionnedReferences =
            new Regex(Settings.Default.RegexProjectReferencePattern,
                RegexOptions.Compiled & RegexOptions.IgnoreCase);
        private readonly Regex regexPackageConfigContent =
            new Regex(Settings.Default.RegexPackageConfigPackagePattern,
                RegexOptions.Compiled & RegexOptions.IgnoreCase);

        private Workspace _workspace;
        private MenuCommand _menuCommandDeleteVersionned;
        private MenuCommand _menuCommandDeleteAll;
        private MenuCommand _menuCommandOpenPackages;
        private MenuCommand _menuCommandFixReferences;
        private MenuCommand _menuCommandFixPackagesConfig;

        Workspace GetWorkspace(string file)
        {
            if (_workspace != null) return _workspace;

            var workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(file);
            if (workspaceInfo == null) return null;
            var server = new TfsTeamProjectCollection(workspaceInfo.ServerUri);
            return workspaceInfo.GetWorkspace(server);
        }

        List<Project> FindProjectRecurse(Project proj)
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

        private IEnumerable<String> GetSolutionPackagesConfig(DTE2 dte)
        {
            foreach (var mainProj in dte.Solution.Projects.OfType<Project>())
            {
                foreach (var proj in FindProjectRecurse(mainProj))
                {
                    foreach (var item in proj.ProjectItems.OfType<ProjectItem>())
                    {
                        if (item.Name.ToLower() == "packages.config")
                        {
                            yield return Path.Combine(Path.GetDirectoryName(proj.FullName), item.Name);
                        }
                    }
                }
            }
        }

        private IEnumerable<Project> GetSolutionProjects(DTE2 dte)
        {
            var projectFiles = dte.Solution.Projects.OfType<Project>();

            foreach (var projectFile in projectFiles)
            {
                foreach (var proj in FindProjectRecurse(projectFile))
                {
                    yield return proj;
                }
            }
        }

        #region Delete methods

        private void DeleteNonVersionnedFolders(string packageFolder)
        {
            if (!Utils.CheckForRepositoryConfig(packageFolder)) return;

            var regex = new Regex(@"\d+", RegexOptions.IgnoreCase);
            var folders = from dir in Directory.EnumerateDirectories(packageFolder)
                          where !regex.IsMatch(dir)
                          select dir;

            InitVisualDeletion(Resources.DeletionNonVersionnedFoldersStart);

            var folderDeletor = new FolderDeletor();
            folderDeletor.DeletionProgress += ProgressVisualDeletion;
            folderDeletor.DeleteFolders(folders);

            EndVisualDeletion(Resources.DeletionNonVersionnedFoldersEnd);
        }

        private void DeleteAllFolder(string packageFolder)
        {
            if (!Utils.CheckForRepositoryConfig(packageFolder)) return;

            var folders = from dir in Directory.EnumerateDirectories(packageFolder)
                          select dir;

            InitVisualDeletion(Resources.DeletionAllFoldersStart);

            var folderDeletor = new FolderDeletor();
            folderDeletor.DeletionProgress += ProgressVisualDeletion;
            folderDeletor.DeleteFolders(folders);

            EndVisualDeletion(Resources.DeletionAllFoldersEnd);
        }

        #endregion
    }
}
