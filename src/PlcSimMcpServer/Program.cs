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
            Console.WriteLine("Diagnostics for PlcSimMcpServer:");
            Console.WriteLine("- Working directory: " + Environment.CurrentDirectory);
            Console.WriteLine("- CLR version: " + Environment.Version);
            Console.WriteLine("- 64-bit process: " + Environment.Is64BitProcess);
            
            try {
                var asm = Assembly.LoadFrom(@"Libs\Siemens.Simatic.Simulation.Runtime.Api.x64.dll");
                Console.WriteLine($"- PLCSIM Advanced API loaded successfully from: {asm.Location}");
            } catch (Exception ex) {
                Console.WriteLine($"- Failed to load PLCSIM Advanced API: {ex.Message}");
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
