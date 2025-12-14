using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace RamaFemenina.Extensions
{
    public static class DispatcherQueueExtensions
    {
        /// <summary>
        /// Encola una acción en el DispatcherQueue y espera a que se complete.
        /// </summary>
        public static Task EnqueueAsync(this DispatcherQueue dispatcher, Action action)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (!dispatcher.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }))
            {
                tcs.SetException(new InvalidOperationException("Failed to enqueue action to DispatcherQueue"));
            }

            return tcs.Task;
        }

        /// <summary>
        /// Encola una acción asíncrona en el DispatcherQueue y espera a que se complete.
        /// </summary>
        public static Task EnqueueAsync(this DispatcherQueue dispatcher, Func<Task> asyncAction)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (!dispatcher.TryEnqueue(async () =>
            {
                try
                {
                    await asyncAction();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }))
            {
                tcs.SetException(new InvalidOperationException("Failed to enqueue async action to DispatcherQueue"));
            }

            return tcs.Task;
        }

        /// <summary>
        /// Encola una función en el DispatcherQueue y espera a que se complete, retornando el resultado.
        /// </summary>
        public static Task<T> EnqueueAsync<T>(this DispatcherQueue dispatcher, Func<T> function)
        {
            var tcs = new TaskCompletionSource<T>();

            if (!dispatcher.TryEnqueue(() =>
            {
                try
                {
                    var result = function();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }))
            {
                tcs.SetException(new InvalidOperationException("Failed to enqueue function to DispatcherQueue"));
            }

            return tcs.Task;
        }

        /// <summary>
        /// Encola una función asíncrona en el DispatcherQueue y espera a que se complete, retornando el resultado.
        /// </summary>
        public static Task<T> EnqueueAsync<T>(this DispatcherQueue dispatcher, Func<Task<T>> asyncFunction)
        {
            var tcs = new TaskCompletionSource<T>();

            if (!dispatcher.TryEnqueue(async () =>
            {
                try
                {
                    var result = await asyncFunction();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }))
            {
                tcs.SetException(new InvalidOperationException("Failed to enqueue async function to DispatcherQueue"));
            }

            return tcs.Task;
        }
    }
}
