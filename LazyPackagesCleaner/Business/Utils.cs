using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.TeamFoundation.Framework.Client;

namespace MrLuje.LazyPackagesCleaner.Business
{
    public static class Utils
    {
        public static string FindPackageFolder(string nugetConfigPath, string solutionFolder)
        {
            if (!String.IsNullOrEmpty(nugetConfigPath))
            {
                var nugetPath = ReadPathFromNugetFile(nugetConfigPath);
                if (!String.IsNullOrEmpty(nugetPath))
                {
                    return Path.Combine(Path.GetDirectoryName(nugetConfigPath), nugetPath);
                }
            }

            var packageFolderTry = Path.Combine(solutionFolder, "packages");
            if (Directory.Exists(packageFolderTry)) return packageFolderTry;

            packageFolderTry = Path.Combine(solutionFolder, "..", "packages");
            if (Directory.Exists(packageFolderTry)) return packageFolderTry;

            return string.Empty;
        }

        public static string ReadPathFromNugetFile(string nugetConfigPath)
        {
            try
            {
                var configXml = XElement.Load(nugetConfigPath).Element("config").Element("add");
                if (configXml.Attribute("key").Value.ToLower() == _repositoriesConfig_element_repositorypath)
                {
                    return configXml.Attribute("value").Value;
                }

                return "";
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        private const string _repositoriesConfig_FileName = "repositories.config";
        private const string _repositoriesConfig_element_repositorypath = "repositorypath";

        public static bool CheckForRepositoryConfig(string path)
        {
            return File.Exists(Path.Combine(path, _repositoriesConfig_FileName));
        }
    }
}
