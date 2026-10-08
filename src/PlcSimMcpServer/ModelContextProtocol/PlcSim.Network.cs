using System;
using System.ComponentModel;
using ModelContextProtocol.Server;
using Siemens.Simatic.Simulation.Runtime;

namespace PlcSimMcpServer
{
    public static partial class PlcSimServer
    {
        [McpServerTool(Name = "plcsim_get_network"), Description("Get network settings of the simulation")]
        public static string PlcSimGetNetwork() { return "Not implemented"; }

        [McpServerTool(Name = "plcsim_set_network"), Description("Configure network settings for the simulation")]
        public static string PlcSimSetNetwork(
            [Description("Name of the instance")] string instanceName,
            [Description("Network Mode (e.g. TCPIPMultipleAdapter)")] string mode = "TCPIPMultipleAdapter",
            [Description("Interface mapping (e.g. IE1)")] string mapping = "IE1")
        {
            if (Enum.TryParse<ENetworkMode>(mode, out var eMode))
            {
                SimulationRuntimeManager.NetworkMode = eMode;
                
                var instance = SimulationRuntimeManager.CreateInterface(instanceName);
                if (instance != null)
                {
                    if (Enum.TryParse<EPLCInterface>(mapping, out var eMapping))
                    {
                        instance.SetNetInterfaceMapping(eMapping, 0); 
                        SimulationRuntimeManager.SetNetInterfaceBindings(0);
                        return $"Network set to {mode} and mapped {mapping} for '{instanceName}'.";
                    }
                }
                return $"Instance '{instanceName}' not found or mapping invalid.";
            }
            return $"Invalid network mode '{mode}'.";
        }
    }
}
