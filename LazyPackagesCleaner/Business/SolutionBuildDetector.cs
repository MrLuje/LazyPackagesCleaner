using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace MrLuje.LazyPackagesCleaner
{
    public class SolutionBuildDetector
    {
        private Dictionary<String, Boolean> _solutionsBuilds;
        public event Action FirstBuild;

        public SolutionBuildDetector()
        {
            _solutionsBuilds = new Dictionary<string, bool>();
        }

        public void SolutionLoad(string solutionPath)
        {
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
