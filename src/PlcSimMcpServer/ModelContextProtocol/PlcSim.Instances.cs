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
        [McpServerTool(Name = "plcsim_deploy_instance"), Description("Deploy a fully configured instance in one step (Sets network mode, creates, maps, powers on, and sets IP)")]
        public static string PlcSimDeployInstance(
            [Description("Name of the instance")] string instanceName,
            [Description("Network Mode (e.g. TCPIPSingleAdapter)")] string networkMode = "TCPIPSingleAdapter",
            [Description("CPU type (e.g. CPU1500_Unspecified)")] string cpuType = "CPU1500_Unspecified",
            [Description("IP Address (e.g., 192.168.0.1)")] string ipAddress = null,
            [Description("Subnet Mask (e.g., 255.255.255.0)")] string subnetMask = "255.255.255.0",
            [Description("Default Gateway (e.g., 0.0.0.0)")] string defaultGateway = "0.0.0.0")
        {
            var log = new StringBuilder();
            Action<string> logAction = (msg) => { log.AppendLine(msg); AddLog(msg); };
            logAction($"--- Deploying Instance '{instanceName}' ---");

            try
            {
                // 1. Set Global Network Mode
                if (Enum.TryParse<ENetworkMode>(networkMode, true, out var eMode))
                {
                    SimulationRuntimeManager.NetworkMode = eMode;
                    logAction($"[SUCCESS] Global network mode set to {eMode}.");
                }
                else
                {
                    logAction($"[ERROR] Invalid network mode '{networkMode}'.");
                    return log.ToString();
                }

                // 2. Clean up old instance
                if (SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                {
                    var oldInst = SimulationRuntimeManager.CreateInterface(instanceName);
                    if (oldInst.OperatingState != EOperatingState.Off) oldInst.PowerOff();
                    oldInst.UnregisterInstance();
                    logAction($"[SUCCESS] Old instance '{instanceName}' deleted.");
                }

                // 3. Create Instance
                if (!Enum.TryParse<ECPUType>(cpuType, out var eCpuType)) 
                {
                    logAction($"[ERROR] Invalid CPU type '{cpuType}'.");
                    return log.ToString();
                }
                var instance = SimulationRuntimeManager.RegisterInstance(eCpuType, instanceName);
                logAction($"[SUCCESS] Instance '{instanceName}' created.");

                // 4. Map Interface (Virtual Switch = 0)
                uint pcInterfaceIndex = 0;
                instance.SetNetInterfaceMapping(EPLCInterface.IE1, pcInterfaceIndex);
                logAction($"[SUCCESS] Mapped IE1 to PC interface {pcInterfaceIndex}.");

                // 5. Power ON
                instance.PowerOn();
                logAction($"[SUCCESS] Power On triggered for '{instanceName}'.");
                System.Threading.Thread.Sleep(2000); // Give it a moment to boot

                // 6. Set IP Address
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    var suite = new SIPSuite4
                    {
                        IPAddress = new SIP { IPString = ipAddress },
                        SubnetMask = new SIP { IPString = subnetMask },
                        DefaultGateway = new SIP { IPString = defaultGateway }
                    };
                    instance.SetIPSuite(0, suite, true); // Port 0 for X1
                    string gwLog = defaultGateway == "0.0.0.0" ? "0.0.0.0 (will be replaced by IP address by PLCSIM)" : defaultGateway;
                    logAction($"[SUCCESS] Configured X1 (Port 0) with IP {ipAddress}, Mask {subnetMask}, GW {gwLog}.");
                }

                logAction($"\nDeployment complete. TIA Portal can now discover '{instanceName}'.");
                return log.ToString();
            }
            catch (Exception ex)
            {
                logAction($"\n[FATAL ERROR] Deployment failed: {ex.Message}");
                return log.ToString();
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
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            return sb.ToString().TrimEnd();
        }
    }
}
