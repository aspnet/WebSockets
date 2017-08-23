using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebSockets.Internal.fx.src.System.Net.WebSockets.Client.src.Resources;

namespace System.Net.WebSockets
{
    // Needed to support the WebSockets code from CoreFX.
    internal static class SR
    {
        internal static readonly string net_Websockets_AlreadyOneOutstandingOperation = Strings.net_Websockets_AlreadyOneOutstandingOperation;
        internal static readonly string net_WebSockets_Argument_InvalidMessageType = Strings.net_WebSockets_Argument_InvalidMessageType;
        internal static readonly string net_WebSockets_InvalidCharInProtocolString = Strings.net_WebSockets_InvalidCharInProtocolString;
        internal static readonly string net_WebSockets_InvalidCloseStatusCode = Strings.net_WebSockets_InvalidCloseStatusCode;
        internal static readonly string net_WebSockets_InvalidCloseStatusDescription = Strings.net_WebSockets_InvalidCloseStatusDescription;
        internal static readonly string net_WebSockets_InvalidEmptySubProtocol = Strings.net_WebSockets_InvalidEmptySubProtocol;
        internal static readonly string net_WebSockets_InvalidState = Strings.net_WebSockets_InvalidState;
        internal static readonly string net_WebSockets_InvalidState_ClosedOrAborted = Strings.net_WebSockets_InvalidState_ClosedOrAborted;
        internal static readonly string net_WebSockets_ReasonNotNull = Strings.net_WebSockets_ReasonNotNull;
        internal static readonly string net_WebSockets_UnsupportedPlatform = Strings.net_WebSockets_UnsupportedPlatform;

        internal static string Format(string name, params object[] args)
        {
            if (args != null)
            {
                return string.Format(name, args);
            }
            return name;
        }
    }
}
