using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Client
{
    public class WorkCompletedEventArgs<TWorkItem, TResult> :EventArgs
    {
        public TWorkItem WorkItem {get; set;}
        public TResult Result {get; set;}
    }

    /// <summary>
    /// Async Work Queue where workers execute an async function
    /// predicate provided by the user on the workitem in the queue.
    /// Provide callback to handle each async result of the items.
    /// </summary>
    /// <typeparam name="TWorkItem"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public class AsyncWorkQueue<TWorkItem, TResult>
    {
        BlockingCollection<Tuple<TWorkItem, Func<TWorkItem, Task<TResult>>>> _pendingTaskQueue;

        public event EventHandler<WorkCompletedEventArgs<TWorkItem, TResult>> WorkCompleted;

        public int WorkerCount { get; set; }
        public AsyncWorkQueue(int workerCount)
        {
            this.WorkerCount = workerCount;

            _pendingTaskQueue = new BlockingCollection<Tuple<TWorkItem, Func<TWorkItem, Task<TResult>>>>();
        }

        public void StartWorking()
        {
            for (int i = 0; i < this.WorkerCount; i++)
                Task.Factory.StartNew(Consume);
        }

        public void EnqueueWork(TWorkItem item, Func<TWorkItem, Task<TResult>> startWorkFunc)
        {
            var workItem = new Tuple<TWorkItem, Func<TWorkItem, Task<TResult>>>(item, startWorkFunc);
            
            _pendingTaskQueue.Add(workItem);
        }

        private async void Consume()
        {
            foreach (var item in _pendingTaskQueue.GetConsumingEnumerable())
            {
                TResult result = await item.Item2(item.Item1);

                //Pass result to external handler

                if (WorkCompleted != null)
                    WorkCompleted(this, new WorkCompletedEventArgs<TWorkItem, TResult>()
                    {
                        WorkItem = item.Item1,
                        Result = result
                    });

            }
        }

    }
}
