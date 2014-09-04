// Guids.cs
// MUST match guids.h
using System;

namespace MrLuje.LazyPackagesCleaner
{
    static class GuidList
    {
        public const string guidLazyPackagesCleanerPkgString = "4e4e3df2-3bbf-4d3b-8d87-6452f76fb6da";
        public const string guidLazyPackagesCleanerCmdSetString = "89edcd68-4eec-4260-8fa0-2226ee11e564";

        public static readonly Guid guidLazyPackagesCleanerCmdSet = new Guid(guidLazyPackagesCleanerCmdSetString);
    };
}