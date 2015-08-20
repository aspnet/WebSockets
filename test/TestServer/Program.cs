﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            RunEchoServer().Wait();
        }

        private static async Task RunEchoServer()
        {
            var username = Environment.GetEnvironmentVariable("USERNAME");
            var userdomain = Environment.GetEnvironmentVariable("USERDOMAIN");
            var prefix = "http://*:12345/";

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            try
            {
                listener.Start();
            }
            catch (HttpListenerException ex)
            {
                if (ex.ErrorCode == 5)
                {
                    Console.WriteLine("You need to run the following command:");
                    Console.WriteLine("  netsh http add urlacl url={0} user={1}\\{2} listen=yes", prefix, userdomain, username);

                    Console.ReadLine();
                    return;
                }

                throw; 
            }
            Console.WriteLine("Started");

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                if (!context.Request.IsWebSocketRequest)
                {
                    context.Response.Close();
                    continue;
                }
                Console.WriteLine("Accepted");

                var wsContext = await context.AcceptWebSocketAsync(null);
                var webSocket = wsContext.WebSocket;

                byte[] buffer = new byte[1024];
                WebSocketReceiveResult received = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (received.MessageType != WebSocketMessageType.Close)
                {
                    // Console.WriteLine("Echo, " + received.Count + ", " + received.MessageType + ", " + received.EndOfMessage);
                    // Echo anything we receive
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, received.Count), received.MessageType, received.EndOfMessage, CancellationToken.None);

                    received = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                await webSocket.CloseAsync(received.CloseStatus.Value, received.CloseStatusDescription, CancellationToken.None);

                webSocket.Dispose();
                Console.WriteLine("Finished");
            }
        }
    }
}
