using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FastSearchNet5.FileSearcher
{
    internal class FileDelegateSearcher: FileSearcherBase
    {
        private Func<FileInfo, bool> isValid;

        public FileDelegateSearcher(string folder, Func<FileInfo, bool> isValid, 
            ExecuteHandlers handlerOption = ExecuteHandlers.InCurrentTask)
            : base(folder, handlerOption)
        {
            this.isValid = isValid;
        }

        public FileDelegateSearcher(string folder): this(folder, (arg) => true)
        {
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
            var resultFiles = new List<FileInfo>();

            try
            {
                dirInfo = new DirectoryInfo(folder);
                directories = dirInfo.GetDirectories();

                if (directories.Length == 0)
                {
                    var files = dirInfo.GetFiles();

                    resultFiles.AddRange(files.Where(file => isValid(file)));

                    if (resultFiles.Count > 0)
                        OnFilesFound(resultFiles);

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
                var files = dirInfo.GetFiles();

                resultFiles.AddRange(files.Where(file => isValid(file)));

                if (resultFiles.Count > 0)
                    OnFilesFound(resultFiles);
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
                var resultFiles = new List<FileInfo>();

                try
                {
                    var dirInfo = new DirectoryInfo(folder);
                    directories = dirInfo.GetDirectories();

                    var files = dirInfo.GetFiles();

                    resultFiles.AddRange(files.Where(file => isValid(file)));

                    if (resultFiles.Count > 0) OnFilesFound(resultFiles);

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
