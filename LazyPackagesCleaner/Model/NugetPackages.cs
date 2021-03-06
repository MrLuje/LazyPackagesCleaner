﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace MrLuje.LazyPackagesCleaner.Model
{
    public class NugetPackages
    {
        private readonly Dictionary<String, List<String>> _dico;

        public NugetPackages()
        {
            _dico = new Dictionary<string, List<string>>();
        }

        public void AddVersion(string package, string version)
        {
            if (_dico.ContainsKey(package))
            {
                if (_dico[package].Contains(version)) return;
                _dico[package].Add(version);
            }
            else
            {
                _dico.Add(package, new List<string>
                {
                    version
                });
            }
        }

        public List<String> GetVersions(string package)
        {
            return _dico[package];
        }

        public bool HasConflictingVersions()
        {
            return _dico.Any(kvp => kvp.Value.Count > 1);
        }

        public IEnumerable<PackageVersions> GetConflictingVersions()
        {
            return _dico.Where(kvp => kvp.Value.Count > 1)
                        .Select(kvp => new PackageVersions(kvp.Key)
                        {
                            Versions = kvp.Value
                        });
        }
    }
}
