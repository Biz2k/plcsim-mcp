using System;
using System.ComponentModel;
using System.Linq;
using ModelContextProtocol.Server;
using Siemens.Simatic.Simulation.Runtime;

namespace PlcSimMcpServer
{
    public static partial class PlcSimServer
    {
        [McpServerTool(Name = "plcsim_set_scale_factor"), Description("Scale the virtual time of the PLC. 1.0 is real-time, 2.0 is 2x faster, 0.5 is half-speed.")]
        public static string PlcSimSetScaleFactor(
            [Description("Name of the instance")] string instanceName,
            [Description("Scale factor (e.g. 1.0, 2.0, 0.5)")] double scaleFactor)
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                    return $"Failed: Instance '{instanceName}' does not exist.";

                var instance = SimulationRuntimeManager.CreateInterface(instanceName);
                if (scaleFactor <= 0)
                {
                    return "Error: Scale factor must be greater than 0.";
                }

                instance.ScaleFactor = scaleFactor;
                return $"Virtual time scale factor set to {scaleFactor}x for instance '{instanceName}'.";
            }
            catch (Exception ex)
            {
                return $"Error setting scale factor: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_get_scale_factor"), Description("Get the current virtual time scale factor of the PLC.")]
        public static string PlcSimGetScaleFactor([Description("Name of the instance")] string instanceName)
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                    return $"Failed: Instance '{instanceName}' does not exist.";

                var instance = SimulationRuntimeManager.CreateInterface(instanceName);
                return $"Current ScaleFactor for '{instanceName}': {instance.ScaleFactor}x";
            }
            catch (Exception ex)
            {
                return $"Error getting scale factor: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_set_system_time"), Description("Set the internal System Time of the virtual PLC.")]
        public static string PlcSimSetSystemTime(
            [Description("Name of the instance")] string instanceName,
            [Description("DateTime string (e.g. '2023-10-15T12:00:00Z')")] string dateTimeString)
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                    return $"Failed: Instance '{instanceName}' does not exist.";

                if (!DateTime.TryParse(dateTimeString, out DateTime dt))
                {
                    return $"Error: Invalid DateTime format. Use ISO 8601 (e.g. '2023-10-15T12:00:00Z').";
                }

                var instance = SimulationRuntimeManager.CreateInterface(instanceName);
                instance.SystemTime = dt;
                return $"System time successfully set to {dt:O} for instance '{instanceName}'.";
            }
            catch (Exception ex)
            {
                return $"Error setting system time: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_get_system_time"), Description("Get the current internal System Time of the virtual PLC.")]
        public static string PlcSimGetSystemTime([Description("Name of the instance")] string instanceName)
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                    return $"Failed: Instance '{instanceName}' does not exist.";

                var instance = SimulationRuntimeManager.CreateInterface(instanceName);
                return $"System time for '{instanceName}': {instance.SystemTime:O}";
            }
            catch (Exception ex)
            {
                return $"Error getting system time: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_set_operating_mode"), Description("Set the operating mode of the virtual PLC (Default or SingleStep). SingleStep is required for manual step execution.")]
        public static string PlcSimSetOperatingMode(
            [Description("Name of the instance")] string instanceName,
            [Description("Operating Mode ('Default' or 'SingleStep')")] string operatingMode)
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                    return $"Failed: Instance '{instanceName}' does not exist.";

                if (operatingMode.Equals("SingleStep", StringComparison.OrdinalIgnoreCase))
                {
                    operatingMode = "SingleStep_C";
                }

                if (!Enum.TryParse<EOperatingMode>(operatingMode, true, out var eMode))
                    return $"Error: Invalid operating mode '{operatingMode}'. Valid modes: {string.Join(", ", Enum.GetNames(typeof(EOperatingMode)))}";

                using var instance = SimulationRuntimeManager.CreateInterface(instanceName);
                instance.OperatingMode = eMode;
                return $"Operating mode successfully set to {eMode} for instance '{instanceName}'.";
            }
            catch (Exception ex)
            {
                return $"Error setting operating mode: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_get_operating_mode"), Description("Get the current operating mode of the virtual PLC.")]
        public static string PlcSimGetOperatingMode([Description("Name of the instance")] string instanceName)
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                    return $"Failed: Instance '{instanceName}' does not exist.";

                using var instance = SimulationRuntimeManager.CreateInterface(instanceName);
                return $"Operating mode for '{instanceName}': {instance.OperatingMode}";
            }
            catch (Exception ex)
            {
                return $"Error getting operating mode: {ex.Message}";
            }
        }
    }
}
