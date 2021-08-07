using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace FastSearchNet5.FileSearcher
{
    internal class FileCancellationDelegateSearcher : FileCancellationSearcherBase
    {
        private Func<FileInfo, bool> isValid;

        public FileCancellationDelegateSearcher(string folder, Func<FileInfo, bool> isValid, 
            CancellationToken token, ExecuteHandlers handlerOption, bool suppressOperationCanceledException)
            : base(folder, token, handlerOption, suppressOperationCanceledException)
        {
            this.isValid = isValid;
        }

        protected override void GetFiles(string folder)
        {
            Token.ThrowIfCancellationRequested();

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
                Token.ThrowIfCancellationRequested();

                GetFiles(d.FullName);
            }

            Token.ThrowIfCancellationRequested();

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
                Token.ThrowIfCancellationRequested();

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
