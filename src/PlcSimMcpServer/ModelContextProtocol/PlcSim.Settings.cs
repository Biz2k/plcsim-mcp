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
            sb.AppendLine($"UiShortcutPath: {settings.UiShortcutPath}");
            sb.AppendLine($"UiExecutablePath: {settings.UiExecutablePath}");
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

        [McpServerTool(Name = "plcsim_set_mcp_settings"), Description("Update the MCP server settings (e.g. set ApiDllPath or UI paths). Leave optional fields empty to keep their current values.")]
        public static string PlcSimSetMcpSettings(
            [Description("Absolute path to Siemens.Simatic.Simulation.Runtime.Api.x64.dll to use")] string apiDllPath = null,
            [Description("Absolute path to S7-PLCSIM V21.lnk shortcut")] string uiShortcutPath = null,
            [Description("Absolute path to Siemens.Simatic.PlcSim.Advanced.UserInterface.exe")] string uiExecutablePath = null)
        {
            var settings = ApiResolver.LoadSettings();
            
            bool changed = false;
            if (!string.IsNullOrWhiteSpace(apiDllPath))
            {
                if (!File.Exists(apiDllPath)) return $"Error: The specified API file does not exist: {apiDllPath}";
                settings.ApiDllPath = apiDllPath;
                changed = true;
            }
            if (!string.IsNullOrWhiteSpace(uiShortcutPath))
            {
                settings.UiShortcutPath = uiShortcutPath;
                changed = true;
            }
            if (!string.IsNullOrWhiteSpace(uiExecutablePath))
            {
                settings.UiExecutablePath = uiExecutablePath;
                changed = true;
            }

            if (!changed) return "No settings were changed. Provide at least one argument.";

            ApiResolver.SaveSettings(settings);

            string ver = "Unknown";
            try {
                ver = FileVersionInfo.GetVersionInfo(apiDllPath).FileVersion;
            } catch {}

            return $"Settings updated successfully.\nNew ApiDllPath: {settings.ApiDllPath}\nNew UiShortcutPath: {settings.UiShortcutPath}\nNew UiExecutablePath: {settings.UiExecutablePath}\nVersion: {ver}\n\nNote: API changes require MCP restart.";
        }
    }
}
