using System;
using System.ComponentModel;
using System.Linq;
using ModelContextProtocol.Server;
using Siemens.Simatic.Simulation.Runtime;

namespace PlcSimMcpServer
{
    public static partial class PlcSimServer
    {
        [McpServerTool(Name = "plcsim_get_process_events"), Description("DANGER: Calling this on a PLC without a downloaded Hardware Configuration will crash the entire MCP server! Get a list of all configured process events (Hardware Interrupts) in the PLC.")]
        public static string PlcSimGetProcessEvents(
            [Description("Name of the instance")] string instanceName)
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                    return $"Failed: Instance '{instanceName}' does not exist.";

                using var instance = SimulationRuntimeManager.CreateInterface(instanceName);

                var events = instance.GetConfiguredProcessEvents();
                
                if (events == null || events.Length == 0)
                {
                    return "No process events configured in this instance.";
                }

                var lines = events.Select(e => $"- Name: '{e.Name}', HardwareId: {e.HardwareIdentifier}, Channel: {e.Channel}, Type: {e.ProcessEventType}");
                return string.Join("\n", lines);
            }
            catch (Exception ex)
            {
                return $"Error getting process events: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_trigger_event"), Description("DANGER: Passing an invalid hardwareId will crash the entire MCP server! Trigger a hardware event (Process Event, Pull/Plug, or Alarm) in the PLC.")]
        public static string PlcSimTriggerEvent(
            [Description("Name of the instance")] string instanceName,
            [Description("Type of the event: 'Process', 'PullPlug', or 'Alarm'")] string eventType,
            [Description("Hardware Identifier (System Constant) of the module")] ushort hardwareId,
            [Description("Optional: Channel number (for ProcessEvent or Alarm)")] ushort channel = 0,
            [Description("Optional: Process Event Type (e.g., RisingEdge, FallingEdge). Default is RisingEdge")] string processType = "RisingEdge",
            [Description("Optional: Pull/Plug Event Type (e.g., Pull, Plug, PlugWrongModule). Default is Pull")] string pullPlugType = "Pull")
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                    return $"Failed: Instance '{instanceName}' does not exist.";

                using var instance = SimulationRuntimeManager.CreateInterface(instanceName);
                ushort seqNum = 0;

                switch (eventType.ToLowerInvariant())
                {
                    case "process":
                        if (!Enum.TryParse<EProcessEventType>(processType, true, out var eProcessType))
                        {
                            return $"Invalid Process Event Type. Valid options: {string.Join(", ", Enum.GetNames(typeof(EProcessEventType)))}";
                        }
                        instance.ProcessEvent(hardwareId, channel, eProcessType, out seqNum);
                        break;
                    case "pullplug":
                        if (!Enum.TryParse<EPullOrPlugEventType>(pullPlugType, true, out var ePullPlugType))
                        {
                            return $"Invalid Pull/Plug Event Type. Valid options: {string.Join(", ", Enum.GetNames(typeof(EPullOrPlugEventType)))}";
                        }
                        instance.PullOrPlugEvent(hardwareId, ePullPlugType, out seqNum);
                        break;
                    case "alarm":
                        // Minimal implementation of alarm for now: No specific diag events array
                        // ModuleState: 0 means no error, typically 1 or bits mean error states
                        instance.AlarmNotification(hardwareId, 1, 0, new SDiagExtChannelDescription[0], out seqNum);
                        break;
                    default:
                        return "Invalid eventType. Use 'Process', 'PullPlug', or 'Alarm'.";
                }

                return $"Successfully triggered {eventType} event on HardwareId {hardwareId}. Sequence Number: {seqNum}";
            }
            catch (Exception ex)
            {
                return $"Error triggering event: {ex.Message}";
            }
        }
    }
}
