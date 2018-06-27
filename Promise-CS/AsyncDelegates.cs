using System;
using System.Threading.Tasks;

namespace PromiseCS.AsyncDelegates
{
    /// <summary>
    /// Represents an <see langword="async"/> <see cref="Action"/>.
    /// </summary>
    public delegate Task AsyncAction();
    /// <summary>
    /// Represents an <see langword="async"/> <see cref="Action{T1}"/>.
    /// </summary>
    public delegate Task AsyncAction<in T>(T arg1);
    /// <summary>
    /// Represents an <see langword="async"/> <see cref="Action{T1, T2}"/>.
    /// </summary>
    public delegate Task AsyncAction<in T1, in T2>(T1 arg1, T2 arg2);
    /// <summary>
    /// Represents an <see langword="async"/> <see cref="Action{T1, T2, T3}"/>.
    /// </summary>
    public delegate Task AsyncAction<in T1, in T2, in T3>(T1 arg1, T2 arg2, T3 arg3);
    /// <summary>
    /// Represents an <see langword="async"/> <see cref="Action{T1, T2, T3, T4}"/>.
    /// </summary>
    public delegate Task AsyncAction<in T1, in T2, in T3, in T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    /// <summary>
    /// Represents an <see langword="async"/> <see cref="Action{T1, T2, T3, T4, T5}"/>.
    /// </summary>
    public delegate Task AsyncAction<in T1, in T2, in T3, in T4, in T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    /// <summary>
    /// Represents an <see langword="async"/> <see cref="Func{TResult}"/>.
    /// </summary>
    public delegate Task<TResult> AsyncFunc<TResult>();
    /// <summary>
    /// Represents an <see langword="async"/> <see cref="Func{T1, TResult}"/>.
    /// </summary>
    public delegate Task<TResult> AsyncFunc<in T, TResult>(T arg1);
    /// <summary>
    /// Represents an <see langword="async"/> <see cref="Func{T1, T2, TResult}"/>.
    /// </summary>
    public delegate Task<TResult> AsyncFunc<in T1, in T2, TResult>(T1 arg1, T2 arg2);
    /// <summary>
    /// Represents an <see langword="async"/> <see cref="Func{T1, T2, T3, TResult}"/>.
    /// </summary>
    public delegate Task<TResult> AsyncFunc<in T1, in T2, in T3, TResult>(T1 arg1, T2 arg2, T3 arg3);
    /// <summary>
    /// Represents an <see langword="async"/> <see cref="Func{T1, T2, T3, T4, TResult}"/>.
    /// </summary>
    public delegate Task<TResult> AsyncFunc<in T1, in T2, in T3, in T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    /// <summary>
    /// Represents an <see langword="async"/> <see cref="Func{T1, T2, T3, T4, T5, TResult}"/>.
    /// </summary>
    public delegate Task<TResult> AsyncFunc<in T1, in T2, in T3, in T4, in T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
}
