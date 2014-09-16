using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using MrLuje.LazyPackagesCleaner.Properties;

namespace MrLuje.LazyPackagesCleaner.Business
{
    public class SolutionEventDetector
    {
        private readonly DTE2 _dte;
        private Dictionary<String, Boolean> _solutionsBuilds;
        public event Action FirstBuild;

        public event _dispSolutionEvents_OpenedEventHandler OnSolutionOpened
        {
            add { _dte.Events.SolutionEvents.Opened += value; }
            remove { _dte.Events.SolutionEvents.Opened -= value; }
        }

        public event _dispBuildEvents_OnBuildBeginEventHandler OnBuildBegin
        {
            add { _dte.Events.BuildEvents.OnBuildBegin += value; }
            remove { _dte.Events.BuildEvents.OnBuildBegin -= value; }
        }

        public event _dispSolutionEvents_BeforeClosingEventHandler OnBeforeClosing
        {
            add { _dte.Events.SolutionEvents.BeforeClosing += value; }
            remove { _dte.Events.SolutionEvents.BeforeClosing -= value; }
        }

        public SolutionEventDetector(DTE2 dte)
        {
            _dte = dte;
            _solutionsBuilds = new Dictionary<string, bool>();

            this.OnSolutionOpened += () => SolutionLoad(dte.Solution.FullName);
            this.OnBeforeClosing += () => SolutionClose(dte.Solution.FullName);
        }

        public void SolutionLoad(string solutionPath)
        {
            if (Settings.Default.DebugMode)
                MessageBox.Show("Opened: " + _dte.Solution.FullName);

            if (_solutionsBuilds.ContainsKey(solutionPath)) return;

            _solutionsBuilds.Add(solutionPath, false);
        }

        public void SolutionBuild(string solutionPath)
        {
            if (!_solutionsBuilds.ContainsKey(solutionPath)) return;
            if (_solutionsBuilds[solutionPath]) return;

            OnFirstBuild();

            _solutionsBuilds[solutionPath] = true;
        }

        public void SolutionClose(string solutionPath)
        {
            if (!_solutionsBuilds.ContainsKey(solutionPath)) return;

            _solutionsBuilds.Remove(solutionPath);
        }

        private void OnFirstBuild()
        {
            if (FirstBuild != null)
                FirstBuild();
        }
    }
}
