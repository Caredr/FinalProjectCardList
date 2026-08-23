using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.BackgroundTasks
{
    public class BackgroundTaskRunner : IDisposable
    {
        private readonly ConcurrentBag<IBackgroundTask> _tasks = new();
        private Task? _runningTasks;
        private CancellationTokenSource? _stoppingCts;

        public void AddTask(IBackgroundTask task)
        {
            if (_runningTasks is not null)
                throw new InvalidOperationException("Tasks are already running");

            _tasks.Add(task);
        }

        public void StartTasks(CancellationToken ct)
        {
            if (_runningTasks is not null)
                throw new InvalidOperationException("Tasks are already running");

            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            static async Task RunSafe(IBackgroundTask task, CancellationToken ct)
            {
                try
                {
                    await task.Start(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error in {task.GetType().Name}: {ex}");
                }
            }

            _runningTasks = Task.WhenAll(_tasks.Select(t => RunSafe(t, _stoppingCts.Token)));
        }

        public async Task StopTasks(CancellationToken ct)
        {
            if (_runningTasks is null)
                return;

            try
            {
                _stoppingCts?.Cancel();
            }
            finally
            {
                await _runningTasks.WaitAsync(ct).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        }
        public void Dispose()
        {
            _stoppingCts?.Cancel();
            _stoppingCts?.Dispose();
        }
    }
}
