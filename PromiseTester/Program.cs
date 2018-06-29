using System;
using System.Text;
using System.Diagnostics;
using PromiseCS.Tools;
using PromiseCS.Iteration;
using PromiseCS;

namespace PromiseTester
{
    /// <summary>
    /// Here you will see the most recent features being tested.
    /// </summary>
    class Program
    {
        static string pingData = $"Test";
        static Random rand = new Random();
        static void Main(string[] args)
        {
            AsyncEnumerable<string> enumerable = new AsyncEnumerable<string>(() => new AsyncGenerator<string>((yield, complete) =>
            {
                for (int x = 0; x < 25; x++)
                {
                    Console.WriteLine($"Sending \"{pingData}\"... (#{x})");
                    yield(PTools.SendToWebSocket(
                        new Uri("wss://echo.websocket.org"),
                        () => $"{pingData}{rand.Next()}", (pingData.Length + 1) * 2
                        )
                        //.Timeout(2500)
                        .Then(bytes => Encoding.Unicode.GetString(bytes))
                        );
                }
            }));

            Stopwatch watch = new Stopwatch();
            watch.Start();

            //The following two blocks of code do almost the same thing, but .ForEachAsync has promise error handling:

            foreach (string s in enumerable)
            {
                Console.WriteLine($"Received \"{s}\" in {watch.Elapsed}");
                watch.Restart();
            }

            //var enumeratePromise = enumerable
            //    .ForEachAsync(s =>
            //    {
            //        Console.WriteLine($"Received \"{s}\" in {watch.Elapsed}");
            //        watch.Restart();
            //    })
            //    .Catch(e => Console.WriteLine(e));
            //enumeratePromise.Wait();

            Console.WriteLine("Done!");
            Console.ReadKey();
        }
    }
}
