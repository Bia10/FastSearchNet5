using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastSearchNet5.FileSearcher
{
    internal abstract class FileCancellationSearcherBase : FileSearcherBase
    {
        protected CancellationToken Token;
        protected bool SuppressOperationCanceledException { get; set; }

        public FileCancellationSearcherBase(string folder, 
            CancellationToken token, ExecuteHandlers handlerOption, bool suppressOperationCanceledException) 
            : base(folder, handlerOption)
        {
            Token = token;
            SuppressOperationCanceledException = suppressOperationCanceledException;
        }

        public override void StartSearch()
        {
            try
            {
                GetFilesFast();
            }
            catch (OperationCanceledException)
            {
                OnSearchCompleted(true); // isCanceled == true
                                         
                if (!SuppressOperationCanceledException)
                    Token.ThrowIfCancellationRequested();

                return;
            }

            OnSearchCompleted(false); 
        }

        protected override void OnFilesFound(List<FileInfo> files)
        {
            var arg = new FileEventArgs(files);

            if (HandlerOption == ExecuteHandlers.InNewTask)
                taskHandlers.Add(Task.Run(()
                    => CallFilesFound(files), Token));
            else
                CallFilesFound(files);
        }

        protected override void OnSearchCompleted(bool isCanceled)
        {
            if (HandlerOption == ExecuteHandlers.InNewTask)
            {
                try
                {
                    Task.WaitAll(taskHandlers.ToArray());
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerException is not TaskCanceledException)
                        throw;

                    if (!isCanceled)
                        isCanceled = true;
                }

                CallSearchCompleted(isCanceled);           
            }
            else
                CallSearchCompleted(isCanceled);
        }

        protected override void GetFilesFast()
        {
            var startDirs = GetStartDirectories(folder);

            startDirs.AsParallel().WithCancellation(Token).ForAll((d) =>
            {
                GetStartDirectories(d.FullName).AsParallel().WithCancellation(Token).ForAll((dir) =>
                {
                    GetFiles(dir.FullName);
                });
            });
        }
    }
}
