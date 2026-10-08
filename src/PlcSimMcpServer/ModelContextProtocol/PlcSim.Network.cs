using System;
using System.ComponentModel;
using System.Linq;
using ModelContextProtocol.Server;
using Siemens.Simatic.Simulation.Runtime;

namespace PlcSimMcpServer
{
    public static partial class PlcSimServer
    {
        [McpServerTool(Name = "plcsim_get_pc_interfaces"), Description("Get a list of available host PC network interfaces for mapping")]
        public static string PlcSimGetPcInterfaces()
        {
            var interfaces = SimulationRuntimeManager.NetInterfaces;
            if (interfaces == null || interfaces.Length == 0) return "No PC network interfaces found.";
            
            return string.Join("\n", interfaces.Select(i => $"[{i.interfaceIndex}] {i.interfaceName} ({i.interfaceDescription})"));
        }

        [McpServerTool(Name = "plcsim_set_global_network_mode"), Description("Set the global network routing mode (e.g., TCPIPSingleAdapter, TCPIPMultipleAdapter, Softbus)")]
        public static string PlcSimSetGlobalNetworkMode(
            [Description("Network Mode (e.g. TCPIPSingleAdapter for Virtual Switch, TCPIPMultipleAdapter for Multi-Adapter)")] string mode)
        {
            if (Enum.TryParse<ENetworkMode>(mode, true, out var eMode))
            {
                SimulationRuntimeManager.NetworkMode = eMode;
                return $"Global network mode set to {eMode}.";
            }
            return $"Error: Invalid network mode '{mode}'.";
        }

        [McpServerTool(Name = "plcsim_set_instance_mapping"), Description("Map a PLC interface (e.g., IE1) to a PC network interface index")]
        public static string PlcSimSetInstanceMapping(
            [Description("Name of the instance")] string instanceName,
            [Description("PLC Interface (e.g., IE1, IE2)")] string plcInterface,
            [Description("PC Interface Index (use 0 for Virtual Switch/TCPIPSingleAdapter)")] uint pcInterfaceIndex)
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance == null) return $"Error: Instance '{instanceName}' not found.";

            if (Enum.TryParse<EPLCInterface>(plcInterface, true, out var ePlcInterface))
            {
                try 
                {
                    instance.SetNetInterfaceMapping(ePlcInterface, pcInterfaceIndex);
                    
                    if (SimulationRuntimeManager.NetworkMode == ENetworkMode.TCPIPMultipleAdapter)
                    {
                         SimulationRuntimeManager.SetNetInterfaceBindings(pcInterfaceIndex);
                    }
                    return $"Mapped {plcInterface} of '{instanceName}' to PC interface index {pcInterfaceIndex}.";
                }
                catch (Exception ex)
                {
                    return $"Error mapping interface: {ex.Message}";
                }
            }
            return $"Error: Invalid PLC Interface '{plcInterface}'.";
        }

        [McpServerTool(Name = "plcsim_set_instance_ip"), Description("Configure the IP suite for a specific port of a PLCSim instance")]
        public static string PlcSimSetInstanceIp(
            [Description("Name of the instance")] string instanceName,
            [Description("Port ID (e.g., 1 for X1)")] uint portId,
            [Description("IP Address (e.g., 192.168.0.1)")] string ipAddress,
            [Description("Subnet Mask (e.g., 255.255.255.0)")] string subnetMask = "255.255.255.0",
            [Description("Default Gateway (e.g., 0.0.0.0)")] string defaultGateway = "0.0.0.0")
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance == null) return $"Error: Instance '{instanceName}' not found.";

            try
            {
                var suite = new SIPSuite4
                {
                    IPAddress = new SIP { IPString = ipAddress },
                    SubnetMask = new SIP { IPString = subnetMask },
                    DefaultGateway = new SIP { IPString = defaultGateway }
                };
                instance.SetIPSuite(portId, suite, true);
                return $"Instance '{instanceName}' Port {portId} configured with IP {ipAddress}.";
            }
            catch (Exception ex)
            {
                return $"Error setting IP suite: {ex.Message}";
            }
        }
    }
}
