// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;

namespace MiniBench
{
    internal class ReportingOptions
    {
        private CommandOption _appendOption;
        private CommandOption _outputOption;
        private CommandOption _quietOption;

        public string Append => _appendOption.HasValue() ? _appendOption.Value() : string.Empty;

        public string Output => _outputOption.HasValue() ? _outputOption.Value() : string.Empty;

        public bool Quiet => _quietOption.HasValue();

        public ReportingOptions(CommandOption appendOption, CommandOption outputOption, CommandOption quietOption)
        {
            _appendOption = appendOption;
            _outputOption = outputOption;
            _quietOption = quietOption;
        }

        public static ReportingOptions Attach(CommandLineApplication cmd)
        {
            var appendOption = cmd.Option("-a|--append <REPORT>", "Append results to the specified report file", CommandOptionType.SingleValue);
            var outputOption = cmd.Option("-o|--output <REPORT>", "Output results to the specified report file (overrides '-a' and will delete the report file if it already exists", CommandOptionType.SingleValue);
            var quietOption = cmd.Option("-q|--quiet", "Do not output result summary to Console.", CommandOptionType.NoValue);
            return new ReportingOptions(appendOption, outputOption, quietOption);
        }
    }
}