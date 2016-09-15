// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace MiniBench
{
    internal class CoreFXScenario : Scenario
    {
        public override string FullName => ".NET BCL/CoreFX WebSockets";

        public override string Name => "CoreFX";

        private WebServer _server;

        public override Task Initialize(TextWriter output, CancellationToken cancellationToken)
        {
            // Create the server
            _server = WebServer.CreateServer(app =>
            {
                app.UseWebSockets();
                app.Use(async (context, next) =>
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        var serverSocket = await context.WebSockets.AcceptWebSocketAsync();
                        output.WriteLine("S: WebSocket Accepted");
                        var buf = new byte[1024];
                        var receive = await serverSocket.ReceiveAsync(new ArraySegment<byte>(buf), cancellationToken);
                        while (!receive.CloseStatus.HasValue)
                        {
                            // Echo the message
                            await serverSocket.SendAsync(new ArraySegment<byte>(buf, 0, receive.Count), receive.MessageType, receive.EndOfMessage, cancellationToken);

                            // Receive a new message
                            receive = await serverSocket.ReceiveAsync(new ArraySegment<byte>(buf), cancellationToken);
                        }

                        await serverSocket.CloseAsync(receive.CloseStatus.Value, receive.CloseStatusDescription, cancellationToken);
                    }
                });
            });

            return Task.FromResult(0);
        }

        public override async Task<ScenarioResult> Run(TextWriter output, BenchmarkOptions benchmarkOptions, CancellationToken cancellationToken)
        {
            // Create some random data to send back and forth
            var payload = new byte[256];
            new Random().NextBytes(payload);

            // Establish the web socket
            var clientSocket = new ClientWebSocket();
            output.WriteLine("C: WebSocket Connecting to " + _server.WebSocketUrl);
            await clientSocket.ConnectAsync(_server.WebSocketUrl, cancellationToken);
            output.WriteLine("C: WebSocket Connected");

            // Do the warm-up
            output.WriteLine("C: Beginning Warmup");
            for (var i = 0; i < benchmarkOptions.WarmupIterations; i++)
            {
                // Don't care about the results for this.
                await PerformIteration(clientSocket, benchmarkOptions.PipelineDepth, payload, cancellationToken);
            }

            // Now perform the test
            Console.WriteLine("C: Beginning Test");
            int totalMessages = 0;
            var startTime = DateTime.UtcNow;
            var endTime = startTime + benchmarkOptions.TestLength;
            var stopwatch = Stopwatch.StartNew();
            while (DateTime.UtcNow < endTime)
            {
                totalMessages += await PerformIteration(clientSocket, benchmarkOptions.PipelineDepth, payload, cancellationToken);
            }
            stopwatch.Stop();

            await clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test Complete", cancellationToken);

            return new ScenarioResult(
                Name,
                benchmarkOptions.PipelineDepth,
                startTime,
                benchmarkOptions.TestLength,
                stopwatch.Elapsed,
                totalMessages);
        }

        private static async Task<int> PerformIteration(ClientWebSocket clientSocket, int pipelineDepth, byte[] payloadBase, CancellationToken cancellationToken)
        {
            var status = new bool[pipelineDepth];
            for (var i = 0; i < pipelineDepth; i++)
            {
                status[i] = false;
                payloadBase[0] = (byte)i;
                await clientSocket.SendAsync(new ArraySegment<byte>(payloadBase), WebSocketMessageType.Binary, endOfMessage: true, cancellationToken: cancellationToken);
            }

            // Now wait for the responses
            var buf = new byte[270]; // Just allocate a little more to be safe...
            while (status.Any(b => !b))
            {
                var resp = await ReceiveWholeMessageAsync(clientSocket, buf, cancellationToken);
                if (resp.CloseStatus.HasValue)
                {
                    throw new OperationCanceledException("Server closed socket!");
                }
                if (resp.Count != payloadBase.Length)
                {
                    throw new InvalidOperationException($"Incomplete payload! Expected {payloadBase.Length} bytes but got {resp.Count}");
                }
                var msg = buf[0];
                if (msg > pipelineDepth)
                {
                    throw new InvalidOperationException("Ack for unsent message??");
                }
                status[msg] = true;
            }

            return pipelineDepth;
        }

        private static async Task<WebSocketReceiveResult> ReceiveWholeMessageAsync(WebSocket clientSocket, byte[] buf, CancellationToken cancellationToken)
        {
            var offset = 0;
            var received = 0;
            WebSocketReceiveResult resp;
            do
            {
                resp = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buf, offset, buf.Length - offset), cancellationToken);

                offset += resp.Count;
                received += resp.Count;
            } while (!resp.CloseStatus.HasValue && !resp.EndOfMessage);

            return new WebSocketReceiveResult(received, resp.MessageType, resp.EndOfMessage, resp.CloseStatus, resp.CloseStatusDescription);

        }

        public override void Dispose()
        {
            _server.Dispose();
        }
    }
}