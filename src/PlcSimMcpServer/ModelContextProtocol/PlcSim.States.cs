using System;
using System.ComponentModel;
using System.Linq;
using ModelContextProtocol.Server;
using Siemens.Simatic.Simulation.Runtime;

namespace PlcSimMcpServer
{
    public static partial class PlcSimServer
    {
        [McpServerTool(Name = "plcsim_instance_control"), Description("Control the state of a PLCSim instance")]
        public static string PlcSimInstanceControl(
            [Description("Name of the instance")] string instanceName,
            [Description("Action to perform: PowerOn, PowerOff, Run, Stop, MemoryReset")] string action)
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                    return $"Failed: Instance '{instanceName}' not found.";

                var instance = SimulationRuntimeManager.CreateInterface(instanceName);

                switch (action.ToLowerInvariant())
                {
                    case "poweron":
                        if (instance.OperatingState == EOperatingState.Off)
                        {
                            instance.PowerOn();
                            return $"Power on triggered for '{instanceName}'.";
                        }
                        return $"Instance '{instanceName}' is already powered on. Current state: {instance.OperatingState}";

                    case "poweroff":
                        if (instance.OperatingState != EOperatingState.Off)
                        {
                            instance.PowerOff();
                            return $"Power off triggered for '{instanceName}'.";
                        }
                        return $"Instance '{instanceName}' is already powered off.";

                    case "run":
                        if (instance.OperatingState == EOperatingState.Stop)
                        {
                            instance.Run();
                            return $"Run triggered for '{instanceName}'.";
                        }
                        return $"Failed: PLC must be in Stop state to trigger Run. Current state: {instance.OperatingState}";

                    case "stop":
                        if (instance.OperatingState == EOperatingState.Run)
                        {
                            instance.Stop();
                            return $"Stop triggered for '{instanceName}'.";
                        }
                        return $"Failed: PLC must be in Run state to trigger Stop. Current state: {instance.OperatingState}";

                    case "memoryreset":
                    case "mres":
                        if (instance.OperatingState == EOperatingState.Stop)
                        {
                            instance.MemoryReset();
                            return $"Memory reset triggered for '{instanceName}'.";
                        }
                        return $"Failed: Memory reset requires the PLC to be in Stop state. Current state: {instance.OperatingState}";

                    default:
                        return $"Error: Unknown action '{action}'. Valid actions: PowerOn, PowerOff, Run, Stop, MemoryReset";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
