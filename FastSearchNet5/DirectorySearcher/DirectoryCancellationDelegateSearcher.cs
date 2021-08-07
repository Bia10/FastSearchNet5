using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace FastSearchNet5.DirectorySearcher
{
    internal class DirectoryCancellationDelegateSearcher : DirectoryCancellationSearcherBase
    {
        private Func<DirectoryInfo, bool> isValid;

        public DirectoryCancellationDelegateSearcher(string folder, Func<DirectoryInfo, bool> isValid,
            CancellationToken token, ExecuteHandlers handlerOption, bool suppressOperationCanceledException) 
            : base(folder, token, handlerOption, suppressOperationCanceledException)
        {
            this.isValid = isValid;
        }

        protected override void GetDirectories(string folder)
        {
            token.ThrowIfCancellationRequested();

            DirectoryInfo[] directories;

            try
            {
                var dirInfo = new DirectoryInfo(folder);
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


            foreach (var dir in directories)
            {
                token.ThrowIfCancellationRequested();

                GetDirectories(dir.FullName);
            }

            token.ThrowIfCancellationRequested();

            try
            {
                var resultDirs = directories.Where(dir
                    => isValid(dir)).ToList();

                if (resultDirs.Count > 0)
                    OnDirectoriesFound(resultDirs);
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

                DirectoryInfo[] directories;
                var resultDirs = new List<DirectoryInfo>();

                try
                {
                    var dirInfo = new DirectoryInfo(folder);
                    directories = dirInfo.GetDirectories();

                    switch (directories.Length)
                    {
                        case > 1:
                        {
                            resultDirs.AddRange(directories.Where(dir 
                                => isValid(dir)));

                            if (resultDirs.Count > 0) OnDirectoriesFound(resultDirs);

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
                foreach (var dir in directories)
                {
                    if (isValid(dir)) OnDirectoriesFound(new List<DirectoryInfo> { dir });
                }

                folder = directories[0].FullName;
            }
        }
    }
}
