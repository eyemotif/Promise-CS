using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.IO;
using System.Text.RegularExpressions;

namespace PromiseCS.Tools
{
    /// <summary>
    /// Provides various tools that use <see cref="Promise"/>s.
    /// </summary>
    public static class PTools
    {
        /// <summary>
        /// Will a web request to a uri.
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
        /// Will download a file from a uri to a file on disk.
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
        /// Will download data from a uri.
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
        /// Will send data to a <see cref="Socket"/> under a given <see cref="IPAddress"/> and port.
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
        /// Will send data to a <see cref="WebSocket"/> under a given <see cref="Uri"/>.
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
        /// Will send data to a <see cref="WebSocket"/> under a given <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> of the <see cref="WebSocket"/>.</param>
        /// <param name="getData">A <see cref="Func{T}"/> that returns data to send to the <see cref="WebSocket"/>.</param>
        /// <param name="bufferSize">The receiving buffer size.</param>
        /// <returns>A new <see cref="Promise{T}"/> that will return the response of the <see cref="WebSocket"/>.</returns>
        public static Promise<byte[]> SendToWebSocket(Uri uri, Func<string> getData, int bufferSize = 1024)
        {
            return SendToWebSocket(uri, () => Encoding.Unicode.GetBytes(getData()), true, bufferSize);
        }
        /// <summary>
        /// Will return a <see cref="MatchCollection"/> of all occurrences of a regular expression in a string.
        /// </summary>
        /// <param name="input">The string to search.</param>
        /// <param name="expression">The <see cref="Regex"/> to use for a regular expression.</param>
        /// <returns>A <see cref="MatchCollection"/> of all occurrences of a regular expression in a string.</returns>
        public static Promise<MatchCollection> GetMatches(string input, Regex expression)
        {
            return new Promise<MatchCollection>((resolve, reject) =>
            {
                try { resolve(expression.Matches(input)); }
                catch (Exception e) { reject(e); }
            });
        }
    }
}
