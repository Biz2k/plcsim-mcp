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
        [McpServerTool(Name = "plcsim_get_supported_cpus"), Description("Get a categorized list of all supported CPU types for plcsim_create_instance")]
        public static string PlcSimGetSupportedCpus()
        {
            return @"Поддерживаемые типы процессоров (ECPUType) для plcsim_create_instance:

S7-1500 (Обычные, F-безопасные, Компактные и Технологические)
- Универсальный: CPU1500_Unspecified (самый частый вариант по умолчанию)
- Стандартные: CPU1511, CPU1513, CPU1515, CPU1516, CPU1517, CPU1518
- F (Fail-safe): CPU1511F, CPU1513F, CPU1515F, CPU1516F, CPU1517F, CPU1518F
- C (Compact): CPU1511C, CPU1512C
- T / TF (Technology): CPU1511T, CPU1515T, CPU1516T, CPU1517T, CPU1518T, CPU1511TF, CPU1515TF, CPU1516TF, CPU1517TF, CPU1518TF
- ODK / MFP: CPU1518ODK, CPU1518FODK, CPU1518MFP, CPU1518FMFP

Станции распределенной периферии (ET200SP / ET200PRO)
- Универсальные: ET200SP_Unspecified, ET200PRO_Unspecified
- ET200SP: CPU1510SP, CPU1512SP, CPU1514SP, CPU1510SPF, CPU1512SPF, CPU1514SPF, CPU1514SPT, CPU1514SPTF
- ET200PRO: CPU1513PRO, CPU1516PRO, CPU1513PROF, CPU1516PROF

Резервированные / High-Availability (R/H)
- Универсальный: CPU1500_RH_Unspecified
- Модели: CPU1513R, CPU1515R, CPU1517H, CPU1518HF

Программные контроллеры (Software Controllers / Open Controller)
- Универсальный: CPU1500_SW_OC_Unspecified
- Модели: CPU1505SP, CPU1507S, CPU1508S, CPU1505SPF, CPU1507SF, CPU1508SF, CPU1505SPT, CPU1508ST, CPU1505SPTF, CPU1508STF

SINUMERIK (ЧПУ)
- Универсальный: CPU1500_SINUMERIK_Unspecified
- Модели: SINUMERIK_MCU1720, SINUMERIK_NCU1740, SINUMERIK_NCU1750, SINUMERIK_NCU1760, SINUMERIK_PPU1740

S7-1200 (Только для версий PLCSIM V21+)
- Универсальный: CPU1200G2_Unspecified
- Модели: CPU1212C_DCDCDC, CPU1212C_DCDCRLY, CPU1212C_ACDCRLY, CPU1214C_DCDCDC, CPU1214C_DCDCRLY, CPU1214C_ACDCRLY
- F (Fail-safe): CPU1212FC_DCDCDC, CPU1212FC_DCDCRLY, CPU1214FC_DCDCDC, CPU1214FC_DCDCRLY
- Прочие: CPU1216, CPU1217
- Дополнительно (Drive Controller): CPU1504DTF, CPU1507DTF, CPU1514PA";
        }

        [McpServerTool(Name = "plcsim_create_instance"), Description("Create a new PLCSim virtual controller instance")]
        public static string PlcSimCreateInstance(
            [Description("Name of the instance")] string instanceName,
            [Description("CPU type. Call plcsim_get_supported_cpus for a full list.")] string cpuType = "CPU1500_Unspecified")
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

                // 4. Interface mapping not needed for Softbus / Default
                logAction($"[SUCCESS] Network mode is set to {eMode}.");

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
