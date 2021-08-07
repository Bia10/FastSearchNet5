﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FastSearchNet5.FileSearcher
{
    internal class FilePatternSearcher : FileSearcherBase
    {
        private string pattern;

        public FilePatternSearcher(string folder, string pattern = "*",
            ExecuteHandlers handlerOption = ExecuteHandlers.InCurrentTask) 
            : base(folder, handlerOption)
        {
            this.pattern = pattern;
        }

        /// <summary>
        /// Starts a file search operation with realtime reporting using several threads in thread pool.
        /// </summary>
        public override void StartSearch()
        {
             GetFilesFast();
        }

        protected override void GetFiles(string folder)
        {
            DirectoryInfo dirInfo;
            DirectoryInfo[] directories;

            try
            {
                dirInfo = new DirectoryInfo(folder);
                directories = dirInfo.GetDirectories();

                if (directories.Length == 0)
                {
                    var resFiles = dirInfo.GetFiles(pattern);
                    if (resFiles.Length > 0)
                        OnFilesFound(resFiles.ToList());
                    return;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            catch (PathTooLongException)
            {
                return;
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }

            foreach (var d in directories)
            {
                GetFiles(d.FullName);
            }

            try
            {
                var resFiles = dirInfo.GetFiles(pattern);
                if (resFiles.Length > 0)
                    OnFilesFound(resFiles.ToList());
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (PathTooLongException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        protected override IEnumerable<DirectoryInfo> GetStartDirectories(string folder)
        {
            while (true)
            {
                DirectoryInfo[] directories;

                try
                {
                    var dirInfo = new DirectoryInfo(folder);
                    directories = dirInfo.GetDirectories();

                    var resFiles = dirInfo.GetFiles(pattern);
                    if (resFiles.Length > 0) OnFilesFound(resFiles.ToList());

                    switch (directories.Length)
                    {
                        case > 1:
                            return new List<DirectoryInfo>(directories);
                        case 0:
                            return new List<DirectoryInfo>();
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    return new List<DirectoryInfo>();
                }
                catch (PathTooLongException)
                {
                    return new List<DirectoryInfo>();
                }
                catch (DirectoryNotFoundException)
                {
                    return new List<DirectoryInfo>();
                }

                folder = directories[0].FullName;
            }
        }
    }
}
