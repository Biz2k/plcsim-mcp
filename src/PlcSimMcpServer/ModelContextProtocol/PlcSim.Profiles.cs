using System.ComponentModel;
using ModelContextProtocol.Server;
using Siemens.Simatic.Simulation.Runtime;

namespace PlcSimMcpServer
{
    public static partial class PlcSimServer
    {
        [McpServerTool(Name = "plcsim_delete_profile"), Description("Delete a saved PLCSim simulation profile")]
        public static string PlcSimDeleteProfile() { return "Not implemented"; }

        [McpServerTool(Name = "plcsim_list_profiles"), Description("List all saved simulation profiles")]
        public static string PlcSimListProfiles() { return "Not implemented"; }

        [McpServerTool(Name = "plcsim_load_profile"), Description("Load a saved simulation profile")]
        public static string PlcSimLoadProfile(
            [Description("Name of the instance")] string instanceName,
            [Description("Path to the storage folder")] string storagePath)
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance != null)
            {
                instance.RetrieveStorage(storagePath);
                return $"Profile loaded for '{instanceName}' from '{storagePath}'.";
            }
            return $"Instance '{instanceName}' not found.";
        }

        [McpServerTool(Name = "plcsim_save_profile"), Description("Save the current simulation state as a profile")]
        public static string PlcSimSaveProfile(
            [Description("Name of the instance")] string instanceName,
            [Description("Path to the storage folder")] string storagePath)
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance != null)
            {
                instance.ArchiveStorage(storagePath);
                return $"Profile saved for '{instanceName}' to '{storagePath}'.";
            }
            return $"Instance '{instanceName}' not found.";
        }

        [McpServerTool(Name = "plcsim_update_profile"), Description("Update an existing simulation profile")]
        public static string PlcSimUpdateProfile() { return "Not implemented"; }
    }
}
