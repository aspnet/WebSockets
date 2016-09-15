// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.CommandLineUtils;

namespace MiniBench
{
    public class Program
    {
        public static readonly Assembly Asm = typeof(Program).GetTypeInfo().Assembly;
        public static readonly string Version = Asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        public static int Main(string[] args)
        {
            if (args.Any(a => string.Equals(a, "--debug", StringComparison.Ordinal)))
            {
                args = args.Where(a => !string.Equals(a, "--debug", StringComparison.Ordinal)).ToArray();
                Console.WriteLine($"Waiting for debugger. Process ID: {System.Diagnostics.Process.GetCurrentProcess().Id}");
                Console.WriteLine("Press ENTER to continue...");
                Console.ReadLine();
            }
            var cancellationToken = CreateCtrlCCancelToken();

            var app = new CommandLineApplication();
            app.Name = Asm.GetName().Name;
            app.FullName = "ASP.NET WebSocket Micro-benchmarker";
            app.VersionOption("-v|--version", Version);
            app.HelpOption("-h|-?|--help");

            app.Command("corefx", ScenarioRunner.Create(new CoreFXScenario(), cancellationToken));

            app.Command("help", help =>
            {
                help.Description = "Get help for individual scenarios";
                var commandArg = help.Argument("COMMAND", "The command to get help for");
                help.OnExecute(() =>
                {
                    app.ShowHelp(commandArg.Value);
                    return 0;
                });
            });

            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 0;
            });

            return app.Execute(args);
        }

        private static CancellationToken CreateCtrlCCancelToken()
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                // We've already been cancelled once... let the process terminate
                if (cts.IsCancellationRequested)
                {
                    return;
                }

                Console.WriteLine("Attempting to cancel, press Ctrl-C again to terminate...");
                cts.Cancel();
                e.Cancel = true;
            };
            return cts.Token;
        }
    }
}
