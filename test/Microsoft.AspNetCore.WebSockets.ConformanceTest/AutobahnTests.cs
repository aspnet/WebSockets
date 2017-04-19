﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.xunit;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest
{
    public class AutobahnTests : LoggedTest
    {
        public AutobahnTests(ITestOutputHelper output) : base(output)
        {
        }

        // Skip if wstest is not installed for now, see https://github.com/aspnet/WebSockets/issues/95
        // We will enable Wstest on every build once we've gotten the necessary infrastructure sorted out :).
        [ConditionalFact]
        [SkipIfWsTestNotPresent]
        public async Task AutobahnTestSuite()
        {
            using (StartLog(out var loggerFactory))
            {
                var reportDir = Environment.GetEnvironmentVariable("AUTOBAHN_SUITES_REPORT_DIR");
                var outDir = !string.IsNullOrEmpty(reportDir) ?
                    reportDir :
                    Path.Combine(AppContext.BaseDirectory, "autobahnreports");

                if (Directory.Exists(outDir))
                {
                    Directory.Delete(outDir, recursive: true);
                }

                outDir = outDir.Replace("\\", "\\\\");

                // 9.* is Limits/Performance which is VERY SLOW; 12.*/13.* are compression which we don't implement
                var spec = new AutobahnSpec(outDir)
                    .IncludeCase("*")
                    .ExcludeCase("9.*", "12.*", "13.*");

                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromMinutes(5)); // These tests generally complete in just over 1 minute.

                AutobahnResult result;
                using (var tester = new AutobahnTester(loggerFactory, spec))
                {
                    await tester.DeployTestAndAddToSpec(ServerType.Kestrel, ssl: false, environment: "ManagedSockets", cancellationToken: cts.Token);

                    // Windows-only WebListener tests, and Kestrel SSL tests (due to: https://github.com/aspnet/WebSockets/issues/102)
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        await tester.DeployTestAndAddToSpec(ServerType.Kestrel, ssl: true, environment: "ManagedSockets", cancellationToken: cts.Token);

                        if (IsWindows8OrHigher())
                        {
                            // WebListener occasionally gives a non-strict response on 3.2. IIS Express seems to have the same behavior. Wonder if it's related to HttpSys?
                            // For now, just allow the non-strict response, it's not a failure.
                            await tester.DeployTestAndAddToSpec(ServerType.WebListener, ssl: false, environment: "ManagedSockets", cancellationToken: cts.Token);
                        }
                    }

                    result = await tester.Run(cts.Token);
                    tester.Verify(result);
                }

                // If it hasn't been cancelled yet, cancel the token just to be sure
                cts.Cancel();
            }
        }

        private bool IsWindows8OrHigher()
        {
            const string WindowsName = "Microsoft Windows ";
            const int VersionOffset = 18;

            if (RuntimeInformation.OSDescription.StartsWith(WindowsName))
            {
                var versionStr = RuntimeInformation.OSDescription.Substring(VersionOffset);
                Version version;
                if (Version.TryParse(versionStr, out version))
                {
                    return version.Major > 6 || (version.Major == 6 && version.Minor >= 2);
                }
            }

            return false;
        }

        private bool IsIISExpress10Installed()
        {
            var pf = Environment.GetEnvironmentVariable("PROGRAMFILES");
            var iisExpressExe = Path.Combine(pf, "IIS Express", "iisexpress.exe");
            return File.Exists(iisExpressExe) && FileVersionInfo.GetVersionInfo(iisExpressExe).FileMajorPart >= 10;
        }
    }
}
