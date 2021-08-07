using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace FastSearchNet5.DirectorySearcher
{
    internal class DirectoryCancellationPatternSearcher : DirectoryCancellationSearcherBase
    {
        private string pattern;

        public DirectoryCancellationPatternSearcher(string folder, string pattern, CancellationToken token, ExecuteHandlers handlerOption, bool suppressOperationCanceledException)
            : base(folder, token, handlerOption, suppressOperationCanceledException)
        {
            this.pattern = pattern;
        }

        protected override void GetDirectories(string folder)
        {
            token.ThrowIfCancellationRequested();

            DirectoryInfo dirInfo;
            DirectoryInfo[] directories;

            try
            {
                dirInfo = new DirectoryInfo(folder);
                directories = dirInfo.GetDirectories();

                if (directories.Length == 0) return;
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
                token.ThrowIfCancellationRequested();

                GetDirectories(d.FullName);
            }

            token.ThrowIfCancellationRequested();

            try
            {
                var resultDirs = dirInfo.GetDirectories(pattern);
                if (resultDirs.Length > 0)
                    OnDirectoriesFound(resultDirs.ToList());
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

        protected override List<DirectoryInfo> GetStartDirectories(string folder)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                DirectoryInfo dirInfo;
                DirectoryInfo[] directories;
                DirectoryInfo[] resultDirs;

                try
                {
                    dirInfo = new DirectoryInfo(folder);
                    directories = dirInfo.GetDirectories();

                    switch (directories.Length)
                    {
                        case > 1:
                        {
                            resultDirs = dirInfo.GetDirectories(pattern);
                            if (resultDirs.Length > 0) OnDirectoriesFound(resultDirs.ToList());

                            return new List<DirectoryInfo>(directories);
                        }
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

                // if directories.Length == 1
                resultDirs = dirInfo.GetDirectories(pattern);
                if (resultDirs.Length > 0) OnDirectoriesFound(resultDirs.ToList());

                folder = directories[0].FullName;
            }
        }
    }
}
