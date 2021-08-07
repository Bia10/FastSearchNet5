using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastSearchNet5.DirectorySearcher
{
    /// <summary>
    /// Represents a class for fast directory search.
    /// </summary>
    public class DirectorySearcher
    {
        #region Instance members
        private DirectoryCancellationSearcherBase searcher;
        private CancellationTokenSource tokenSource;

        /// <summary>
        /// Event fires when next portion of directories is found. Event handlers are not thread safe. 
        /// </summary>
        public event EventHandler<DirectoryEventArgs> DirectoriesFound
        {
            add => searcher.DirectoriesFound += value;
            remove => searcher.DirectoriesFound -= value;
        }

        /// <summary>
        /// Event fires when search process is completed or stopped.
        /// </summary>
        public event EventHandler<SearchCompletedEventArgs> SearchCompleted
        {
            add => searcher.SearchCompleted += value;
            remove => searcher.SearchCompleted -= value;
        }

        #region DirectoryCancellationPatternSearcher constructors
        /// <summary>
        /// Initialize a new instance of DirectorySearch class. 
        /// </summary>
        /// <param name="folder">The start search directory.</param>
        /// <param name="pattern">The search pattern.</param>
        /// <param name="tokenSource">Instance of CancellationTokenSource for search process cancellation possibility.</param>
        /// <param name="handlerOption">Specifies where DirectoriesFound event handlers are executed.</param>
        /// <param name="suppressOperationCanceledException">Determines whether necessary suppress OperationCanceledException if it possible.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public DirectorySearcher(string folder, string pattern, CancellationTokenSource tokenSource, 
            ExecuteHandlers handlerOption = ExecuteHandlers.InCurrentTask, bool suppressOperationCanceledException = true)
        {
            CheckFolder(folder);
            CheckPattern(pattern);
            CheckTokenSource(tokenSource);

            searcher = new DirectoryCancellationPatternSearcher(folder, pattern, tokenSource.Token,
                handlerOption, suppressOperationCanceledException);
            this.tokenSource = tokenSource;
        }

        /// <summary>
        /// Initialize a new instance of DirectorySearch class. 
        /// </summary>
        /// <param name="folder">The start search directory.</param>
        /// <param name="tokenSource">Instance of CancellationTokenSource for search process cancellation possibility.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public DirectorySearcher(string folder, CancellationTokenSource tokenSource)
            : this (folder, "*", tokenSource)
        {
        }
        #endregion

        #region DirectoryCancellationDelegateSearcher constructors
        /// <summary>
        /// Initialize a new instance of DirectorySearch class.
        /// </summary>
        /// <param name="folder">The start search directory.</param>
        /// <param name="isValid">The delegate that determines algorithm of directory selection.</param>
        /// <param name="tokenSource">Instance of CancellationTokenSource for search process cancellation possibility.</param>
        /// <param name="handlerOption">Specifies where DirectoriesFound event handlers are executed.</param>
        /// <param name="suppressOperationCanceledException">Determines whether necessary suppress OperationCanceledException if it possible.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public DirectorySearcher(string folder, Func<DirectoryInfo, bool> isValid, CancellationTokenSource tokenSource, 
            ExecuteHandlers handlerOption = ExecuteHandlers.InCurrentTask, bool suppressOperationCanceledException = true)
        {
            CheckFolder(folder);
            CheckDelegate(isValid);
            CheckTokenSource(tokenSource);

            searcher = new DirectoryCancellationDelegateSearcher(folder, isValid, tokenSource.Token,
                handlerOption, suppressOperationCanceledException);
            this.tokenSource = tokenSource;
        }

        /// <summary>
        /// Initialize a new instance of DirectorySearch class.
        /// </summary>
        /// <param name="folder">The start search directory.</param>
        /// <param name="isValid">The delegate that determines algorithm of directory selection.</param>
        /// <param name="tokenSource">Instance of CancellationTokenSource for search process cancellation possibility.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public DirectorySearcher(string folder, Func<DirectoryInfo, bool> isValid, CancellationTokenSource tokenSource)
            : this(folder, isValid, tokenSource, ExecuteHandlers.InCurrentTask)
        {
        }
        #endregion
        
        #region Checking methods
        private static void CheckFolder(string folder)
        {
            switch (folder)
            {
                case null:
                    throw new ArgumentNullException(nameof(folder), "Argument is null.");
                case "":
                    throw new ArgumentException("Argument is not valid.", nameof(folder));
            }

            var dir = new DirectoryInfo(folder);
            if (!dir.Exists)
                throw new ArgumentException("Argument does not represent an existing directory.", nameof(folder));
        }

        private static void CheckPattern(string pattern)
        {
            switch (pattern)
            {
                case null:
                    throw new ArgumentNullException(nameof(pattern), "Argument is null.");
                case "":
                    throw new ArgumentException("Argument is not valid.", nameof(pattern));
            }
        }

        private static void CheckDelegate(Func<DirectoryInfo, bool> isValid)
        {
            if (isValid == null)
                throw new ArgumentNullException(nameof(isValid), "Argument is null.");
        }

        private static void CheckTokenSource(CancellationTokenSource tokenSource)
        {
            if (tokenSource == null)
                throw new ArgumentNullException(nameof(tokenSource), "Argument \"tokenSource\" is null.");
        }
        #endregion

        /// <summary>
        /// Starts a directory search operation with realtime reporting using several threads in thread pool.
        /// </summary>
        public void StartSearch()
        {
            searcher.StartSearch();
        }

        /// <summary>
        /// Starts a directory search operation with realtime reporting using several threads in thread pool as an asynchronous operation.
        /// </summary>
        public Task StartSearchAsync()
        {
            return Task.Run(StartSearch, tokenSource.Token);
        }

        /// <summary>
        /// Stops a directory search operation.
        /// </summary>
        public void StopSearch()
        {
            tokenSource.Cancel();
        }
        #endregion

        #region Static members
        #region Public members
        /// <summary>
        /// Returns a list of directories that are contained in directory and all subdirectories.
        /// </summary>
        /// <param name="folder">The start search directory.</param>
        /// <param name="pattern">The search pattern.</param>
        /// <returns>List of finding directories.</returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public static List<DirectoryInfo> GetDirectories(string folder, string pattern = "*")
        {
            var directories = new List<DirectoryInfo>();
            GetDirectories(folder, directories, pattern);

            return directories;
        }

        /// <summary>
        /// Returns a list of directories that are contained in directory and all subdirectories.
        /// </summary>
        /// <param name="folder">The start search directory.</param>
        /// <param name="isValid">The delegate that determines algorithm of directory selection.</param>
        /// <returns>List of finding directories.</returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public static List<DirectoryInfo> GetDirectories(string folder, Func<DirectoryInfo, bool> isValid)
        {
            var directories = new List<DirectoryInfo>();
            GetDirectories(folder, directories, isValid);

            return directories;
        }

        /// <summary>
        /// Returns a list of directories that are contained in directory and all subdirectories as an asynchronous operation.
        /// </summary>
        /// <param name="folder">The start search directory.</param>
        /// <param name="pattern">The search pattern.</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public static Task<List<DirectoryInfo>> GetDirectoriesAsync(string folder, string pattern = "*")
        {
            return Task.Run(() 
                => GetDirectories(folder, pattern));
        }

        /// <summary>
        /// Returns a list of directories that are contained in directory and all subdirectories as an asynchronous operation.
        /// </summary>
        /// <param name="folder">The start search directory.</param>
        /// <param name="isValid">The delegate that determines algorithm of directory selection.</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public static Task<List<DirectoryInfo>> GetDirectoriesAsync(string folder, Func<DirectoryInfo, bool> isValid)
        {
            return Task.Run(() 
                => GetDirectories(folder, isValid));
        }

        /// <summary>
        /// Returns a list of directories that are contained in directory and all subdirectories using several threads in thread pool.
        /// </summary>
        /// <param name="folder">The start search directory.</param>
        /// <param name="pattern">The search pattern.</param>
        /// <returns>List of finding directories.</returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public static List<DirectoryInfo> GetDirectoriesFast(string folder, string pattern = "*")
        {
            var dirs = new ConcurrentBag<DirectoryInfo>();

            var startDirs = GetStartDirectories(folder, dirs, pattern);

            startDirs.AsParallel().ForAll((d) =>
            {
                GetStartDirectories(d.FullName, dirs, pattern).AsParallel().ForAll((dir) =>
                {
                    GetDirectories(dir.FullName, pattern).ForEach((r) => dirs.Add(r));
                });
            });

            return dirs.ToList();
        }

        /// <summary>
        /// Returns a list of directories that are contained in directory and all subdirectories using several threads in thread pool.
        /// </summary>
        /// <param name="folder">The start search directory.</param>
        /// <param name="isValid">The delegate that determines algorithm of directory selection.</param>
        /// <returns>List of finding directories.</returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public static List<DirectoryInfo> GetDirectoriesFast(string folder, Func<DirectoryInfo, bool> isValid)
        {
            var dirs = new ConcurrentBag<DirectoryInfo>();

            var startDirs = GetStartDirectories(folder, dirs, isValid);

            startDirs.AsParallel().ForAll((d) =>
            {
                GetStartDirectories(d.FullName, dirs, isValid).AsParallel().ForAll((dir) =>
                {
                    GetDirectories(dir.FullName, isValid).ForEach((r) => dirs.Add(r));
                });
            });

            return dirs.ToList();
        }

        /// <summary>
        /// Returns a list of directories that are contained in directory and all subdirectories using several threads in thread pool as an asynchronous operation.
        /// </summary>
        /// <param name="folder">The start search directory.</param>
        /// <param name="pattern">The search pattern.</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public static Task<List<DirectoryInfo>> GetDirectoriesFastAsync(string folder, string pattern = "*")
        {
            return Task.Run(()
                => GetDirectoriesFast(folder, pattern));
        }

        /// <summary>
        /// Returns a list of directories that are contained in directory and all subdirectories using several threads in thread pool as an asynchronous operation.
        /// </summary>
        /// <param name="folder">The start search directory.</param>
        /// <param name="isValid">The delegate that determines algorithm of directory selection.</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public static Task<List<DirectoryInfo>> GetDirectoriesFastAsync(string folder, Func<DirectoryInfo, bool> isValid)
        {
            return Task.Run(()
                => GetDirectoriesFast(folder, isValid));
        }
        #endregion

        #region Private members
        private static void GetDirectories(string folder, ICollection<DirectoryInfo> result, string pattern)
        {
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

            Array.ForEach(directories, (d) 
                => GetDirectories(d.FullName, result, pattern));

            try
            {
                Array.ForEach(dirInfo.GetDirectories(pattern), result.Add);
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        private static void GetDirectories(string folder, ICollection<DirectoryInfo> result,
            Func<DirectoryInfo, bool> isValid)
        {
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

            Array.ForEach(directories, (d) 
                => GetDirectories(d.FullName, result, isValid));

            try
            {
                Array.ForEach(dirInfo.GetDirectories(), (d) =>
                {
                    if (isValid(d))
                        result.Add(d);
                });
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

        private static IEnumerable<DirectoryInfo> GetStartDirectories(string folder, ConcurrentBag<DirectoryInfo> dirs,
            string pattern)
        {
            while (true)
            {
                DirectoryInfo dirInfo;
                DirectoryInfo[] directories;

                try
                {
                    dirInfo = new DirectoryInfo(folder);
                    directories = dirInfo.GetDirectories();

                    switch (directories.Length)
                    {
                        case > 1:
                            Array.ForEach(dirInfo.GetDirectories(pattern), dirs.Add);
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

                // if directories.Length == 1
                Array.ForEach(dirInfo.GetDirectories(pattern), dirs.Add);

                folder = directories[0].FullName;
            }
        }

        private static List<DirectoryInfo> GetStartDirectories(string folder, ConcurrentBag<DirectoryInfo> dirs,
            Func<DirectoryInfo, bool> isValid)
        {
            DirectoryInfo[] directories;
            try
            {
                var dirInfo = new DirectoryInfo(folder);
                directories = dirInfo.GetDirectories();

                switch (directories.Length)
                {
                    case > 1:
                        Array.ForEach(directories, (d) =>
                        {
                            if (isValid(d))
                                dirs.Add(d);
                        });

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

            // if directories.Length == 1
            Array.ForEach(directories, (d) =>
            {
                if (isValid(d))
                    dirs.Add(d);
            });

            return GetStartDirectories(directories[0].FullName, dirs, isValid);
        }
        #endregion
        #endregion
    }
}
