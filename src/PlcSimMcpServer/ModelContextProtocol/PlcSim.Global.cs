using System.ComponentModel;
using ModelContextProtocol.Server;
using Siemens.Simatic.Simulation.Runtime;

namespace PlcSimMcpServer
{
    public static partial class PlcSimServer
    {
        private static readonly System.Collections.Generic.List<string> _actionLogs = new System.Collections.Generic.List<string>();

        public static void AddLog(string message)
        {
            var log = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
            lock (_actionLogs)
            {
                _actionLogs.Add(log);
                if (_actionLogs.Count > 100) _actionLogs.RemoveAt(0); // Keep last 100
            }
        }

        [McpServerTool(Name = "plcsim_get_logs"), Description("Get recent action and event logs from the PLCSIM backend")]
        public static string PlcSimGetLogs()
        {
            lock (_actionLogs)
            {
                if (_actionLogs.Count == 0) return "No recent logs.";
                return string.Join("\n", _actionLogs);
            }
        }

        [McpServerTool(Name = "plcsim_connect"), Description("Connect to a PLCSim simulation instance")]
        public static string PlcSimConnect()
        {
            AddLog($"Connected to PLCSIM Advanced API. Version: {SimulationRuntimeManager.Version}");
            return $"Connected to PLCSIM Advanced API. Version: {SimulationRuntimeManager.Version}";
        }

        [McpServerTool(Name = "plcsim_cleanup_memory"), Description("Force the .NET Garbage Collector to run. Use this to release C++ COM wrappers if Manager.exe hangs.")]
        public static string PlcSimCleanupMemory()
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            AddLog("Triggered explicit Garbage Collection to drop API wrappers.");
            return "Memory released. Manager.exe should now be able to close naturally.";
        }

        [McpServerTool(Name = "plcsim_host_control"), Description("Control the PLCSIM application host. Actions: 'StartUI' (launches graphical window), 'StartBackground' (starts headless manager), 'Stop' (kills all PLCSIM processes to fix ghost state).")]
        public static string PlcSimHostControl(
            [Description("Action: StartUI, StartBackground, Stop")] string action)
        {
            try
            {
                if (action.Equals("StartUI", System.StringComparison.OrdinalIgnoreCase) || action.Equals("Start", System.StringComparison.OrdinalIgnoreCase))
                {
                    var settings = ApiResolver.LoadSettings();
                    
                    // Launch graphical UI
                    string shortcut = settings.UiShortcutPath;
                    if (!string.IsNullOrEmpty(shortcut) && System.IO.File.Exists(shortcut))
                    {
                        var startInfo = new System.Diagnostics.ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{shortcut}\"", UseShellExecute = true };
                        System.Diagnostics.Process.Start(startInfo);
                        return $"Started PLCSIM Graphical UI via Explorer Shortcut: {shortcut}";
                    }
                    
                    var advPath = settings.UiExecutablePath;
                    if (!string.IsNullOrEmpty(advPath) && System.IO.File.Exists(advPath))
                    {
                        var startInfo = new System.Diagnostics.ProcessStartInfo { FileName = advPath, WorkingDirectory = System.IO.Path.GetDirectoryName(advPath) };
                        System.Diagnostics.Process.Start(startInfo);
                        return $"Started PLCSIM Advanced Graphical UI from: {advPath}";
                    }
                    
                    return "Failed to find PLCSIM Graphical UI executable or shortcut.";
                }
                else if (action.Equals("StartBackground", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (!SimulationRuntimeManager.IsInitialized)
                    {
                        var dummy = SimulationRuntimeManager.RegisteredInstanceInfo;
                    }
                    return "PLCSIM Background Manager started (Headless Mode).";
                }
                else if (action.Equals("Stop", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Force GC to drop API connections first
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                    System.GC.Collect();
                    
                    // Kill all orphaned processes
                    string[] targets = { "Siemens.Simatic.PlcSim.Advanced.UserInterface", "Siemens.Simatic.Simulation.Runtime.Manager", "S7PLCSIMV21", "Siemens.Simatic.Srm.Rdp.Utilities.Services" };
                    int killedCount = 0;
                    foreach (var target in targets)
                    {
                        foreach (var process in System.Diagnostics.Process.GetProcessesByName(target))
                        {
                            try { process.Kill(); killedCount++; } catch { }
                        }
                    }
                    return $"Stop action completed. Killed {killedCount} PLCSIM host processes (resolved Ghost State).";
                }
                
                return $"Unknown action: {action}. Use StartUI, StartBackground, or Stop.";
            }
            catch (System.Exception ex)
            {
                return $"Error in host control: {ex.Message}";
            }
        }
    }
}
