using FastSearchNet5.FileSearcher;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace FastSearchNet5.TestConsole
{
    internal static class Program
    {
        private static readonly object locker = new();
        private static List<FileInfo> files;
        private static Stopwatch stopWatch;

        private static void Main()
        {
            const string searchPattern = "localhost v95.exe";
            StartSearch(searchPattern);

            Console.ReadKey(true);
        }

        private static async void StartSearch(string pattern)
        {
            stopWatch = new Stopwatch();
            stopWatch.Start();

            Console.WriteLine("Search had been started.\n");

            files = new List<FileInfo>();

            var searchDirectories = new List<string>
            {
               @"C:\",
               @"D:\"
            };

            var searcher = new FileSearcherMultiple(searchDirectories, (f) 
                => Regex.IsMatch(f.Name, pattern), new CancellationTokenSource());

            searcher.FilesFound += Searcher_FilesFound;
            searcher.SearchCompleted += Searcher_SearchCompleted;

            try
            {
                await searcher.StartSearchAsync();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException != null) Console.WriteLine($"Error occurred: {ex.InnerException.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
            }
            finally
            {
                Console.Write("\nPress any key to continue...");
            }
        }

        private static void Searcher_FilesFound(object sender, FileEventArgs arg)
        {
            lock (locker)
            {
                arg.Files.ForEach((f) =>
                {
                    files.Add(f); 
                    Console.WriteLine($"File location: {f.FullName}\nCreation.Time: {f.CreationTime}\n");
                });
            }
        }

        private static void Searcher_SearchCompleted(object sender, SearchCompletedEventArgs arg)
        {
            stopWatch.Stop();

            Console.WriteLine(arg.IsCanceled ? "Search stopped." : "Search completed.");
            Console.WriteLine($"Files found: {files.Count}"); 
            Console.WriteLine($"Time spent: {stopWatch.Elapsed.Minutes} min {stopWatch.Elapsed.Seconds} s {stopWatch.Elapsed.Milliseconds} ms");
        }
    }
}
