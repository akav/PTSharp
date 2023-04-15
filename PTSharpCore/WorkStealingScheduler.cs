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
        private readonly List<Thread> _threads = new List<Thread>();

        public WorkStealingScheduler(int concurrencyLevel)
        {
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
                _threads.Add(thread);
            }
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (Thread.CurrentThread != _threads[0])
                return false;

            return TryExecuteTask(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks.ToArray();
        }

        public override int MaximumConcurrencyLevel => _threads.Count;

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


