using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ModelContextProtocol.Server;

namespace PlcSimMcpServer
{
    public static partial class PlcSimServer
    {
        [McpServerTool(Name = "plcsim_get_mcp_settings"), Description("Get current MCP server settings and list all installed PLCSIM Advanced API versions found on the host machine.")]
        public static string PlcSimGetMcpSettings()
        {
            var settings = ApiResolver.LoadSettings();
            var sb = new StringBuilder();
            
            sb.AppendLine("=== Current MCP Settings ===");
            sb.AppendLine($"ApiDllPath: {settings.ApiDllPath ?? "Not configured (will auto-detect)"}");
            if (!string.IsNullOrEmpty(settings.ApiDllPath) && File.Exists(settings.ApiDllPath))
            {
                try {
                    var info = FileVersionInfo.GetVersionInfo(settings.ApiDllPath);
                    sb.AppendLine($"Current API Version: {info.FileVersion}");
                } catch {
                    sb.AppendLine("Current API Version: Unknown (could not read file version)");
                }
            }
            else if (!string.IsNullOrEmpty(settings.ApiDllPath))
            {
                sb.AppendLine("Current API Version: INVALID FILE PATH");
            }

            sb.AppendLine();
            sb.AppendLine("=== Found PLCSIM API Versions on Host ===");
            
            var availablePaths = ApiResolver.GetAvailableApiVersions();
            if (availablePaths.Count == 0)
            {
                sb.AppendLine("No PLCSIM Advanced API installations found in standard paths.");
            }
            else
            {
                for (int i = 0; i < availablePaths.Count; i++)
                {
                    string path = availablePaths[i];
                    string ver = "Unknown";
                    try {
                        ver = FileVersionInfo.GetVersionInfo(path).FileVersion;
                    } catch {}
                    sb.AppendLine($"[{i}] Version: {ver}");
                    sb.AppendLine($"    Path: {path}");
                }
            }

            return sb.ToString();
        }

        [McpServerTool(Name = "plcsim_set_mcp_settings"), Description("Update the MCP server settings (e.g. set a specific ApiDllPath)")]
        public static string PlcSimSetMcpSettings(
            [Description("Absolute path to Siemens.Simatic.Simulation.Runtime.Api.x64.dll to use")] string apiDllPath)
        {
            if (string.IsNullOrWhiteSpace(apiDllPath))
            {
                return "Error: apiDllPath cannot be empty.";
            }

            if (!File.Exists(apiDllPath))
            {
                return $"Error: The specified file does not exist: {apiDllPath}";
            }
            
            var settings = ApiResolver.LoadSettings();
            settings.ApiDllPath = apiDllPath;
            ApiResolver.SaveSettings(settings);

            string ver = "Unknown";
            try {
                ver = FileVersionInfo.GetVersionInfo(apiDllPath).FileVersion;
            } catch {}

            return $"Settings updated successfully.\nNew ApiDllPath: {apiDllPath}\nVersion: {ver}\n\nNote: If the server is already running and has loaded the PLCSIM API, you must restart the MCP server for this change to take effect.";
        }
    }
}
