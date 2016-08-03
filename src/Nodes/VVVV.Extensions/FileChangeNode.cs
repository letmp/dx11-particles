#region usings
using System;
using System.ComponentModel.Composition;
using System.IO;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Extensions
{
    #region PluginInfo
    [PluginInfo(Name = "Change", Category = "file", Version = "", Help = "Checks if a file was changed or renamed", Tags = "", Author = "tmp")]
    #endregion PluginInfo
    public class FileChangeNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Filename", StringType = StringType.Filename, IsSingle = true)]
        public ISpread<string> FPath;

        [Output("Changed", IsBang = true)]
        public ISpread<bool> FChanged;

        [Output("Renamed", IsBang = true)]
        public ISpread<bool> FRenamed;

        //[Import()]
        //public ILogger FLogger;
        #endregion fields & pins

        private bool changed = false;
        private bool renamed = false;

        public void Evaluate(int SpreadMax)
        {
            FChanged.SliceCount = FRenamed.SliceCount = 1;

            if (FPath.IsChanged && FPath[0].Length > 0) CreateFileWatcher(FPath[0]);
            
            FChanged[0] = changed;
            FRenamed[0] = renamed;
            if (changed) {  changed = false; }
            if (renamed) { renamed = false; }

        }

        public void CreateFileWatcher(string path)
        {
            // Create a new FileSystemWatcher and set its properties.
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = Path.GetDirectoryName(path);
            /* Watch for changes in LastAccess and LastWrite times, and 
               the renaming of files or directories. */
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            
            // Only watch text files.
            watcher.Filter = Path.GetFileName(path);

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

            // Begin watching.
            watcher.EnableRaisingEvents = true;

            this.changed = true;
        }

        public void OnChanged(object source, FileSystemEventArgs e)
        {
            //FLogger.Log(LogType.Debug, "File: " + e.FullPath + " " + e.ChangeType);
            this.changed = true;
        }

        public void OnRenamed(object source, RenamedEventArgs e)
        {
            //FLogger.Log(LogType.Debug, "File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
            this.renamed = true;
        }
    }
}
