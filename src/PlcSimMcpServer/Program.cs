using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace PlcSimMcpServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Initialize PLCSIM API resolution dynamically before any tools are loaded
            ApiResolver.Initialize();

            var options = CliOptions.ParseArgs(args);

            if (options.Doctor)
            {
                RunDoctor();
                return;
            }

            await RunStdioHost(options);
        }

        public static void RunDoctor()
        {
            Console.Error.WriteLine("Diagnostics for PlcSimMcpServer:");
            Console.Error.WriteLine("- Working directory: " + Environment.CurrentDirectory);
            Console.Error.WriteLine("- CLR version: " + Environment.Version);
            Console.Error.WriteLine("- 64-bit process: " + Environment.Is64BitProcess);
            
            try {
                // Just trigger a type load from the API to verify it resolves correctly
                var version = Siemens.Simatic.Simulation.Runtime.SimulationRuntimeManager.Version;
                Console.Error.WriteLine($"- PLCSIM Advanced API loaded successfully. Version: {version}");
            } catch (Exception ex) {
                Console.Error.WriteLine($"- Failed to load PLCSIM Advanced API: {ex.Message}");
            }
        }

        private static async Task RunStdioHost(CliOptions options)
        {
            var builder = Host.CreateDefaultBuilder();
            
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);
            });

            builder.ConfigureServices((context, services) =>
            {
                // Watchdog removed due to false positives
                
                services
                    .AddMcpServer(serverOptions =>
                    {
                        serverOptions.ServerInfo = new global::ModelContextProtocol.Protocol.Implementation
                        {
                            Name = "PlcSimMcpServer",
                            Title = "PLCSIM Advanced MCP Server",
                            Version = "0.1.0"
                        };
                        serverOptions.ServerInstructions = "Exposes PLCSIM Advanced API.";
                    })
                    .WithStdioServerTransport()
                    .WithTools(BuildTools());
            });

            var host = builder.Build();

            await host.RunAsync();
        }

        public static IEnumerable<Type> BuildTools()
        {
            return new[]
            {
                typeof(PlcSimServer)
            };
        }
    }
}
