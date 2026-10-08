using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

        [McpServerTool(Name = "plcsim_get_instances"), Description("Get a detailed list of all PLCSim instances including their states, mappings, and IP addresses")]
        public static string PlcSimGetInstances()
        {
            var instances = SimulationRuntimeManager.RegisteredInstanceInfo;
            if (instances == null || instances.Length == 0) return "No instances found.";
            
            var sb = new StringBuilder();
            sb.AppendLine("Registered PLCSIM Instances:");
            
            foreach (var info in instances)
            {
                sb.AppendLine($"\n- Name: {info.Name}");
                
                try 
                {
                    var instance = SimulationRuntimeManager.CreateInterface(info.Name);
                    sb.AppendLine($"  CPU Type: {instance.CPUType}");
                    sb.AppendLine($"  State: {instance.OperatingState}");
                    
                    try {
                        sb.AppendLine($"  IP: {instance.ControllerIP}");
                    } catch { }
                    
                    try {
                        var ipSuite = instance.ControllerIPSuite4; 
                        if (ipSuite != null && ipSuite.Length > 0)
                        {
                            sb.AppendLine($"  IPSuite (Default): IP={ipSuite[0].IPAddress.IPString}, Mask={ipSuite[0].SubnetMask.IPString}, GW={ipSuite[0].DefaultGateway.IPString}");
                        }
                    } catch { }

                    try {
                        var mapping = instance.GetNetInterfaceMapping(EPLCInterface.IE1);
                        sb.AppendLine($"  Mapping (IE1): PC Interface Index = {mapping}");
                    } catch { }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"  [Error reading details: {ex.Message}]");
                }
            }
            return sb.ToString().TrimEnd();
        }
    }
}
