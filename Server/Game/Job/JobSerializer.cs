using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game.Job
{
    public class JobSerializer
    {
        JobTimer _timer = new JobTimer();
        Queue<IJob> _jobQueue = new Queue<IJob>();
        object _lock = new object();

        public void Push(Action action) { Push(new Job(action)); }
        public void Push<T1>(Action<T1> action, T1 t1) { Push(new Job<T1>(action, t1)); }
        public void Push<T1, T2>(Action<T1, T2> action, T1 t1, T2 t2) { Push(new Job<T1, T2>(action, t1, t2)); }
        public void Push<T1, T2, T3>(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3) { Push(new Job<T1, T2, T3>(action, t1, t2, t3)); }


        public void PushAfter(int tickAfter, Action action)
        {
            PushAfter(tickAfter, action);
        }
        public void PushAfter<T1>(int tickAfter, Action<T1> action, T1 t1)
        {
            PushAfter(tickAfter, action, t1);
        }

        public void PushAfter<T1, T2>(int tickAfter, Action<T1, T2> action, T1 t1, T2 t2)
        {
            PushAfter(tickAfter, action, t1, t2);
        }

        public void PushAfter<T1, T2, T3>(int tickAfter, Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3)
        {
            PushAfter(tickAfter, action, t1, t2, t3);
        }

        public void PushAfter(int tickAfter, IJob job)
        {
            _timer.Push(job, tickAfter);
        }

        public void Push(IJob job)
        {
            lock (_lock)
            {
                _jobQueue.Enqueue(job);
            }
        }

        public void Flush()
        {
            _timer.Flush();
            while (true)
            {
                IJob job = PopOrNull();
                if (job == null)
                    return;

                job.Excute();
            }
        }

        IJob PopOrNull()
        {
            lock (_lock)
            {
                if (_jobQueue.Count == 0)
                {
                    return null;
                }
                return _jobQueue.Dequeue();
            }
        }
    }
}
