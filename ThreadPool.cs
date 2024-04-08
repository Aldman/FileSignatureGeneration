using System.Collections.Concurrent;
using System.Diagnostics;

namespace FileSignatureGeneration;

internal class ThreadPool
{
    private const int TimeoutInSec = 7;
    
    private readonly ConcurrentQueue<Action> _queue = new();
    private readonly Stopwatch _sw = new();
    
    public ThreadPool(int threadsCount)
    {
        _sw.Start();
        
        for (var i = 0; i < threadsCount; i++)
        {
            var thread = new Thread(InvokeQueueAction);
            thread.Start();
        }
    }
    
    internal bool IsIdle => _queue.IsEmpty && TimeoutElapsed;

    private bool TimeoutElapsed => _sw.Elapsed.Seconds >= TimeoutInSec;

    internal void Run(Action action)
    {
        _queue.Enqueue(action);
    }

    private void InvokeQueueAction()
    {
        while (true)
        {
            if (TimeoutElapsed) return;
            
            if (_queue.IsEmpty)
            {
                Thread.Sleep(100);
                continue;
            }

            if (!_queue.TryDequeue(out var action)) continue;

            lock (action)
            {
                action.Invoke();
                _sw.Reset();
            }
        }
    }
}