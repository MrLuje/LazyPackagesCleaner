using System;
using System.Collections.Generic;

namespace MrLuje.LazyPackagesCleaner.Model
{
    public class PackageVersions
    {
        public string PackageName { get; set; }

        public List<String> Versions { get; set; }

        public PackageVersions(string name)
        {
            this.PackageName = name;
        }
    }
}
