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

        [McpServerTool(Name = "plcsim_set_instance"), Description("Configure the network properties of a specific PLCSim instance")]
        public static string PlcSimSetInstance(
            [Description("Name of the instance")] string instanceName,
            [Description("PLC Interface to map (e.g., IE1, IE2) (Optional)")] string plcInterface = null,
            [Description("PC Interface Index to map to (use 0 for Virtual Switch/TCPIPSingleAdapter) (Optional)")] uint? pcInterfaceIndex = null,
            [Description("Port ID to set IP for (e.g., 1 for X1) (Optional)")] uint? portId = null,
            [Description("IP Address (e.g., 192.168.0.1) (Optional)")] string ipAddress = null,
            [Description("Subnet Mask (e.g., 255.255.255.0) (Optional)")] string subnetMask = null,
            [Description("Default Gateway (e.g., 0.0.0.0) (Optional)")] string defaultGateway = null)
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance == null) return $"Error: Instance '{instanceName}' not found.";

            string result = "";

            if (!string.IsNullOrEmpty(plcInterface) && pcInterfaceIndex.HasValue)
            {
                if (Enum.TryParse<EPLCInterface>(plcInterface, true, out var ePlcInterface))
                {
                    try 
                    {
                        instance.SetNetInterfaceMapping(ePlcInterface, pcInterfaceIndex.Value);
                        if (SimulationRuntimeManager.NetworkMode == ENetworkMode.TCPIPMultipleAdapter)
                        {
                             SimulationRuntimeManager.SetNetInterfaceBindings(pcInterfaceIndex.Value);
                        }
                        result += $"Mapped {plcInterface} to PC interface {pcInterfaceIndex.Value}.\n";
                    }
                    catch (Exception ex)
                    {
                        result += $"Error mapping interface: {ex.Message}\n";
                    }
                }
                else
                {
                    result += $"Error: Invalid PLC Interface '{plcInterface}'.\n";
                }
            }

            if (portId.HasValue && !string.IsNullOrEmpty(ipAddress))
            {
                try
                {
                    var suite = new SIPSuite4
                    {
                        IPAddress = new SIP { IPString = ipAddress },
                        SubnetMask = new SIP { IPString = string.IsNullOrEmpty(subnetMask) ? "255.255.255.0" : subnetMask },
                        DefaultGateway = new SIP { IPString = string.IsNullOrEmpty(defaultGateway) ? "0.0.0.0" : defaultGateway }
                    };
                    instance.SetIPSuite(portId.Value, suite, true);
                    result += $"Configured Port {portId.Value} with IP {ipAddress}.\n";
                }
                catch (Exception ex)
                {
                    result += $"Error setting IP suite: {ex.Message}\n";
                }
            }

            if (string.IsNullOrEmpty(result))
            {
                return "No configuration options were provided.";
            }

            return result.Trim();
        }
    }
}
