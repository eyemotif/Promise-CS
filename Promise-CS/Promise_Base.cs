using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using PromiseCS.AsyncDelegates;

namespace PromiseCS
{
    /// <summary>
    /// Represents an asynchronous task.
    /// </summary>
    public class Promise : IDisposable
    {
        /// <summary>
        /// The <see cref="Task"/> that holds the work for this <see cref="Promise"/>.
        /// </summary>
        protected virtual Task ThisTask { get; set; }
        /// <summary>
        /// The <see cref="Exception"/> that holds error data for this <see cref="Promise"/>, if it
        /// has been rejected.
        /// </summary>
        protected virtual Exception ThisError { get; set; }
        /// <summary>
        /// The <see cref="Exception"/> that was used to reject this <see cref="Promise"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if this <see cref="Promise"/> has not
        /// been rejected.</exception>
        public Exception Error
        {
            get
            {
                if (!IsRejected) throw new InvalidOperationException("Promise has not been rejected.");
                else return ThisError;
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="Promise"/> has either been Fulfilled 
        /// or Rejected.
        /// </summary>
        public bool IsCompleted => IsFulfilled || IsRejected;
        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="Promise"/> has been Fulfilled.
        /// </summary>
        public virtual bool IsFulfilled { get; protected set; }
        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="Promise"/> has been Rejected.
        /// </summary>
        public virtual bool IsRejected { get; protected set; }

        /// <summary>
        /// For use in intherting classes.
        /// </summary>
        protected Promise() { /*Empty*/ }
        /// <summary>
        /// Creates a new <see cref="Promise"/> that executes an <see cref="Action"/> asynchronously.
        /// </summary>
        /// <param name="executor">The <see cref="Action"/> to execute asynchronously.</param>
        public Promise(Action<Action, Action<Exception>> executor)
        {
            ThisTask = Task.Run(() =>
            {
                executor(() =>
                {
                    if (IsCompleted) throw new InvalidOperationException("Promise has already been completed");
                    IsFulfilled = true;
                }, e =>
                {
                    if (IsCompleted) throw new InvalidOperationException("Promise has already been completed");
                    IsRejected = true;
                    ThisError = e;
                });
            });
        }
        /// <summary>
        /// Creates a new <see cref="Promise"/> that executes an <see cref="AsyncAction"/>.
        /// </summary>
        /// <param name="executor">The <see cref="AsyncAction"/> to execute.</param>
        public Promise(AsyncAction<Action, Action<Exception>> executor)
            : this((resolve, reject) => executor(resolve, reject).Wait()) { /*Empty*/ }
        /// <summary>
        /// Creates a new <see cref="Promise"/> from an existing <see cref="Task"/>.
        /// </summary>
        /// <param name="waitingTask">The <see cref="Task"/> to use.</param>
        public Promise(Task waitingTask)
            : this((resolve, reject) =>
        {
            try
            {
                waitingTask.Wait();
                resolve();
            }
            catch (Exception e)
            {
            reject(e);
            }
        }) { /*Empty*/ }
        /// <summary>
        /// Implicitly converts a <see cref="Promise"/> to a <see cref="Task"/>.
        /// </summary>
        /// <param name="prom">The <see cref="Promise"/> to convert.</param>
        public static implicit operator Task(Promise prom) => prom.ThisTask;

        /// <summary>
        /// Returns a new <see cref="Promise"/> that will wait for this promise to Fulfill, then execute 
        /// an <see cref="Action"/>.
        /// </summary>
        /// <param name="onFulfilled">The <see cref="Action"/> to execute.</param>
        /// <returns>The new <see cref="Promise"/>.</returns>
        public Promise Then(Action onFulfilled)
        {
            return new Promise((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled)
                {
                    onFulfilled();
                    resolve();
                }
                else reject(ThisError);
                Dispose();
            });
        }
        /// <summary>
        /// Returns a new <see cref="Promise"/> that will wait for this promise to either Fulfill or Reject,
        /// then executes one of two <see cref="Action"/>s accordingly.
        /// </summary>
        /// <param name="onFulfilled">The <see cref="Action"/> when this <see cref="Promise"/> is Fulfilled.</param>
        /// <param name="onRejected">The <see cref="Action"/> when this <see cref="Promise"/> is Rejected.</param>
        /// <returns>The new <see cref="Promise"/>.</returns>
        public Promise Then(Action onFulfilled, Action<Exception> onRejected)
        {
            return new Promise((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled)
                {
                    onFulfilled();
                    resolve();
                }
                else
                {
                    onRejected(ThisError);
                    reject(ThisError);
                }
                Dispose();
            });
        }
        /// <summary>
        /// Returns a new <see cref="Promise"/> that, if this promise is Rejected, will execute 
        /// an <see cref="Action"/>.
        /// </summary>
        /// <param name="onRejected">The <see cref="Action"/> to execute.</param>
        /// <returns>The new <see cref="Promise"/>.</returns>
        public Promise Catch(Action<Exception> onRejected)
        {
            return new Promise((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled) resolve();
                else
                {
                    onRejected(ThisError);
                    reject(ThisError);
                }
                Dispose();
            });
        }
        /// <summary>
        /// Returns a new <see cref="Promise"/> that will wait for this promise to Complete, then executes
        /// an <see cref="Action"/>.
        /// </summary>
        /// <param name="onFinally">The <see cref="Action"/>.</param>
        /// <returns>The new <see cref="Promise"/>.</returns>
        public Promise Finally(Action onFinally)
        {
            return new Promise((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled) resolve();
                else reject(ThisError);
                onFinally();
                Dispose();
            });
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/> that will wait for this promise to Fulfill, then waits 
        /// for a promise returned by a given <see cref="Func{TResult}"/>.
        /// </summary>
        /// <param name="onFulfilled">The <see cref="Func{TResult}"/> to execute when Fulfilled.</param>
        /// <returns>A new <see cref="Promise"/>.</returns>
        public Promise Then(Func<Promise> onFulfilled)
        {
            return new Promise((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled)
                {
                    Promise newPromise = onFulfilled();
                    newPromise.Wait();

                    if (newPromise.IsFulfilled) resolve();
                    else reject(newPromise.Error);
                }
                else reject(Error);
            });
        }
        /// <summary>
        /// Returns a new <see cref="Promise{TResult}"/> that will wait for this promise to Fulfill, 
        /// then waits for a promise returned by a given <see cref="Func{TResult}"/>.
        /// </summary>
        /// <param name="onFulfilled">The <see cref="Func{TResult}"/> to execute when Fulfilled.</param>
        /// <returns>A new <see cref="Promise{TResult}"/>.</returns>
        public Promise<T> Then<T>(Func<Promise<T>> onFulfilled)
        {
            return new Promise<T>((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled)
                {
                    Promise<T> newPromise = onFulfilled();
                    newPromise.Wait();

                    if (newPromise.IsFulfilled) resolve(newPromise.Result);
                    else reject(newPromise.Error);
                }
                else reject(Error);
            });
        }
        /// <summary>
        /// Returns a new <see cref="Promise"/> that will wait for this promise to Fulfill or Reject, 
        /// then if it Fulfills, it waits for a promise returned by a given <see cref="Func{TResult}"/>,
        /// otherwise it executes the other given <see cref="Action{T}"/>.
        /// </summary>
        /// <param name="onFulfilled">The <see cref="Func{TResult}"/> to execute when Fulfilled.</param>
        /// <param name="onRejected">The <see cref="Action{T}"/> to execute when Rejected.</param>
        /// <returns>The new <see cref="Promise"/>.</returns>
        public Promise Then(Func<Promise> onFulfilled, Action<Exception> onRejected)
        {
            return new Promise((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled)
                {
                    Promise newPromise = onFulfilled();
                    newPromise.Wait();

                    if (newPromise.IsFulfilled) resolve();
                    else reject(newPromise.Error);
                }
                else
                {
                    onRejected(Error);
                    reject(Error);
                }
            });
        }
        /// <summary>
        /// Returns a new <see cref="Promise{TResult}"/> that will wait for this promise to Fulfill or Reject, 
        /// then if it Fulfills, it waits for a promise returned by a given <see cref="Func{TResult}"/>,
        /// otherwise it executes the other given <see cref="Action{T}"/>.
        /// </summary>
        /// <param name="onFulfilled">The <see cref="Func{TResult}"/> to execute when Fulfilled.</param>
        /// <param name="onRejected">The <see cref="Action{T}"/> to execute when Rejected.</param>
        /// <returns>The new <see cref="Promise{TResult}"/>.</returns>
        public Promise<T> Then<T>(Func<Promise<T>> onFulfilled, Action<Exception> onRejected)
        {
            return new Promise<T>((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled)
                {
                    Promise<T> newPromise = onFulfilled();
                    newPromise.Wait();

                    if (newPromise.IsFulfilled) resolve(newPromise.Result);
                    else reject(newPromise.Error);
                }
                else
                {
                    onRejected(Error);
                    reject(Error);
                }
            });
        }

        /// <summary>
        /// Gets the <see cref="TaskAwaiter"/> for this <see cref="Promise"/>.
        /// </summary>
        /// <returns>The <see cref="TaskAwaiter"/> for this <see cref="Promise"/>.</returns>
        public TaskAwaiter GetAwaiter() => Task.Run(() =>
        {
            ThisTask.Wait();
            if (!IsFulfilled) throw Error;
        }).GetAwaiter();
        /// <summary>
        /// Synchronously waits for this <see cref="Promise"/> to end.
        /// </summary>
        public virtual void Wait() => ThisTask.Wait();
        /// <summary>
        /// Disposes all unmanaged resources held by this <see cref="Promise"/>.
        /// </summary>
        public virtual void Dispose()
        {
            ThisTask.Dispose();
        }
        /// <summary>
        /// Converts this <see cref="Promise"/> into a human readable string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Promise{{{(IsCompleted ? $"{(IsFulfilled ? "Fulfilled" : $"Rejected: {Error}")}" : "Pending...")}}}";
        }
    }
}
