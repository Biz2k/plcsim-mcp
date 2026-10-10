using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using ModelContextProtocol.Server;

namespace PlcSimMcpServer
{
    public static partial class PlcSimServer
    {
        [McpServerTool(Name = "plcsim_host_repair"), Description("DANGER: Forcefully kills S7PLCSIMV21 and Runtime.Manager processes to clear Ghost State. Use only if the simulator is completely frozen or API is unresponsive.")]
        public static string PlcSimHostRepair()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                var managerProcesses = Process.GetProcessesByName("Siemens.Simatic.Simulation.Runtime.Manager");
                foreach (var p in managerProcesses)
                {
                    try
                    {
                        p.Kill();
                        sb.AppendLine($"Killed Manager process (ID: {p.Id})");
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"Failed to kill Manager (ID: {p.Id}): {ex.Message}");
                    }
                }

                var plcProcesses = Process.GetProcessesByName("S7PLCSIMV21");
                foreach (var p in plcProcesses)
                {
                    try
                    {
                        p.Kill();
                        sb.AppendLine($"Killed S7PLCSIMV21 process (ID: {p.Id})");
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"Failed to kill S7PLCSIMV21 (ID: {p.Id}): {ex.Message}");
                    }
                }

                if (sb.Length == 0)
                {
                    sb.AppendLine("No ghost processes found.");
                }
                else
                {
                    sb.AppendLine("Successfully cleared PLCSIM Advanced processes.");
                    sb.AppendLine("WARNING: Any existing COM references in this MCP Server are now invalid. If the MCP server starts crashing, please restart it.");
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Critical error during host repair: {ex.Message}";
            }
        }
    }
}
