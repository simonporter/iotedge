﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.Devices.Edge.Hub.Service
{
    using System;
    using System.Diagnostics.Tracing;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Common.Internal.Logging;
    using Microsoft.Azure.Devices.Edge.Hub.Mqtt;
    using Microsoft.Azure.Devices.ProtocolGateway;
    using Microsoft.Azure.Devices.ProtocolGateway.Instrumentation;
    using Microsoft.Extensions.Logging;
    using Serilog;
    using Serilog.Core;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    class Program
    {
        public static int Main() => MainAsync().Result;

        static async Task<int> MainAsync()
        {
            Logger loggerConfig = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] - {Message}{NewLine}{Exception}"
                )
                .CreateLogger();

            ILoggerFactory factory = new LoggerFactory()
                .AddSerilog(loggerConfig);
            ILogger logger = factory.CreateLogger<Program>();

            logger.LogInformation("Starting local IoT Hub.");

            var eventListener = new ConsoleEventListner();

            eventListener.EnableEvents(CommonEventSource.Log, EventLevel.Verbose);
            eventListener.EnableEvents(DefaultEventSource.Log, EventLevel.Verbose);

            var cts = new CancellationTokenSource();

            // TODO: Read certificate from device secret store
            string certPath = Path.Combine(Environment.GetEnvironmentVariable("SSL_CERTIFICATE_PATH"), Environment.GetEnvironmentVariable("SSL_CERTIFICATE_NAME"));
            var certificate = new X509Certificate2(certPath);
            var settingsProvider = new AppConfigSettingsProvider();

            var bootstrapper = new MqttBootstrapper(settingsProvider, certificate);

            await bootstrapper.StartAsync(cts.Token);

            while (true)
            {
                string input = Console.ReadLine();
                if (input != null && input.ToLowerInvariant() == "exit")
                {
                    break;
                }
            }

            cts.Cancel();
            bootstrapper.CloseCompletion.Wait(TimeSpan.FromSeconds(20));

            return 0;
        }
    }
}