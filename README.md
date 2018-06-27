# Promise-CS
Promises for C#.

This is a small project, and I'm making it just because I wanted to create easy asynchronous code in c#.
The main class library is in [Promise-CS/](Promise-CS), and there is a NuGet package to install under the same name.

Here is a YouTube series that I'll be doing that will go into the details on how to use this library: [click here!](https://www.youtube.com/playlist?list=PL_rR6KDNtaPIt6-pOY4wu-VNAImpJs8wr)

# Here's a quick guide:

To create a promise from scratch, you only need this code:

```c#
using System; //For the Console class
using System.Threading; //For Thread.Sleep()
using PromiseCS; //For the promise
```

```c#
Promise<string> delayedPromise = new Promise<string>((resolve, reject) => 
{
    //Code to execute asynchronously:
    Thread.Sleep(1000);
    resolve("Hello, world!");
});
```

The promise will start executing immediately. The promise's result will now be `"Hello, world!"`, so we can then do something like this:

```c#
delayedPromise.Then(result => Console.WriteLine(result));
```

Note that `.Then` returns a new promise, so you could write the two above statements as one line:

```c#
Promise delayedPromise = new Promise<string>((resolve, reject) => 
{
    Thread.sleep(1000);
    resolve("Hello, world!");
}).Then(result => Console.WriteLine(result));
```

Depending on what you return in `.Then` changes the final Promise's type. In this case, `.Then` didn't return anything, so the final Promise
did not have a return value. I suggest using the `var` keyword when creating Promise variables.


Finally, if you want to handle errors within your promise:

```c#
using System; //For the Console class
using System.Net.NetworkInformation; //For Ping and PingReply
using PromiseCS; //For the promise

var pingPromise = new Promise<PingReply>((resolve, reject) => 
{
    try
    {
        Ping pinger = new Ping();
        PingReply reply = pinger.Send("https://github.com");
        resolve(reply);
    }
    catch (Exception e) { reject(e); }
})
.Then(reply => Console.WriteLine($"Success! (Roundtrip time: {reply.RoundtripTime} ms)")
.Catch(e => Console.WriteLine($"Error! {e}")
.Finally(() => Console.WriteLine("Done!");
```

- In `.Then`, `reply` will be the same value as the one that was used to call `resolve()`.
- In `.Catch`, `e` will be the exception caught by the `catch` block, and used to call `reject()`.
- `Then` will be called if either `resolve()` or `reject()` was called.

Of course, `PromiseCS.Tools` provides tools so that you don't have to create your own promises:

```c#
using PromiseCS.Tools; //For PTools.Fetch

var fetchPromise = PTools.Fetch("https://github.com")
    .Then(text => Console.WriteLine($"Contents of GitHub's main page: \n{text}")
    .Catch(e => Console.WriteLine($"An exception occurred! \n{e}");
```

And that's it! Please check out the playlist linked at the top if you want an in-depth guide on how to use all the features in this library.
