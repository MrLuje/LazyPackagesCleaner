﻿using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MrLuje.LazyPackagesCleaner.Business
{
    public static class Utils
    {
        public static string FindPackageFolder(string nugetConfigPath, string solutionFolder)
        {
            if (!String.IsNullOrEmpty(nugetConfigPath))
                return ReadPathFromNugetFile(nugetConfigPath);

            string packageFolderTry = Path.Combine(solutionFolder, "packages");
            if (Directory.Exists(packageFolderTry)) return packageFolderTry;

            packageFolderTry = Path.Combine(solutionFolder, "..", "packages");
            if (Directory.Exists(packageFolderTry)) return packageFolderTry;

            return string.Empty;
        }

        public static string ReadPathFromNugetFile(string nugetConfigPath)
        {
            var pathLine = File.ReadAllLines(nugetConfigPath)
                               .First(l => l.Contains(_repositoriesConfig_element_repositorypath));

            var regex = new Regex("(?<=value=\")(.)*\"");
            var result = regex.Match(pathLine);

            return "";
        }

        private const string _repositoriesConfig_FileName = "repositories.config";
        private const string _repositoriesConfig_element_repositorypath = "repositorypath";

        public static bool CheckForRepositoryConfig(string path)
        {
            return File.Exists(Path.Combine(path, _repositoriesConfig_FileName));
        }
    }
}
