using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MrLuje.LazyPackagesCleaner.Properties;

namespace MrLuje.LazyPackagesCleaner.GUI
{
    public class SettingsTool : DialogPage
    {
        [DisplayName("Enable delete on clean")]
        [Description("Delete non-versionned folders on Clean Solution")]
        public bool EnableDeleteOnClean
        {
            get { return Settings.Default.EnableDeleteOnClean; }
            set { Settings.Default.EnableDeleteOnClean = value; }
        }

        [DisplayName("Enable delete on first builds")]
        [Description("Delete non-versionned folders the first time a solution is built after being opened")]
        public bool EnableDeleteOnFirstBuild
        {
            get { return Settings.Default.EnableDeleteOnFirstBuild; }
            set { Settings.Default.EnableDeleteOnFirstBuild = value; }
        }

        [DisplayName("Enable package clean on fix config")]
        [Description("Delete all package folders after \"Fix packages config\"")]
        public bool EnableFullPackageCleanOnFixConfig
        {
            get { return Settings.Default.EnableFullPackageCleanOnFixConfig; }
            set { Settings.Default.EnableFullPackageCleanOnFixConfig = value; }
        }

        public bool DebugMode
        {
            get { return Settings.Default.DebugMode; }
            set { Settings.Default.DebugMode = value; }
        }
    }
}
