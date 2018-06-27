using System;
using System.Text;
using System.Diagnostics;
using PromiseCS.Tools;
using PromiseCS.Iteration;

namespace PromiseTester
{
    class Program
    {
        static string pingData = $"Test";
        static void Main(string[] args)
        {
            int count = 0;
            AsyncEnumerable<string> enumerable = new AsyncEnumerable<string>(() => new AsyncStreamEnumerator<string>(() =>
            {
                count++;
                Console.WriteLine($"Sending \"{pingData}\"... (#{count})");
                if (count == 25) return null;
                return PTools.SendToWebSocket(
                    new Uri("wss://echo.websocket.org"),
                    () => pingData, (pingData.Length + 1) * 2
                    )
                    .Timeout(2500)
                    .Then(bytes => Encoding.Unicode.GetString(bytes));
            }));

            Stopwatch watch = new Stopwatch();
            watch.Start();

            //The following two blocks of code do almost the same thing, but .ForEachAsync has promise error handling:

            //foreach (string s in enumerable)
            //{
            //    Console.WriteLine($"Received \"{s}\" in {watch.Elapsed}");
            //    watch.Restart();
            //}

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
