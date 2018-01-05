// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.WebSockets.Test
{
    public class WebSocketMiddlewareTests
    {
        private static string ClientAddress = "ws://localhost:54321/";

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
        public async Task Connect_Success()
        {
            using (var server = KestrelWebSocketHelpers.CreateServer(async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    await client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
        public async Task NegotiateSubProtocol_Success()
        {
            using (var server = KestrelWebSocketHelpers.CreateServer(async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                Assert.Equal("alpha, bravo, charlie", context.Request.Headers["Sec-WebSocket-Protocol"]);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync("Bravo");
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    client.Options.AddSubProtocol("alpha");
                    client.Options.AddSubProtocol("bravo");
                    client.Options.AddSubProtocol("charlie");
                    await client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);
                    Assert.Equal("Bravo", client.SubProtocol);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
        public async Task SendEmptyData_Success()
        {
            using (var server = KestrelWebSocketHelpers.CreateServer(async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                var serverBuffer = new byte[0];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(0, result.Count);
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    await client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);
                    var orriginalData = new byte[0];
                    await client.SendAsync(new ArraySegment<byte>(orriginalData), WebSocketMessageType.Binary, true, CancellationToken.None);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
        public async Task SendShortData_Success()
        {
            var orriginalData = Encoding.UTF8.GetBytes("Hello World");
            using (var server = KestrelWebSocketHelpers.CreateServer(async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                var serverBuffer = new byte[orriginalData.Length];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(orriginalData.Length, result.Count);
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                Assert.Equal(orriginalData, serverBuffer);
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    await client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);
                    await client.SendAsync(new ArraySegment<byte>(orriginalData), WebSocketMessageType.Binary, true, CancellationToken.None);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
        public async Task SendMediumData_Success()
        {
            var orriginalData = Encoding.UTF8.GetBytes(new string('a', 130));
            using (var server = KestrelWebSocketHelpers.CreateServer(async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                var serverBuffer = new byte[orriginalData.Length];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(orriginalData.Length, result.Count);
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                Assert.Equal(orriginalData, serverBuffer);
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    await client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);
                    await client.SendAsync(new ArraySegment<byte>(orriginalData), WebSocketMessageType.Binary, true, CancellationToken.None);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
        public async Task SendLongData_Success()
        {
            var orriginalData = Encoding.UTF8.GetBytes(new string('a', 0x1FFFF));
            using (var server = KestrelWebSocketHelpers.CreateServer(async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                var serverBuffer = new byte[orriginalData.Length];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                int intermediateCount = result.Count;
                Assert.False(result.EndOfMessage);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer, intermediateCount, orriginalData.Length - intermediateCount), CancellationToken.None);
                intermediateCount += result.Count;
                Assert.False(result.EndOfMessage);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer, intermediateCount, orriginalData.Length - intermediateCount), CancellationToken.None);
                intermediateCount += result.Count;
                Assert.True(result.EndOfMessage);
                Assert.Equal(orriginalData.Length, intermediateCount);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                Assert.Equal(orriginalData, serverBuffer);
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    await client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);
                    await client.SendAsync(new ArraySegment<byte>(orriginalData), WebSocketMessageType.Binary, true, CancellationToken.None);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
        public async Task SendFragmentedData_Success()
        {
            var orriginalData = Encoding.UTF8.GetBytes("Hello World");
            using (var server = KestrelWebSocketHelpers.CreateServer(async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                var serverBuffer = new byte[orriginalData.Length];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.False(result.EndOfMessage);
                Assert.Equal(2, result.Count);
                int totalReceived = result.Count;
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

                result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(serverBuffer, totalReceived, serverBuffer.Length - totalReceived), CancellationToken.None);
                Assert.False(result.EndOfMessage);
                Assert.Equal(2, result.Count);
                totalReceived += result.Count;
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

                result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(serverBuffer, totalReceived, serverBuffer.Length - totalReceived), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(7, result.Count);
                totalReceived += result.Count;
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

                Assert.Equal(orriginalData, serverBuffer);
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    await client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);
                    await client.SendAsync(new ArraySegment<byte>(orriginalData, 0, 2), WebSocketMessageType.Binary, false, CancellationToken.None);
                    await client.SendAsync(new ArraySegment<byte>(orriginalData, 2, 2), WebSocketMessageType.Binary, false, CancellationToken.None);
                    await client.SendAsync(new ArraySegment<byte>(orriginalData, 4, 7), WebSocketMessageType.Binary, true, CancellationToken.None);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
        public async Task ReceiveShortData_Success()
        {
            var orriginalData = Encoding.UTF8.GetBytes("Hello World");
            using (var server = KestrelWebSocketHelpers.CreateServer(async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                await webSocket.SendAsync(new ArraySegment<byte>(orriginalData), WebSocketMessageType.Binary, true, CancellationToken.None);
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    await client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);
                    var clientBuffer = new byte[orriginalData.Length];
                    var result = await client.ReceiveAsync(new ArraySegment<byte>(clientBuffer), CancellationToken.None);
                    Assert.True(result.EndOfMessage);
                    Assert.Equal(orriginalData.Length, result.Count);
                    Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                    Assert.Equal(orriginalData, clientBuffer);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
        public async Task ReceiveMediumData_Success()
        {
            var orriginalData = Encoding.UTF8.GetBytes(new string('a', 130));
            using (var server = KestrelWebSocketHelpers.CreateServer(async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                await webSocket.SendAsync(new ArraySegment<byte>(orriginalData), WebSocketMessageType.Binary, true, CancellationToken.None);
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    await client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);
                    var clientBuffer = new byte[orriginalData.Length];
                    var result = await client.ReceiveAsync(new ArraySegment<byte>(clientBuffer), CancellationToken.None);
                    Assert.True(result.EndOfMessage);
                    Assert.Equal(orriginalData.Length, result.Count);
                    Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                    Assert.Equal(orriginalData, clientBuffer);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
        public async Task ReceiveLongData()
        {
            var orriginalData = Encoding.UTF8.GetBytes(new string('a', 0x1FFFF));
            using (var server = KestrelWebSocketHelpers.CreateServer(async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                await webSocket.SendAsync(new ArraySegment<byte>(orriginalData), WebSocketMessageType.Binary, true, CancellationToken.None);
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    await client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);
                    var clientBuffer = new byte[orriginalData.Length];
                    WebSocketReceiveResult result;
                    int receivedCount = 0;
                    do
                    {
                        result = await client.ReceiveAsync(new ArraySegment<byte>(clientBuffer, receivedCount, clientBuffer.Length - receivedCount), CancellationToken.None);
                        receivedCount += result.Count;
                        Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                    }
                    while (!result.EndOfMessage);

                    Assert.Equal(orriginalData.Length, receivedCount);
                    Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                    Assert.Equal(orriginalData, clientBuffer);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
        public async Task ReceiveFragmentedData_Success()
        {
            var orriginalData = Encoding.UTF8.GetBytes("Hello World");
            using (var server = KestrelWebSocketHelpers.CreateServer(async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                await webSocket.SendAsync(new ArraySegment<byte>(orriginalData, 0, 2), WebSocketMessageType.Binary, false, CancellationToken.None);
                await webSocket.SendAsync(new ArraySegment<byte>(orriginalData, 2, 2), WebSocketMessageType.Binary, false, CancellationToken.None);
                await webSocket.SendAsync(new ArraySegment<byte>(orriginalData, 4, 7), WebSocketMessageType.Binary, true, CancellationToken.None);
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    await client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);
                    var clientBuffer = new byte[orriginalData.Length];
                    var result = await client.ReceiveAsync(new ArraySegment<byte>(clientBuffer), CancellationToken.None);
                    Assert.False(result.EndOfMessage);
                    Assert.Equal(2, result.Count);
                    int totalReceived = result.Count;
                    Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

                    result = await client.ReceiveAsync(
                        new ArraySegment<byte>(clientBuffer, totalReceived, clientBuffer.Length - totalReceived), CancellationToken.None);
                    Assert.False(result.EndOfMessage);
                    Assert.Equal(2, result.Count);
                    totalReceived += result.Count;
                    Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

                    result = await client.ReceiveAsync(
                        new ArraySegment<byte>(clientBuffer, totalReceived, clientBuffer.Length - totalReceived), CancellationToken.None);
                    Assert.True(result.EndOfMessage);
                    Assert.Equal(7, result.Count);
                    totalReceived += result.Count;
                    Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

                    Assert.Equal(orriginalData, clientBuffer);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
        public async Task SendClose_Success()
        {
            string closeDescription = "Test Closed";
            using (var server = KestrelWebSocketHelpers.CreateServer(async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                var serverBuffer = new byte[1024];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(0, result.Count);
                Assert.Equal(WebSocketMessageType.Close, result.MessageType);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
                Assert.Equal(closeDescription, result.CloseStatusDescription);
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    await client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);
                    await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);

                    Assert.Equal(WebSocketState.CloseSent, client.State);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
        public async Task ReceiveClose_Success()
        {
            string closeDescription = "Test Closed";
            using (var server = KestrelWebSocketHelpers.CreateServer(async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    await client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);
                    var clientBuffer = new byte[1024];
                    var result = await client.ReceiveAsync(new ArraySegment<byte>(clientBuffer), CancellationToken.None);
                    Assert.True(result.EndOfMessage);
                    Assert.Equal(0, result.Count);
                    Assert.Equal(WebSocketMessageType.Close, result.MessageType);
                    Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
                    Assert.Equal(closeDescription, result.CloseStatusDescription);

                    Assert.Equal(WebSocketState.CloseReceived, client.State);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
        public async Task CloseFromOpen_Success()
        {
            string closeDescription = "Test Closed";
            using (var server = KestrelWebSocketHelpers.CreateServer(async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                var serverBuffer = new byte[1024];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(0, result.Count);
                Assert.Equal(WebSocketMessageType.Close, result.MessageType);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
                Assert.Equal(closeDescription, result.CloseStatusDescription);

                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    await client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);

                    Assert.Equal(WebSocketState.Closed, client.State);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
        public async Task CloseFromCloseSent_Success()
        {
            string closeDescription = "Test Closed";
            using (var server = KestrelWebSocketHelpers.CreateServer(async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                var serverBuffer = new byte[1024];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(0, result.Count);
                Assert.Equal(WebSocketMessageType.Close, result.MessageType);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
                Assert.Equal(closeDescription, result.CloseStatusDescription);

                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    await client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);
                    await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);
                    Assert.Equal(WebSocketState.CloseSent, client.State);

                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);
                    Assert.Equal(WebSocketState.Closed, client.State);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
        public async Task CloseFromCloseReceived_Success()
        {
            string closeDescription = "Test Closed";
            using (var server = KestrelWebSocketHelpers.CreateServer(async context =>
            {
                Assert.True(context.WebSockets.IsWebSocketRequest);
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);

                var serverBuffer = new byte[1024];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(0, result.Count);
                Assert.Equal(WebSocketMessageType.Close, result.MessageType);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
                Assert.Equal(closeDescription, result.CloseStatusDescription);
            }))
            {
                using (var client = new ClientWebSocket())
                {
                    await client.ConnectAsync(new Uri(ClientAddress), CancellationToken.None);
                    var clientBuffer = new byte[1024];
                    var result = await client.ReceiveAsync(new ArraySegment<byte>(clientBuffer), CancellationToken.None);
                    Assert.True(result.EndOfMessage);
                    Assert.Equal(0, result.Count);
                    Assert.Equal(WebSocketMessageType.Close, result.MessageType);
                    Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
                    Assert.Equal(closeDescription, result.CloseStatusDescription);

                    Assert.Equal(WebSocketState.CloseReceived, client.State);

                    await client.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);

                    Assert.Equal(WebSocketState.Closed, client.State);
                }
            }
        }
    }
}
