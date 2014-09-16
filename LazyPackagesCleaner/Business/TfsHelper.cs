using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace MrLuje.LazyPackagesCleaner.Business
{
    public static class TfsHelper
    {
        public static Workspace GetWorkspace(string fullpath)
        {
            var workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(fullpath);
            if (workspaceInfo == null) return null;
            var server = new TfsTeamProjectCollection(workspaceInfo.ServerUri);
            return workspaceInfo.GetWorkspace(server);
        }

        public static void CheckoutFile(string fullpath, Workspace workspace = null)
        {
            if (workspace == null)
                workspace = GetWorkspace(fullpath);

            if (workspace != null)
                workspace.PendEdit(fullpath);
        }
    }
}
