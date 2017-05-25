#if NET461
using System;

namespace Microsoft.AspNetCore.WebSockets.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("This Program.Main is only here to work around https://github.com/dotnet/sdk/issues/909");
        }
    }
}
#elif NETCOREAPP2_0
#else
#error Target frameworks need to be updated
#endif
