// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace MiniBench
{
    public class WebServer : IDisposable
    {
        private IDisposable _hostShutdownToken;

        public Uri Url { get; }
        public Uri WebSocketUrl { get; }

        public WebServer(Uri url, IDisposable hostShutdownToken)
        {
            _hostShutdownToken = hostShutdownToken;
            Url = url;
            WebSocketUrl = new UriBuilder(url) { Scheme = "ws" }.Uri;
        }

        public static WebServer CreateServer(Action<IApplicationBuilder> startup)
        {
            Action<IApplicationBuilder> configure = builder =>
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
                startup(builder);
            };

            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection();
            var config = configBuilder.Build();
            config["server.urls"] = "http://127.0.0.1:0";

            var host = new WebHostBuilder()
                .UseConfiguration(config)
                .UseKestrel()
                .Configure(configure)
                .Build();

            host.Start();

            var url = new Uri(host.ServerFeatures.Get<IServerAddressesFeature>().Addresses.Single());

            return new WebServer(url, host);
        }

        public void Dispose()
        {
            _hostShutdownToken.Dispose();
        }
    }
}

