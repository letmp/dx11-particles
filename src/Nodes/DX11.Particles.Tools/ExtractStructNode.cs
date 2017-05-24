using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace DX11.Particles.Tools
{
    #region PluginInfo
    [PluginInfo(Name = "ExtractStruct", Category = "DX11.Particles.Tools", Help = "Extracts variables from the particle struct.", Tags = "", Author = "tmp")]
    #endregion PluginInfo
    public class ExtractStructNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Filename", StringType = StringType.Filename, IsSingle = true)]
        public IDiffSpread<string> FInPath;

        [Input("Struct Name", IsSingle = true, DefaultString = "Particle")]
        public ISpread<string> FInStructName;

        [Output("Variables")]
        public ISpread<string> FOutVariables;

        private bool changed = false;
        private bool renamed = false;
        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (FInPath.IsChanged)
            {
                for (int i = 0; i < FInPath.SliceCount; i++) { 
                    if (FInPath[i].Length > 0) CreateFileWatcher(FInPath[i]);
                }
            }

            if (changed || renamed)
            {
                FOutVariables.SliceCount = 0;

                for (int i = 0; i < FInPath.SliceCount; i++)
                {
                    string line;
                    if (File.Exists(FInPath[i]))
                    {
                        StreamReader file = new StreamReader(FInPath[i]);
                        bool insideStruct = false;
                        bool insideVarDefinition = false;
                        while ((line = file.ReadLine()) != null)
                        {
                            if (line.Contains("struct " + FInStructName[0])) { insideStruct = true; continue; } // struct begins
                            if (insideStruct && line.Contains("#else")) { insideVarDefinition = true; continue; } // variable definition begins

                            if (insideVarDefinition) // extract variables
                            {
                                string variable = line.Substring(0, Math.Max(line.IndexOf(';') + 1, 0));
                                variable = Regex.Replace(variable, @"\s\s", ""); // remove succeding whitespaces
                                variable = Regex.Replace(variable, @"\t", ""); // remove tabs
                                if ( variable != "") FOutVariables.Add(variable);
                            }

                            if (insideVarDefinition && line.Contains("#endif")) { insideVarDefinition = false; break; }// variable definition ends - we can stop search
                        }
                        file.Close();
                    }
                }
                
                changed = false;
                renamed = false;
            }

        }

        public void CreateFileWatcher(string path)
        {
            // Create a new FileSystemWatcher and set its properties.
            System.IO.FileSystemWatcher watcher = new FileSystemWatcher();
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
