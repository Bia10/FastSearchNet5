using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FastSearchNet5.FileSearcher
{
    internal abstract class FileSearcherBase
    {
        /// <summary>
        /// Specifies where FilesFound event handlers are executed.
        /// </summary>
        protected ExecuteHandlers HandlerOption { get; set; }
        protected string folder;
        protected ConcurrentBag<Task> taskHandlers;

        public FileSearcherBase(string folder, ExecuteHandlers handlerOption)
        {
            this.folder = folder;
            this.HandlerOption = handlerOption;
            taskHandlers = new ConcurrentBag<Task>();
        }

        public event EventHandler<FileEventArgs> FilesFound;
        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

        protected virtual void GetFilesFast()
        {
            var startDirs = GetStartDirectories(folder);

            startDirs.AsParallel().ForAll((d) =>
            {
                GetStartDirectories(d.FullName).AsParallel().ForAll((dir) =>
                {
                    GetFiles(dir.FullName);
                });
            });

            OnSearchCompleted(false);
        }

        protected virtual void OnFilesFound(List<FileInfo> files)
        {
            if (HandlerOption == ExecuteHandlers.InNewTask)
            {
                taskHandlers.Add(Task.Run(() => CallFilesFound(files)));
            }
            else
            {
                CallFilesFound(files);
            }
        }

        protected virtual void CallFilesFound(List<FileInfo> files)
        {
            var handler = FilesFound;
            if (handler == null) return;

            var arg = new FileEventArgs(files);
            handler(this, arg);
        }

        protected virtual void OnSearchCompleted(bool isCanceled)
        {
            if (HandlerOption == ExecuteHandlers.InNewTask)
            {
                 Task.WaitAll(taskHandlers.ToArray());   
            }

            CallSearchCompleted(isCanceled);
        }

        protected virtual void CallSearchCompleted(bool isCanceled)
        {
            var handler = SearchCompleted;
            if (handler == null) return;

            var arg = new SearchCompletedEventArgs(isCanceled);
            handler(this, arg);
        }

        protected abstract void GetFiles(string folder);
        protected abstract IEnumerable<DirectoryInfo> GetStartDirectories(string folder);
        public abstract void StartSearch();
    }
}
