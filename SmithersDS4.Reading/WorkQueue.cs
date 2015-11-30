using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

// Copied from Body Snap. Move this into Smithers?

namespace SmithersDS4.Reading
{
    public class WorkItem
    {
        public TaskCompletionSource<object> TaskSource { get; set; }
        public Action Action { get; set; }
        public CancellationToken? CancelToken { get; set; }

        public WorkItem() { }

        public void InitializeAction(Action action, CancellationToken? cancelToken)
        {
            Action = action;
            CancelToken = cancelToken;
        }
    }

    public class WorkItemCompletedEventArgs : EventArgs
    {
        public WorkItem CompletedItem { get; set; }

        public WorkItemCompletedEventArgs(WorkItem item)
        {
            this.CompletedItem = item;
        }
    }

    /// <summary>
    /// A P-C Queue with Reused WorkItem
    /// </summary>
    public class WorkQueue : IDisposable
    {
        BlockingCollection<WorkItem> _pendingTaskQueue;
        ConcurrentQueue<WorkItem> _freeItemQueue = new ConcurrentQueue<WorkItem>();

        public event EventHandler<WorkItemCompletedEventArgs> WorkCompleted;

        public int Capcity { get; set; }
        public int Length { get { return _pendingTaskQueue.Count; } }

        public bool isPaused { get; set; }

        public int WorkerCount { get; set; }

        public WorkQueue(int workerCount, int capacity, WorkItem[] items)
        {
            this.Capcity = capacity;

            _pendingTaskQueue = new BlockingCollection<WorkItem>();

            foreach (WorkItem item in items)
                _freeItemQueue.Enqueue(item);

            this.WorkerCount = workerCount;
            this.isPaused = false;
        }

        public void StartWorking()
        {
            for (int i = 0; i < this.WorkerCount; i++)
                Task.Factory.StartNew(Consume);

        }

        /// <summary>
        /// Enter Worker consuming loop. CompleteAdding will notify and break the loop below
        /// </summary>
        private void Consume()
        {
            foreach (WorkItem item in _pendingTaskQueue.GetConsumingEnumerable())
            {
                if (!this.isPaused)
                    item.Action();

                if (WorkCompleted != null)
                {
                    WorkCompleted(this, new WorkItemCompletedEventArgs(item));
                }
            }
        }

        /// <summary>
        /// Enqueue workitem into the queue, but discard the item if the current queue is full
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Task EnqueueTask(WorkItem item)
        {
            item.TaskSource = new TaskCompletionSource<object>();


            if (!_pendingTaskQueue.IsAddingCompleted && _pendingTaskQueue.Count < this.Capcity)
                _pendingTaskQueue.Add(item);

            return item.TaskSource.Task;
        }

        /// <summary>
        /// Enqueue free item for reuse
        /// </summary>
        /// <param name="item"></param>
        public void EnqueueFreeItem(WorkItem item)
        {
            _freeItemQueue.Enqueue(item);
        }

        /// <summary>
        /// Get Free Item from free queue (from the rear)
        /// </summary>
        /// <returns></returns>
        public WorkItem GetFreeItem()
        {
            WorkItem item = null;
            _freeItemQueue.TryDequeue(out item);

            return item;
        }

        /// <summary>
        ///  Signify the queue that no further items coming and the foreach loop should end. 
        ///  http://msdn.microsoft.com/en-us/library/dd287186(v=vs.110).aspx
        /// </summary>
        public void Dispose()
        {
            _pendingTaskQueue.CompleteAdding();

            GC.SuppressFinalize(this);
        }
    }

}
