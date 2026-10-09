using System;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Siemens.Simatic.Simulation.Runtime;

namespace PlcSimMcpServer
{
    public static partial class PlcSimServer
    {
        private static string GetProfilesDir()
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        [McpServerTool(Name = "plcsim_save_profile"), Description("Save the current simulation state (Virtual SD Card) as a profile")]
        public static string PlcSimSaveProfile(
            [Description("Name of the instance")] string instanceName,
            [Description("Name of the profile to save")] string profileName)
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                    return $"Failed: Instance '{instanceName}' does not exist.";

                var instance = SimulationRuntimeManager.CreateInterface(instanceName);
                string path = Path.Combine(GetProfilesDir(), profileName);

                if (Directory.Exists(path))
                {
                    return $"Error: Profile '{profileName}' already exists. Delete it first or use a different name.";
                }

                instance.ArchiveStorage(path);
                return $"Profile '{profileName}' saved successfully for instance '{instanceName}'.";
            }
            catch (Exception ex)
            {
                return $"Error saving profile: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_load_profile"), Description("Load a saved simulation profile (Virtual SD Card) into an instance")]
        public static string PlcSimLoadProfile(
            [Description("Name of the instance")] string instanceName,
            [Description("Name of the profile to load")] string profileName)
        {
            try
            {
                if (!SimulationRuntimeManager.RegisteredInstanceInfo.Any(i => i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase)))
                    return $"Failed: Instance '{instanceName}' does not exist.";

                string path = Path.Combine(GetProfilesDir(), profileName);
                if (!Directory.Exists(path))
                    return $"Error: Profile '{profileName}' not found.";

                var instance = SimulationRuntimeManager.CreateInterface(instanceName);
                instance.RetrieveStorage(path);
                return $"Profile '{profileName}' loaded successfully into instance '{instanceName}'.";
            }
            catch (Exception ex)
            {
                return $"Error loading profile: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_list_profiles"), Description("List all saved simulation profiles")]
        public static string PlcSimListProfiles()
        {
            try
            {
                var dir = GetProfilesDir();
                var profiles = Directory.GetDirectories(dir).Select(Path.GetFileName).ToArray();
                
                if (profiles.Length == 0)
                    return "No profiles found.";

                return JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return $"Error listing profiles: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_delete_profile"), Description("Delete a saved PLCSim simulation profile")]
        public static string PlcSimDeleteProfile(
            [Description("Name of the profile to delete")] string profileName)
        {
            try
            {
                string path = Path.Combine(GetProfilesDir(), profileName);
                if (!Directory.Exists(path))
                    return $"Error: Profile '{profileName}' not found.";

                Directory.Delete(path, true);
                return $"Profile '{profileName}' deleted successfully.";
            }
            catch (Exception ex)
            {
                return $"Error deleting profile: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_update_profile"), Description("Update an existing simulation profile")]
        public static string PlcSimUpdateProfile(
            [Description("Name of the instance")] string instanceName,
            [Description("Name of the profile to update")] string profileName)
        {
            try
            {
                string path = Path.Combine(GetProfilesDir(), profileName);
                if (!Directory.Exists(path))
                    return $"Error: Profile '{profileName}' not found. Cannot update.";

                Directory.Delete(path, true);
                return PlcSimSaveProfile(instanceName, profileName);
            }
            catch (Exception ex)
            {
                return $"Error updating profile: {ex.Message}";
            }
        }
    }
}
