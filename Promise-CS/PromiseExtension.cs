using System;
using System.Collections.Generic;
using System.Threading;

namespace PromiseCS
{
    /// <summary>
    /// Provides extension methods to various <see cref="Promise"/>-related objects.
    /// </summary>
    public static class PromiseExtension
    {
        /// <summary>
        /// Executes <see cref="Promise.Wait"/> on all <see cref="Promise"/>s in a collection.
        /// </summary>
        /// <param name="promises">The promise collection.</param>
        public static void WaitAll(this IEnumerable<Promise> promises)
        {
            foreach (Promise p in promises) p.Wait();
        }
        /// <summary>
        /// Takes a collection of promises and turns them into a single <see cref="Promise{T}"/> that
        /// will return an array of all the return values of the promises in the collection, in order.
        /// </summary>
        /// <typeparam name="T">The type of all the promises in the collection.</typeparam>
        /// <param name="promises">The promise collection.</param>
        /// <returns>a single <see cref="Promise{T}"/> that will return an array of all the return values
        /// of the promises in the collection, in order.</returns>
        public static Promise<T[]> All<T>(this IEnumerable<Promise<T>> promises)
        {
            return new Promise<T[]>((resolve, reject) =>
            {
                List<Promise<T>> proms = new List<Promise<T>>();
                foreach (var promise in promises)
                {
                    proms.Add(promise);
                    promise.Catch(e => reject(e));
                }
                proms.WaitAll();

                List<T> values = new List<T>();
                foreach (var promise in proms) values.Add(promise.Result);
                resolve(values.ToArray());
            });
        }
        /// <summary>
        /// Returns a new <see cref="Promise"/> that will resolve or reject as soon as one of the promises
        /// in a promise collection resolves or rejects, with the reason from that promise if it was
        /// rejected.
        /// </summary>
        /// <param name="promises">The promise collection.</param>
        /// <returns>The new <see cref="Promise"/>.</returns>
        public static Promise Race(this IEnumerable<Promise> promises)
        {
            return new Promise((resolve, reject) =>
            {
                foreach (Promise p in promises)
                    p.Then(() => resolve(), e => reject(e));
            });
        }
        /// <summary>
        /// Returns a new <see cref="Promise{T}"/> that will resolve or reject as soon as one of the promises
        /// in a promise collection resolves or rejects, with the value or reason from that promise.
        /// </summary>
        /// <typeparam name="T">The type of all the promises in the collection.</typeparam>
        /// <param name="promises">The promise collection.</param>
        /// <returns>The new <see cref="Promise{T}"/>.</returns>
        public static Promise<T> Race<T>(this IEnumerable<Promise<T>> promises)
        {
            return new Promise<T>((resolve, reject) =>
            {
                foreach (Promise<T> p in promises)
                    p.Then(t => resolve(t), e => reject(e));
            });
        }
        /// <summary>
        /// Returns a new <see cref="Promise"/> that will Fulfill if a given promise has Fulfilled
        /// in a given amount of milliseconds, or Reject if it has not.
        /// </summary>
        /// <param name="p">The <see cref="Promise"/> to timeout.</param>
        /// <param name="ms">The amount of milliseconds to timeout.</param>
        /// <returns>The new <see cref="Promise"/>.</returns>
        public static Promise Timeout(this Promise p, int ms)
        {
            return new Promise((resolve, reject) =>
            {
                bool? done = null;
                new Promise((r, j) =>
                {
                    Thread.Sleep(ms);
                    if (!p.IsCompleted)
                    {
                        reject(new TimeoutException($"Promise did not complete in {ms} ms."));
                        done = false;
                    }
                    r();
                });
                new Promise((r, j) =>
                {
                    p.Wait();
                    done = true;
                    r();
                });

                while (!done.HasValue) ;
                if (done.Value)
                {
                    if (p.IsFulfilled) resolve();
                    else reject(p.Error);
                }
            });
        }
        /// <summary>
        /// Returns a new <see cref="Promise{T}"/> that will Fulfill if a given promise has Fulfilled
        /// in a given amount of milliseconds, or Reject if it has not.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="Promise{T}"/></typeparam>
        /// <param name="p">The <see cref="Promise{T}"/> to timeout.</param>
        /// <param name="ms">The amount of milliseconds to timeout.</param>
        /// <returns>The new <see cref="Promise{T}"/>.</returns>
        public static Promise<T> Timeout<T>(this Promise<T> p, int ms)
        {
            return new Promise<T>((resolve, reject) =>
            {
                bool? done = null;
                new Promise((r, j) =>
                {
                    Thread.Sleep(ms);
                    if (!p.IsCompleted)
                    {
                        reject(new TimeoutException($"Promise did not complete in {ms} ms."));
                        done = false;
                    }
                    r();
                });
                new Promise((r, j) =>
                {
                    p.Wait();
                    done = true;
                    r();
                });

                while (!done.HasValue) ;
                if (done.Value)
                {
                    if (p.IsFulfilled) resolve(p.Result);
                    else reject(p.Error);
                }
            });
        }
    }
}
