using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PlcSimMcpServer
{
    public class GhostWatchdogService : BackgroundService
    {
        private readonly ILogger<GhostWatchdogService> _logger;

        public GhostWatchdogService(ILogger<GhostWatchdogService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GhostWatchdogService started.");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var uiProcesses = Process.GetProcessesByName("S7PLCSIMV21");
                    foreach (var p in uiProcesses)
                    {
                        try
                        {
                            // If the process has no main window and has been running for more than 30 seconds
                            if (p.MainWindowHandle == IntPtr.Zero && (DateTime.Now - p.StartTime).TotalSeconds > 30)
                            {
                                _logger.LogWarning($"GhostWatchdog: Detected stuck S7PLCSIMV21.exe (PID {p.Id}). Killing ghost process...");
                                PlcSimServer.AddLog($"GhostWatchdog: Killed ghost S7PLCSIMV21.exe (PID {p.Id}) to prevent deadlock.");
                                p.Kill();

                                // Also kill any orphaned Manager instances just in case
                                var managerProcesses = Process.GetProcessesByName("Siemens.Simatic.Simulation.Runtime.Manager");
                                foreach (var mgr in managerProcesses)
                                {
                                    _logger.LogWarning($"GhostWatchdog: Killing associated Manager.exe (PID {mgr.Id}).");
                                    mgr.Kill();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"GhostWatchdog: Failed to inspect/kill process {p.Id}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"GhostWatchdog: Outer loop error: {ex.Message}");
                }

                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}
