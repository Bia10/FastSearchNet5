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
            files = new List<FileInfo>();

            var searchDirectories = new List<string>();
            var myDrives = DriveInfo.GetDrives();

            foreach (var drive in myDrives)
            {
                if (drive.IsReady != true) continue;

                Console.WriteLine("Adding disk new disk to search:" +
                                  "\n Name: {0} \n Type: {1} \n Label: {2} \n File System: {3} \n",
                                  drive.Name, drive.DriveType, drive.VolumeLabel, drive.DriveFormat);

                searchDirectories.Add(drive.Name);
            }

            Console.WriteLine("Search had been started. \n");

            stopWatch = new Stopwatch();
            stopWatch.Start();

            var searcher = new FileSearcherMultiple(searchDirectories, f 
                => Regex.IsMatch(f.Name, pattern), new CancellationTokenSource());

            searcher.FilesFound += SearcherFilesFound;
            searcher.SearchCompleted += SearcherSearchCompleted;

            try
            {
                await searcher.StartSearchAsync();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException != null) 
                    Console.WriteLine($"Error occurred: {ex.InnerException.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
            }
            finally
            {
                Console.Write("\n Press any key to continue...");
            }
        }

        private static void SearcherFilesFound(object sender, FileEventArgs arg)
        {
            lock (locker)
            {
                arg.Files.ForEach(f =>
                {
                    files.Add(f); 
                    Console.WriteLine($"File location: {f.FullName} \n Creation.Time: {f.CreationTime} \n");
                });
            }
        }

        private static void SearcherSearchCompleted(object sender, SearchCompletedEventArgs arg)
        {
            stopWatch.Stop();

            Console.WriteLine(arg.IsCanceled ? "Search stopped." : "Search completed.");
            Console.WriteLine($"Files found: {files.Count}"); 
            Console.WriteLine($"Time spent: {stopWatch.Elapsed.Minutes} min {stopWatch.Elapsed.Seconds} s {stopWatch.Elapsed.Milliseconds} ms");
        }
    }
}
