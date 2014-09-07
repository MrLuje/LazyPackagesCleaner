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
        [Description("Delete non-versionned folders on Clean Solution")]
        public bool EnableDeleteOnClean
        {
            get { return Settings.Default.EnableDeleteOnClean; }
            set { Settings.Default.EnableDeleteOnClean = value; }
        }

        [Description("Delete non-versionned folders the first time a solution is built after being opened")]
        public bool EnableDeleteOnFirstBuild
        {
            get { return Settings.Default.EnableDeleteOnFirstBuild; }
            set { Settings.Default.EnableDeleteOnFirstBuild = value; }
        }

        public bool DebugMode
        {
            get { return Settings.Default.DebugMode; }
            set { Settings.Default.DebugMode = value; }
        }
    }
}
