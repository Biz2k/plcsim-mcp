using System;
using System.ComponentModel;
using System.Linq;
using ModelContextProtocol.Server;
using Siemens.Simatic.Simulation.Runtime;

namespace PlcSimMcpServer
{
    public static partial class PlcSimServer
    {
        [McpServerTool(Name = "plcsim_memory_reset"), Description("Reset the memory of the simulated PLC")]
        public static string PlcSimMemoryReset([Description("Name of the instance")] string instanceName)
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                    return $"Failed: Instance '{instanceName}' not found.";

                var instance = SimulationRuntimeManager.CreateInterface(instanceName);
                if (instance.OperatingState == EOperatingState.Stop)
                {
                    instance.MemoryReset();
                    return $"Memory reset triggered for '{instanceName}'.";
                }
                return $"Failed: Memory reset requires the PLC to be in Stop state. Current state: {instance.OperatingState}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_power_off"), Description("Power off the simulated PLC")]
        public static string PlcSimPowerOff([Description("Name of the instance")] string instanceName)
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                    return $"Failed: Instance '{instanceName}' not found.";

                var instance = SimulationRuntimeManager.CreateInterface(instanceName);
                if (instance.OperatingState != EOperatingState.Off)
                {
                    instance.PowerOff();
                    return $"Power off triggered for '{instanceName}'.";
                }
                return $"Instance '{instanceName}' is already powered off.";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_power_on"), Description("Power on the simulated PLC")]
        public static string PlcSimPowerOn([Description("Name of the instance")] string instanceName)
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                    return $"Failed: Instance '{instanceName}' not found.";

                var instance = SimulationRuntimeManager.CreateInterface(instanceName);
                if (instance.OperatingState == EOperatingState.Off)
                {
                    instance.PowerOn();
                    return $"Power on triggered for '{instanceName}'.";
                }
                return $"Instance '{instanceName}' is already powered on. Current state: {instance.OperatingState}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_run"), Description("Set the simulated PLC to RUN mode")]
        public static string PlcSimRun([Description("Name of the instance")] string instanceName)
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                    return $"Failed: Instance '{instanceName}' not found.";

                var instance = SimulationRuntimeManager.CreateInterface(instanceName);
                if (instance.OperatingState == EOperatingState.Stop)
                {
                    instance.Run();
                    return $"Run triggered for '{instanceName}'.";
                }
                return $"Failed: PLC must be in Stop state to trigger Run. Current state: {instance.OperatingState}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_stop"), Description("Set the simulated PLC to STOP mode")]
        public static string PlcSimStop([Description("Name of the instance")] string instanceName)
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                    return $"Failed: Instance '{instanceName}' not found.";

                var instance = SimulationRuntimeManager.CreateInterface(instanceName);
                if (instance.OperatingState == EOperatingState.Run)
                {
                    instance.Stop();
                    return $"Stop triggered for '{instanceName}'.";
                }
                return $"Failed: PLC must be in Run state to trigger Stop. Current state: {instance.OperatingState}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
