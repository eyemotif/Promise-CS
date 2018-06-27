using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.IO;

namespace PromiseCS.Tools
{
    /// <summary>
    /// Provides various tools that use <see cref="Promise"/>s.
    /// </summary>
    public static class PTools
    {
        /// <summary>
        /// Makes a web request to a uri.
        /// </summary>
        /// <param name="uri">The uri to request.</param>
        /// <returns>A new <see cref="Promise{String}"/> that will return the value of the response.</returns>
        public static Promise<string> Fetch(string uri)
        {
            return new Promise<string>((resolve, reject) =>
            {
                try
                {
                    WebRequest request = WebRequest.Create(uri);
                    WebResponse response = request.GetResponse();
                    using (Stream dataStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(dataStream);
                        resolve(reader.ReadToEnd());
                        reader.Close();
                    }
                }
                catch (Exception e) { reject(e); }
            });
        }
        /// <summary>
        /// Downloads a file from a uri to a file on disk.
        /// </summary>
        /// <param name="fromUri">The uri to download from.</param>
        /// <param name="toDiskPath">The file on disk to download to.</param>
        /// <returns>A new <see cref="Promise"/> that will resolve when the download is done.</returns>
        public static Promise DownloadFile(string fromUri, string toDiskPath)
        {
            return new Promise((resolve, reject) =>
            {
                try
                {
                    using (WebClient client = new WebClient())
                        client.DownloadFile(fromUri, toDiskPath);
                    resolve();
                }
                catch (Exception e) { reject(e); }
            });
        }
        /// <summary>
        /// Downloads data from a uri.
        /// </summary>
        /// <param name="fromUri">The uri to download from.</param>
        /// <returns>A new <see cref="Promise{Byte}"/> that will return the data downloaded.</returns>
        public static Promise<byte[]> DownloadData(string fromUri)
        {
            return new Promise<byte[]>((resolve, reject) =>
            {
                try
                {
                    using (WebClient client = new WebClient())
                        resolve(client.DownloadData(fromUri));
                }
                catch (Exception e) { reject(e); }
            });
        }
        /// <summary>
        /// Waits for a set amount of milliseconds, then executes a <see cref="Func{T}"/>.
        /// </summary>
        /// <typeparam name="T">The return type of the <see cref="Func{T}"/>.</typeparam>
        /// <param name="msDelay">The amount of milliseconds to delay.</param>
        /// <param name="function">The <see cref="Func{T}"/> to execute.</param>
        /// <returns>A new <see cref="Promise{T}"/> that will return the result of <paramref name="function"/></returns>
        public static Promise<T> Delay<T>(int msDelay, Func<T> function)
        {
            return new Promise<T>((resolve, reject) =>
            {
                Thread.Sleep(msDelay);
                resolve(function());
            });
        }
        /// <summary>
        /// Sends data to a <see cref="Socket"/> under a given <see cref="IPAddress"/> and port.
        /// </summary>
        /// <param name="addr">The <see cref="IPAddress"/> of the <see cref="Socket"/>.</param>
        /// <param name="port">The port of the <see cref="Socket"/>.</param>
        /// <param name="getData">A <see cref="Func{T}"/> that returns data to send to the <see cref="Socket"/>.</param>
        /// <param name="bufferSize">The receiving buffer size.</param>
        /// <returns>A new <see cref="Promise{T}"/> that will return the response of the <see cref="Socket"/>.</returns>
        public static Promise<byte[]> SendToSocket(IPAddress addr, int port, Func<byte[]> getData, int bufferSize = 1024)
        {
            return new Promise<byte[]>((resolve, reject) =>
            {
                IPEndPoint ipe = new IPEndPoint(addr, port);
                List<byte> bytes = new List<byte>();
                byte[] buffer = new byte[bufferSize];
                int amt = 0;

                try
                {
                    Socket s = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    s.Send(getData());
                    do
                    {
                        amt = s.Receive(buffer);
                        bytes.AddRange(new ArraySegment<byte>(buffer, 0, amt));
                    } while (amt > 0);
                    s.Disconnect(false);
                    resolve(bytes.ToArray());
                }
                catch (Exception e) { reject(e); }
            });
        }
        /// <summary>
        /// Sends data to a <see cref="WebSocket"/> under a given <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> of the <see cref="WebSocket"/>.</param>
        /// <param name="getData">A <see cref="Func{T}"/> that returns data to send to the <see cref="WebSocket"/>.</param>
        /// <param name="dataIsText">If the data given will be text.</param>
        /// <param name="bufferSize">The receiving buffer size.</param>
        /// <returns>A new <see cref="Promise{T}"/> that will return the response of the <see cref="WebSocket"/>.</returns>
        public static Promise<byte[]> SendToWebSocket(Uri uri, Func<byte[]> getData, bool dataIsText, int bufferSize = 1024)
        {
            return new Promise<byte[]>(async (resolve, reject) =>
            {
                ClientWebSocket websocket = new ClientWebSocket();
                List<byte> bytes = new List<byte>();
                WebSocketReceiveResult result;
                byte[] buffer = new byte[bufferSize];
                ArraySegment<byte> seg = new ArraySegment<byte>(buffer, 0, buffer.Length);
                try
                {
                    await websocket.ConnectAsync(uri, new CancellationToken());
                    await websocket.SendAsync(getData(), dataIsText ? WebSocketMessageType.Text : WebSocketMessageType.Binary, true, new CancellationToken());
                    do
                    {
                        result = await websocket.ReceiveAsync(seg, new CancellationToken());
                        bytes.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));
                    } while (!result.EndOfMessage);
                    await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "End of connection", new CancellationToken());
                    resolve(bytes.ToArray());
                } catch (Exception e) { reject(e); }
            });
        }
        /// <summary>
        /// Sends data to a <see cref="WebSocket"/> under a given <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> of the <see cref="WebSocket"/>.</param>
        /// <param name="getData">A <see cref="Func{T}"/> that returns data to send to the <see cref="WebSocket"/>.</param>
        /// <param name="bufferSize">The receiving buffer size.</param>
        /// <returns>A new <see cref="Promise{T}"/> that will return the response of the <see cref="WebSocket"/>.</returns>
        public static Promise<byte[]> SendToWebSocket(Uri uri, Func<string> getData, int bufferSize = 1024)
        {
            return SendToWebSocket(uri, () => Encoding.Unicode.GetBytes(getData()), true, bufferSize);
        }
    }
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
