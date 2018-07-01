using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using PromiseCS.AsyncDelegates;
using PromiseCS.Tools;

namespace PromiseCS.Iteration
{
    /// <summary>
    /// Represents a collection of <see cref="Promise{T}"/>s that can be asynchronously iterated over.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="Promise{T}"/>s in the collection.</typeparam>
    public class AsyncEnumerable<T> : IEnumerable<T>
    {
        private Func<AsyncEnumerator<T>> getEnumerator;

        /// <summary>
        /// For use in inherting classes.
        /// </summary>
        protected AsyncEnumerable() { }
        /// <summary>
        /// Creates a new <see cref="AsyncEnumerable{T}"/> from an existing <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="Promises">The <see cref="IEnumerable{T}"/> to use.</param>
        public AsyncEnumerable(IEnumerable<Promise<T>> Promises)
        {
            getEnumerator = () => new AsyncEnumerator<T>(Promises);
        }
        /// <summary>
        /// Creates a new <see cref="AsyncEnumerable{T}"/>, specifying the exact <see cref="AsyncEnumerator{T}"/>
        /// to use.
        /// </summary>
        /// <param name="getEnumerator">A <see cref="Func{T}"/> returns the <see cref="AsyncEnumerator{T}"/>
        /// to use.</param>
        public AsyncEnumerable(Func<AsyncEnumerator<T>> getEnumerator)
        {
            this.getEnumerator = getEnumerator;
        }

        /// <summary>
        /// Asynchronously iterates over each <see cref="Promise{TResult}"/> in the collection. If
        /// any promise has been Rejected, the promise returned will be Rejected.
        /// </summary>
        /// <param name="body">The <see cref="Action{T}"/> to execute on each <see cref="Promise{TResult}"/>'s result.</param>
        /// <returns>A <see cref="Promise"/> that will resolve when done iterating.</returns>
        public virtual Promise ForeachAsync(Action<T> body)
        {
            return new Promise((resolve, reject) =>
            {
                AsyncEnumerator<T> enumerator = GetAsyncEnumerator();
                Promise<bool> mover;

                mover = enumerator.MoveNextAsync();
                mover.Wait();
                do
                {
                    if (mover.IsRejected)
                    {
                        reject(mover.Error);
                        return;
                    }
                    else body(enumerator.Current);
                    mover = enumerator.MoveNextAsync();
                    mover.Wait();
                } while (mover.Result);

                resolve();
                enumerator.Dispose();
            });
        }
        /// <summary>
        /// Asynchronously applies a mapper <see cref="Func{U}"/> to each <see cref="Promise{TResult}"/> in the
        /// collection. If any promise has been Rejected, the promise returned will be Rejected.
        /// </summary>
        /// <typeparam name="U">The returning type of the <see cref="Func{U}"/>.</typeparam>
        /// <param name="mapper">The <see cref="Func{U}"/> to map with.</param>
        /// <returns>The <see cref="Promise{TResult}"/> that will return all mapped values.</returns>
        public virtual Promise<U[]> MapAsync<U>(Func<T, U> mapper)
        {
            return new Promise<U[]>(async (resolve, reject) =>
            {
                List<U> result = new List<U>();
                await ForeachAsync(t => result.Add(mapper(t)))
                    .Then(() => resolve(result.ToArray()))
                    .Catch(e => reject(e));
            });
        }
        /// <summary>
        /// Asynchronously turns this collection into a <typeparamref name="T"/> array. If any promise
        /// has been Rejected, the promise returned will be Rejected.
        /// </summary> 
        /// <returns>A <see cref="Promise{TResult}"/> that will return all the values of the promises
        /// in this collection.</returns>
        public virtual Promise<T[]> ToArray() => MapAsync(t => t);

        /// <summary>
        /// Gets the <see cref="AsyncEnumerator{T}"/> for this collection.
        /// </summary>
        /// <returns>The <see cref="AsyncEnumerator{T}"/> for this collection.</returns>
        public virtual AsyncEnumerator<T> GetAsyncEnumerator()
        {
            return getEnumerator();
        }
        /// <summary>
        /// Gets the <see cref="IEnumerator{T}"/> for this collection.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() => GetAsyncEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    /// <summary>
    /// Represents the base class for asynchronous enumeration. Enumerates through <see cref="Promise{TResult}"/>s in order.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="Promise{TResult}"/>s.</typeparam>
    public class AsyncEnumerator<T> : IEnumerator<T>
    {
        /// <summary>
        /// Gets the Result of the current <see cref="Promise{TResult}"/> the enumerator is on.
        /// </summary>
        public virtual T Current => completedPromises[currentIndex].Result;
        object IEnumerator.Current => Current;

        /// <summary>
        /// All promises that have been iterated over.
        /// </summary>
        protected List<Promise<T>> completedPromises;
        /// <summary>
        /// The current index to <see cref="completedPromises"/>.
        /// </summary>
        protected int currentIndex;

        private IEnumerator<Promise<T>> Promises { get; set; }

        /// <summary>
        /// For use in inheriting classes. Initializes <see cref="completedPromises"/> and <see cref="currentIndex"/>.
        /// </summary>
        protected AsyncEnumerator()
        {
            completedPromises = new List<Promise<T>>();
            currentIndex = -1;
        }
        /// <summary>
        /// Creates a new <see cref="AsyncEnumerator{T}"/> from an existing collection of <see cref="Promise{TResult}"/>s.
        /// </summary>
        /// <param name="Promises">The collection to use.</param>
        public AsyncEnumerator(IEnumerable<Promise<T>> Promises)
            : this()
        {
            this.Promises = Promises.GetEnumerator();
        }

        /// <summary>
        /// Gets a new, completed <see cref="Promise{TResult}"/> for the enumerator to handle.
        /// </summary>
        /// <returns>The completed promise.</returns>
        protected virtual Promise<T> GetNewPromise()
        {
            if (!Promises.MoveNext()) return null;
            Promises.Current.Wait();
            return Promises.Current;
        }
        /// <summary>
        /// Releases all unmanaged resources of this async enumerator.
        /// </summary>
        public virtual void Dispose()
        {
            Promises?.Dispose();
        }
        /// <summary>
        /// Moves to the next <see cref="Promise{TResult}"/>.
        /// </summary>
        /// <returns>If the enumerator has successfully moved to the next element.</returns>
        public bool MoveNext()
        {
            currentIndex++;
            if (currentIndex >= completedPromises.Count)
            {
                Promise<T> newPromise = GetNewPromise();
                if (newPromise == null) return false;
                else
                {
                    if (newPromise.IsRejected) throw newPromise.Error;
                    else completedPromises.Add(newPromise);
                }
            }
            return true;
        }
        /// <summary>
        /// Asynchronously moves to the next <see cref="Promise{TResult}"/>.
        /// </summary>
        /// <returns>A promise that will return the result of <see cref="MoveNext"/>.</returns>
        public Promise<bool> MoveNextAsync()
        {
            return new Promise<bool>((resolve, reject) =>
            {
                try { resolve(MoveNext()); }
                catch (Exception e) { reject(e); }
            });
        }
        /// <summary>
        /// Resets this asynchronous enumerator.
        /// </summary>
        public virtual void Reset()
        {
            currentIndex = -1;
        }
    }
    /// <summary>
    /// Represents an asynchronous enumerator that enumerates in order of Completion.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="Promise{TResult}"/>s.</typeparam>
    public class AsyncRaceEnumerator<T> : AsyncEnumerator<T>
    {
        private List<Promise<T>> allPromises;
        /// <summary>
        /// Creates a new <see cref="AsyncRaceEnumerator{T}"/> from an existing collection of <see cref="Promise{TResult}"/>s.
        /// </summary>
        /// <param name="Promises">The collection to use.</param>
        public AsyncRaceEnumerator(IEnumerable<Promise<T>> Promises)
        {
            allPromises = new List<Promise<T>>(Promises);
        }

        /// <summary>
        /// Returns a new, completed <see cref="Promise{TResult}"/> using <see cref="PromiseExtension.Race{T}(IEnumerable{Promise{T}})"/>.
        /// </summary>
        /// <returns></returns>
        protected override Promise<T> GetNewPromise()
        {
            if (allPromises.Count > 0)
            {
                Promise<T> result = allPromises.Race();
                allPromises.Remove(result);
                return result;
            }
            else return null;
        }
    }
    /// <summary>
    /// Represents an asynchronous enumerator that executes a function for every new value.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="Promise{TResult}"/>s.</typeparam>
    public class AsyncStreamEnumerator<T> : AsyncEnumerator<T>
    {
        /// <summary>
        /// The function to execute in <see cref="GetNewPromise"/>.
        /// </summary>
        protected Func<Promise<T>> next;

        /// <summary>
        /// Creates a new <see cref="AsyncStreamEnumerator{T}"/> using a <see cref="Func{TResult}"/>.
        /// Return <see langword="null"/> when done iterating.
        /// </summary>
        /// <param name="next">The function to use.</param>
        public AsyncStreamEnumerator(Func<Promise<T>> next)
        {
            this.next = next;
        }
        /// <summary>
        /// Creates a new <see cref="AsyncStreamEnumerator{T}"/> using a <see cref="AsyncFunc{TResult}"/>.
        /// Return <see langword="null"/> when done iterating.
        /// </summary>
        /// <param name="next">The function to use.</param>
        public AsyncStreamEnumerator(AsyncFunc<Promise<T>> next)
        {
            this.next = () =>
            {
                Task<Promise<T>> nxt = next();
                nxt.Wait();
                return nxt.Result;
            };
        }

        /// <summary>
        /// Executes <see cref="next"/>, then waits for the <see cref="Promise{TResult}"/> returned
        /// to complete.
        /// </summary>
        /// <returns></returns>
        protected override Promise<T> GetNewPromise()
        {
            Promise<T> next = this.next();
            next?.Wait();
            return next;
        }
        /// <summary>
        /// Resets this <see cref="AsyncStreamEnumerator{T}"/>.
        /// </summary>
        public override void Reset()
        {
            completedPromises.Clear();
            base.Reset();
        }
    }
    /// <summary>
    /// Implements a yield-like asynchronous enumerator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncGenerator<T> : AsyncEnumerator<T>
    {
        /// <summary>
        /// The function to yield a new promise.
        /// </summary>
        protected Generator<Promise<T>> Generator { get; set; }
        private IEnumerator<Promise<T>> generated;

        /// <summary>
        /// Creates a new <see cref="AsyncGenerator{T}"/> from a <see cref="Generator{T}"/>.
        /// </summary>
        /// <param name="Generator">The <see cref="Generator{T}"/> to use.</param>
        public AsyncGenerator(Generator<Promise<T>> Generator)
        {
            this.Generator = Generator;
            generated = null;
        }

        private IEnumerator<Promise<T>> Generate()
        {
            Queue<Promise<T>> yieldQueue = new Queue<Promise<T>>();
            bool finished = false;
            new Promise((resolve, reject) =>
            {
                Generator(p =>
                {
                    if (finished)
                    {
                        reject(new InvalidOperationException("Generator already finished"));
                        return;
                    }
                    yieldQueue.Enqueue(p);
                },
                () =>
                {
                    if (finished)
                    {
                        reject(new InvalidOperationException("Generator already finished"));
                        return;
                    }
                    finished = true;
                });
                resolve();
            })
            .Then(() => finished = true, e => throw e);

            while (!finished || yieldQueue.Count > 0)
            {
                if (yieldQueue.TryDequeue(out Promise<T> toYield))
                    yield return toYield;
            }
        }
        /// <summary>
        /// If needed, generate a new set of promises, then wait for the next promise to complete,
        /// and return it.
        /// </summary>
        /// <returns>The next <see cref="Promise{T}"/></returns>
        protected override Promise<T> GetNewPromise()
        {
            if (generated == null) generated = Generate();
            if (!generated.MoveNext())
            {
                generated = null;
                return null;
            }
            generated.Current.Wait();
            return generated.Current;
        }
        /// <summary>
        /// Resets this <see cref="AsyncGenerator{T}"/>
        /// </summary>
        public override void Reset()
        {
            completedPromises.Clear();
            base.Reset();
        }
    }
    /// <summary>
    /// Provides a function for a generator. The first parameter <see langword="yield"/>s a value, 
    /// the second finishes iteration early, but finishing the function does the same thing.
    /// </summary>
    /// <typeparam name="T">The type to <see langword="yield"/>.</typeparam>
    /// <param name="yield">The <see langword="yield"/> action.</param>
    /// <param name="finish">The completion action.</param>
    public delegate void Generator<T>(Action<T> yield, Action finish);
}
