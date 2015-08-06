// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Dnx.Runtime.Infrastructure;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.Configuration.Memory;

namespace Microsoft.AspNet.WebSockets.Client.Tests
{
    public class KestrelWebSocketHelpers
    {
        public static IDisposable CreateServer(Func<HttpContext, Task> app)
        {
            Action<IApplicationBuilder> startup = builder =>
            {
                builder.Use(async (ct, next) =>
                {
                    try
                    {
                        // Kestrel does not return proper error responses:
                        // https://github.com/aspnet/KestrelHttpServer/issues/43
                        await next();
                    }
                    catch (Exception ex)
                    {
                        if (ct.Response.HasStarted)
                        {
                            throw;
                        }

                        ct.Response.StatusCode = 500;
                        ct.Response.Headers.Clear();
                        await ct.Response.WriteAsync(ex.ToString());
                    }
                });
                builder.UseWebSockets();
                builder.Run(c => app(c));
            };

            var configBuilder = new ConfigurationBuilder();
            configBuilder.Add(new MemoryConfigurationSource());
            var config = configBuilder.Build();
            config.Set("server.urls", "http://localhost:54321");

            var host = new WebHostBuilder(CallContextServiceLocator.Locator.ServiceProvider, config)
                .UseServer("Kestrel")
                .UseStartup(startup)
                .Build();

            return host.Start();
        }
    }
}
