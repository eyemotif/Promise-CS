using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using PromiseCS.AsyncDelegates;

namespace PromiseCS
{
    /// <summary>
    /// Represents an asynchronous task that returns a value.
    /// </summary>
    public class Promise<TResult> : Promise
    {
        /// <summary>
        /// The <typeparamref name="TResult"/> that holds success data for this <see cref="Promise"/>, 
        /// if it has been fulfilled.
        /// </summary>
        protected virtual TResult ThisResult { get; set; }
        /// <summary>
        /// The <typeparamref name="TResult"/> that was used to Fulfill this <see cref="Promise{TResult}"/>.
        /// </summary>
        public TResult Result
        {
            get
            {
                if (!IsCompleted) throw new InvalidOperationException();
                else if (!IsFulfilled) throw new InvalidOperationException();
                else return ThisResult;
            }
        }

        /// <summary>
        /// Creates a new <see cref="Promise{TResult}"/> that executes an <see cref="Action"/> asynchronously.
        /// </summary>
        /// <param name="executor">The <see cref="Action{TResult}"/> to execute asynchronously.</param>
        public Promise(Action<Action<TResult>, Action<Exception>> executor)
        {
            ThisTask = Task.Run(() =>
            {
                executor(t =>
                {
                    if (IsCompleted) throw new InvalidOperationException("Promise has already been completed");
                    ThisResult = t;
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
        /// Creates a new <see cref="Promise{TResult}"/> that executes an <see cref="AsyncAction"/>.
        /// </summary>
        /// <param name="executor">The <see cref="AsyncAction{TResult}"/> to execute.</param>
        public Promise(AsyncAction<Action<TResult>, Action<Exception>> executor)
            : this((resolve, reject) => executor(resolve, reject).Wait()) { /*Empty*/ }
        /// <summary>
        /// Creates a new <see cref="Promise{TResult}"/> from an existing <see cref="Task{TResult}"/>.
        /// </summary>
        /// <param name="waitingTask">The <see cref="Task{TResult}"/> to use.</param>
        public Promise(Task<TResult> waitingTask)
            : this((resolve, reject) =>
        {
            try
            {
                waitingTask.Wait();
                resolve(waitingTask.Result);
            }
            catch (Exception e)
            {
                reject(e);
            }
        }) { /*Empty*/ }
        /// <summary>
        /// Implicitly converts a <see cref="Promise{TResult}"/> to a <see cref="Task{TResult}"/>.
        /// </summary>
        /// <param name="prom">The <see cref="Promise{TResult}"/> to convert.</param>
        public static implicit operator Task<TResult>(Promise<TResult> prom) => prom.GetTypedTask();

        /// <summary>
        /// Returns a new <see cref="Promise"/> that waits for this promise to Fulfill, then execute 
        /// an <see cref="Action{TResult}"/>.
        /// </summary>
        /// <param name="onFulfilled">The <see cref="Action{TResult}"/> to execute.</param>
        /// <returns>The new <see cref="Promise"/>.</returns>
        public Promise Then(Action<TResult> onFulfilled)
        {
            return new Promise<TResult>((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled)
                {
                    onFulfilled(ThisResult);
                    resolve(ThisResult);
                }
                else reject(ThisError);
                Dispose();
            });
        }
        /// <summary>
        /// Returns a new <see cref="Promise"/> that waits for this promise to either Fulfill or Reject,
        /// then executes one of two <see cref="Action"/>s accordingly.
        /// </summary>
        /// <param name="onFulfilled">The <see cref="Action"/> when this <see cref="Promise{TResult}"/>
        /// is Fulfilled.</param>
        /// <param name="onRejected">The <see cref="Action"/> when this <see cref="Promise{TResult}"/> 
        /// is Rejected.</param>
        /// <returns>The new <see cref="Promise"/>.</returns>
        public Promise Then(Action<TResult> onFulfilled, Action<Exception> onRejected)
        {
            return new Promise((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled)
                {
                    onFulfilled(ThisResult);
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
        /// Returns a new <see cref="Promise{U}"/> that waits for this promise to Fulfill, then execute
        /// a <see cref="Func{U}"/>, returning the value of the function.
        /// </summary>
        /// <typeparam name="U">The return type of the <see cref="Func{U}"/></typeparam>
        /// <param name="onFulfilled">The <see cref="Func{TResult}"/> to execute.</param>
        /// <returns>The new <see cref="Promise{U}"/>.</returns>
        public Promise<U> Then<U>(Func<TResult, U> onFulfilled)
        {
            return new Promise<U>((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled)
                {
                    U newRes = onFulfilled(ThisResult);
                    resolve(newRes);
                }
                else reject(ThisError);
                Dispose();
            });
        }
        /// <summary>
        /// Returns a new <see cref="Promise{U}"/> that waits for this promise to either Fulfill or
        /// Reject, then executes one of two functions accordingly.
        /// </summary>
        /// <typeparam name="U">The return type of the <see cref="Func{U}"/></typeparam>
        /// <param name="onFulfilled">The <see cref="Func{U}"/> when this <see cref="Promise{TResult}"/> 
        /// is Fulfilled, and that will give the return result of the new <see cref="Promise{U}"/></param>
        /// <param name="onRejected">The <see cref="Action"/> when this <see cref="Promise{TResult}"/> 
        /// is Rejected.</param>
        /// <returns>The new <see cref="Promise{U}"/>.</returns>
        public Promise<U> Then<U>(Func<TResult, U> onFulfilled, Action<Exception> onRejected)
        {
            return new Promise<U>((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled)
                {
                    U newRes = onFulfilled(ThisResult);
                    resolve(newRes);
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
        /// Returns a new <see cref="Promise{U}"/> that waits for this promise to Fulfill, then 
        /// execute an <see cref="AsyncFunc{U}"/>, returning the value of the function.
        /// </summary>
        /// <typeparam name="U">The return type of the <see cref="AsyncFunc{U}"/></typeparam>
        /// <param name="onFulfilled">The <see cref="AsyncFunc{TResult}"/> to execute.</param>
        /// <returns>The new <see cref="Promise{U}"/>.</returns>
        public Promise<U> ThenAsync<U>(AsyncFunc<TResult, U> onFulfilled)
        {
            return new Promise<U>(async (resolve, reject) =>
            {
                Wait();
                if (IsFulfilled)
                {
                    U newRes = await onFulfilled(ThisResult);
                    resolve(newRes);
                }
                else reject(ThisError);
                Dispose();
            });
        }
        /// <summary>
        /// Returns a new <see cref="Promise{TResult}"/> that, if this promise is Rejected, executes
        /// an <see cref="Action"/>.
        /// </summary>
        /// <param name="onRejected">The <see cref="Action"/> to execute.</param>
        /// <returns>The new <see cref="Promise{TResult}"/>.</returns>
        public new Promise<TResult> Catch(Action<Exception> onRejected)
        {
            return new Promise<TResult>((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled) resolve(ThisResult);
                else
                {
                    onRejected(ThisError);
                    reject(ThisError);
                }
                Dispose();
            });
        }
        /// <summary>
        /// Returns a new <see cref="Promise{TResult}"/> that waits for this promise to Complete,
        /// then executes an <see cref="Action"/>.
        /// </summary>
        /// <param name="onFinally">The <see cref="Action"/>.</param>
        /// <returns>The new <see cref="Promise{TResult}"/>.</returns>
        public new Promise<TResult> Finally(Action onFinally)
        {
            return new Promise<TResult>((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled) resolve(ThisResult);
                else reject(ThisError);
                onFinally();
                Dispose();
            });
        }

        public new Promise Then(Func<TResult, Promise> onFulfilled)
        {
            return new Promise((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled)
                {
                    Promise newPromise = onFulfilled(Result);
                    newPromise.Wait();

                    if (newPromise.IsFulfilled) resolve();
                    else reject(newPromise.Error);
                }
                else reject(Error);
            });
        }
        public new Promise<T> Then<T>(Func<TResult, Promise<T>> onFulfilled)
        {
            return new Promise<T>((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled)
                {
                    Promise<T> newPromise = onFulfilled(Result);
                    newPromise.Wait();

                    if (newPromise.IsFulfilled) resolve(newPromise.Result);
                    else reject(newPromise.Error);
                }
                else reject(Error);
            });
        }
        public new Promise Then(Func<TResult, Promise> onFulfilled, Action<Exception> onRejected)
        {
            return new Promise((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled)
                {
                    Promise newPromise = onFulfilled(Result);
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
        public new Promise<T> Then<T>(Func<TResult, Promise<T>> onFulfilled, Action<Exception> onRejected)
        {
            return new Promise<T>((resolve, reject) =>
            {
                Wait();
                if (IsFulfilled)
                {
                    Promise<T> newPromise = onFulfilled(Result);
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
        /// Gets a new <see cref="Task{TResult}"/> that will return a <typeparamref name="TResult"/> 
        /// if this <see cref="Promise{TResult}"/> is Fulfilled, or throw an <see cref="Exception"/> 
        /// if it is Rejected.
        /// </summary>
        /// <returns></returns>
        public Task<TResult> GetTypedTask()
        {
            return Task.Run(() =>
            {
                ThisTask.Wait();
                if (IsFulfilled) return ThisResult;
                else throw Error;
            });
        }
        /// <summary>
        /// Gets the <see cref="TaskAwaiter{TResult}"/> for this <see cref="Promise{TResult}"/>.
        /// </summary>
        /// <returns>The <see cref="TaskAwaiter{TResult}"/> for this <see cref="Promise{TResult}"/>.</returns>
        public new TaskAwaiter<TResult> GetAwaiter() => GetTypedTask().GetAwaiter();
    }
}
