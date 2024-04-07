using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PTSharpCore
{
    public class WorkStealingScheduler : TaskScheduler, IDisposable
    {
        private BlockingCollection<Task> _tasks = new BlockingCollection<Task>();
        private readonly Thread[] _threads;

        public WorkStealingScheduler(int concurrencyLevel)
        {
            _threads = new Thread[concurrencyLevel];
            for (int i = 0; i < concurrencyLevel; i++)
            {
                var thread = new Thread(() =>
                {
                    foreach (var task in _tasks.GetConsumingEnumerable())
                    {
                        TryExecuteTask(task);
                    }
                });
                thread.IsBackground = true;
                thread.Start();
                _threads[i] = thread;
            }
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (Thread.CurrentThread == _threads[0])
                return TryExecuteTask(task);

            return false;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks.ToArray();
        }

        public override int MaximumConcurrencyLevel => _threads.Length;

        public void Dispose()
        {
            _tasks.CompleteAdding();

            foreach (var thread in _threads)
            {
                thread.Join();
            }
        }
    }
}
