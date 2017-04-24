﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.WebSockets;

namespace Microsoft.AspNetCore.WebSockets.Internal
{
    public static class WebSocketFactory
    {
        public static WebSocket CreateClientWebSocket(Stream stream, string subProtocol, TimeSpan keepAliveInterval, int receiveBufferSize)
        {
            return ManagedWebSocket.CreateFromConnectedStream(
                stream,
                isServer: false,
                subprotocol: subProtocol,
                keepAliveInterval: keepAliveInterval,
                receiveBufferSize: receiveBufferSize);
        }

        public static WebSocket CreateServerWebSocket(Stream stream, string subProtocol, TimeSpan keepAliveInterval, int receiveBufferSize)
        {
            return ManagedWebSocket.CreateFromConnectedStream(
                stream,
                isServer: true,
                subprotocol: subProtocol,
                keepAliveInterval: keepAliveInterval,
                receiveBufferSize: receiveBufferSize);
        }
    }
}
