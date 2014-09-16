using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace MrLuje.LazyPackagesCleaner.Business
{
    public class FolderDeletor
    {
        public delegate void Deletion(uint curr, uint total);

        public event Deletion DeletionProgress;

        private void OnDeletion(uint curr, uint total)
        {
            if (DeletionProgress != null)
                DeletionProgress(curr, total);
        }

        public void DeleteFolders(IEnumerable<string> folders)
        {
            var list = folders.ToList();
            var total = (uint)list.Count();
            var current = 0u;

            OnDeletion(current++, total);

            foreach (var folder in list)
            {
                try
                {
                    DeleteFolder(folder);
                    OnDeletion(current++, total);
                }
                catch (DirectoryNotFoundException) { }
            }
        }

        public void DeleteFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return;

            // Remove readonly on current folder
            var directory = new DirectoryInfo(folderPath) { Attributes = FileAttributes.Normal };

            // Remove readonly on all files & subfolders
            foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                info.Attributes = FileAttributes.Normal;
            }

            // Delete files in current folder
            Array.ForEach(Directory.GetFiles(folderPath), File.Delete);

            // Delete sub folders
            Array.ForEach(Directory.GetDirectories(folderPath), DeleteFolder);

            // "Wait" for previous actions to be finished
            Thread.Sleep(1);

            // Try to delete the folder
            try { Directory.Delete(folderPath); }
            catch { }

            // Try to delete it again if still here
            if (Directory.Exists(folderPath))
            {
                try{Directory.Delete(folderPath, recursive: true);}
                catch { }
            }
        }
    }
}