using System;
using System.ComponentModel;
using System.Linq;
using ModelContextProtocol.Server;
using Siemens.Simatic.Simulation.Runtime;

namespace PlcSimMcpServer
{
    public static partial class PlcSimServer
    {
        [McpServerTool(Name = "plcsim_create_instance"), Description("Create a new PLCSim virtual controller instance")]
        public static string PlcSimCreateInstance(
            [Description("Name of the instance")] string instanceName,
            [Description("CPU type (e.g. CPU1500_Unspecified)")] string cpuType = "CPU1500_Unspecified")
        {
            try
            {
                if (!Enum.TryParse<ECPUType>(cpuType, out var eCpuType))
                {
                    return $"Failed: Invalid CPU type '{cpuType}'.";
                }
                
                if (SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                {
                    return $"Failed: Instance '{instanceName}' already exists.";
                }

                var instance = SimulationRuntimeManager.RegisterInstance(eCpuType, instanceName);
                return $"Instance '{instanceName}' created successfully.";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_delete_instance"), Description("Delete an existing PLCSim instance")]
        public static string PlcSimDeleteInstance([Description("Name of the instance")] string instanceName)
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                {
                    return $"Failed: Instance '{instanceName}' not found.";
                }

                var instance = SimulationRuntimeManager.CreateInterface(instanceName);
                if (instance.OperatingState != EOperatingState.Off)
                {
                    instance.PowerOff();
                }
                instance.UnregisterInstance();
                return $"Instance '{instanceName}' deleted.";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_get_instance_config"), Description("Get configuration of a PLCSim instance")]
        public static string PlcSimGetInstanceConfig() { return "Not implemented"; }

        [McpServerTool(Name = "plcsim_get_instance_state"), Description("Get the current state of a PLCSim instance")]
        public static string PlcSimGetInstanceState([Description("Name of the instance")] string instanceName)
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance != null)
            {
                return $"State of '{instanceName}': {instance.OperatingState}";
            }
            return $"Instance '{instanceName}' not found.";
        }

        [McpServerTool(Name = "plcsim_list_instances"), Description("List all available PLCSim instances")]
        public static string PlcSimListInstances()
        {
            var instances = SimulationRuntimeManager.RegisteredInstanceInfo;
            if (instances == null || instances.Length == 0) return "No instances found.";
            return string.Join("\n", instances.Select(i => $"- {i.Name}"));
        }

        [McpServerTool(Name = "plcsim_set_instance_config"), Description("Configure settings of a PLCSim instance")]
        public static string PlcSimSetInstanceConfig(
            [Description("Name of the instance")] string instanceName,
            [Description("IP Address")] string ipAddress = "",
            [Description("Subnet Mask")] string subnetMask = "255.255.255.0",
            [Description("Default Gateway")] string defaultGateway = "0.0.0.0")
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance != null)
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    var suite = new SIPSuite4
                    {
                        IPAddress = new SIP { IPString = ipAddress },
                        SubnetMask = new SIP { IPString = subnetMask },
                        DefaultGateway = new SIP { IPString = defaultGateway }
                    };
                    instance.SetIPSuite(1u, suite, false);
                }
                return $"Instance '{instanceName}' configured with IP {ipAddress}.";
            }
            return $"Instance '{instanceName}' not found.";
        }
    }
}
