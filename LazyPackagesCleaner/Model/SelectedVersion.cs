using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrLuje.LazyPackagesCleaner.Model
{
    public class SelectedVersion
    {
        public string PackageName { get; set; }

        public string PackageVersion { get; set; }

        public SelectedVersion(string name, string version)
        {
            this.PackageName = name;
            this.PackageVersion = version;
        }
    }
}
