// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.AspNetCore.WebSockets.Internal;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.WebSockets.Test
{
#if NET461
    // ClientWebSocket does not support WebSockets on these platforms and OS. Kestrel does support it.
    [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No WebSockets Client for this platform")]
#elif NETCOREAPP2_2
    // ClientWebSocket has added support for WebSockets on Win7.
#else
#error Unknown TFM
#endif
    public class WebSocketMiddlewareTests : LoggedTest
    {
        private static string ClientAddress = "ws://localhost:54321/";

        public WebSocketMiddlewareTests(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact]
        public async Task Connect_Success()
        {
            using (StartLog(out var loggerFactory))
            {
                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, async context =>
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
        }

        [ConditionalFact]
        public async Task NegotiateSubProtocol_Success()
        {
            using (StartLog(out var loggerFactory))
            {
                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, async context =>
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

                        // The Windows version of ClientWebSocket uses the casing from the header (Bravo)
                        // However, the Managed version seems match the header against the list generated by
                        // the AddSubProtocol calls (case-insensitively) and then use the version from
                        // that list as the value for SubProtocol. This is fine, but means we need to ignore case here.
                        // We could update our AddSubProtocols above to the same case but I think it's better to
                        // ensure this behavior is codified by this test.
                        Assert.Equal("Bravo", client.SubProtocol, ignoreCase: true);
                    }
                }
            }
        }

        [ConditionalFact]
        public async Task SendEmptyData_Success()
        {
            using (StartLog(out var loggerFactory))
            {
                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, async context =>
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
        }

        [ConditionalFact]
        public async Task SendShortData_Success()
        {
            using (StartLog(out var loggerFactory))
            {
                var orriginalData = Encoding.UTF8.GetBytes("Hello World");
                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, async context =>
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
        }

        [ConditionalFact]
        public async Task SendMediumData_Success()
        {
            using (StartLog(out var loggerFactory))
            {
                var orriginalData = Encoding.UTF8.GetBytes(new string('a', 130));
                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, async context =>
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
        }

        [ConditionalFact]
        public async Task SendLongData_Success()
        {
            using (StartLog(out var loggerFactory))
            {
                var orriginalData = Encoding.UTF8.GetBytes(new string('a', 0x1FFFF));
                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, async context =>
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
        }

        [ConditionalFact]
        public async Task SendFragmentedData_Success()
        {
            using (StartLog(out var loggerFactory))
            {
                var orriginalData = Encoding.UTF8.GetBytes("Hello World");
                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, async context =>
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
        }

        [ConditionalFact]
        public async Task ReceiveShortData_Success()
        {
            using (StartLog(out var loggerFactory))
            {
                var orriginalData = Encoding.UTF8.GetBytes("Hello World");
                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, async context =>
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
        }

        [ConditionalFact]
        public async Task ReceiveMediumData_Success()
        {
            using (StartLog(out var loggerFactory))
            {
                var orriginalData = Encoding.UTF8.GetBytes(new string('a', 130));
                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, async context =>
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
        }

        [ConditionalFact]
        public async Task ReceiveLongData()
        {
            using (StartLog(out var loggerFactory))
            {
                var orriginalData = Encoding.UTF8.GetBytes(new string('a', 0x1FFFF));
                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, async context =>
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
        }

        [ConditionalFact]
        public async Task ReceiveFragmentedData_Success()
        {
            using (StartLog(out var loggerFactory))
            {
                var orriginalData = Encoding.UTF8.GetBytes("Hello World");
                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, async context =>
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
        }

        [ConditionalFact]
        public async Task SendClose_Success()
        {
            using (StartLog(out var loggerFactory))
            {
                string closeDescription = "Test Closed";
                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, async context =>
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
        }

        [ConditionalFact]
        public async Task ReceiveClose_Success()
        {
            using (StartLog(out var loggerFactory))
            {
                string closeDescription = "Test Closed";
                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, async context =>
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
        }

        [ConditionalFact]
        public async Task CloseFromOpen_Success()
        {
            using (StartLog(out var loggerFactory))
            {
                string closeDescription = "Test Closed";
                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, async context =>
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
        }

        [ConditionalFact]
        public async Task CloseFromCloseSent_Success()
        {
            using (StartLog(out var loggerFactory))
            {
                string closeDescription = "Test Closed";
                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, async context =>
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
        }

        [ConditionalFact]
        public async Task CloseFromCloseReceived_Success()
        {
            using (StartLog(out var loggerFactory))
            {
                string closeDescription = "Test Closed";
                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, async context =>
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

        [Theory]
        [InlineData(HttpStatusCode.OK, null)]
        [InlineData(HttpStatusCode.Forbidden, "")]
        [InlineData(HttpStatusCode.Forbidden, "http://e.com")]
        [InlineData(HttpStatusCode.OK, "http://e.com", "http://example.com")]
        [InlineData(HttpStatusCode.OK, "*")]
        [InlineData(HttpStatusCode.OK, "http://e.com", "*")]
        public async Task CorsIsAppliedToWebSocketRequests(HttpStatusCode expectedCode, params string[] origins)
        {
            using (StartLog(out var loggerFactory))
            {
                //string closeDescription = "Test Closed";
                var options = new WebSocketOptions();
                if (origins != null)
                {
                    foreach (var origin in origins)
                    {
                        options.AllowedOrigins.Add(origin);
                    }
                }

                using (var server = KestrelWebSocketHelpers.CreateServer(loggerFactory, context =>
                {
                    Assert.True(context.WebSockets.IsWebSocketRequest);
                    return Task.CompletedTask;
                }, options))
                {
                    using (var client = new HttpClient())
                    {
                        var uri = new UriBuilder(ClientAddress);
                        uri.Scheme = "http";

                        // Craft a valid WebSocket Upgrade request
                        using (var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString()))
                        {
                            request.Headers.Connection.Clear();
                            request.Headers.Connection.Add("Upgrade");
                            request.Headers.Upgrade.Add(new System.Net.Http.Headers.ProductHeaderValue("websocket"));
                            request.Headers.Add(Constants.Headers.SecWebSocketVersion, Constants.Headers.SupportedVersion);
                            request.Headers.Add(Constants.Headers.SecWebSocketKey, Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }, Base64FormattingOptions.None));

                            request.Headers.Add("Origin", "http://example.com");

                            var response = await client.SendAsync(request);
                            Assert.Equal(expectedCode, response.StatusCode);
                        }
                    }
                }
            }
        }
    }
}
