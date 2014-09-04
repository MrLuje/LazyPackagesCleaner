// PkgCmdID.cs
// MUST match PkgCmdID.h
using System;

namespace MrLuje.LazyPackagesCleaner
{
    static class PkgCmdIDList
    {
        public const uint cmdDeleteNonVersionned = 0x100;
        public const uint cmdDeleteAll = 0x200;
        public const uint cmdOpenPackages = 0x300;

    };
}