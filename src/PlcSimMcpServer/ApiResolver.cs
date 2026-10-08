using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace PlcSimMcpServer
{
    public static class ApiResolver
    {
        private static string SettingsFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plcsim_settings.json");

        public class ServerSettings
        {
            public string ApiDllPath { get; set; }
        }

        public static void Initialize()
        {
            var settings = LoadSettings();

            if (string.IsNullOrEmpty(settings.ApiDllPath) || !File.Exists(settings.ApiDllPath))
            {
                Console.WriteLine("PLCSIM Advanced API path not configured or invalid. Searching for installed versions...");
                settings.ApiDllPath = DetectAndSelectApi();
                
                if (settings.ApiDllPath != null)
                {
                    SaveSettings(settings);
                    Console.WriteLine($"Saved PLCSIM API path to settings: {settings.ApiDllPath}");
                }
                else
                {
                    Console.WriteLine("WARNING: Could not find any compatible PLCSIM Advanced API installation!");
                }
            }
            else
            {
                Console.WriteLine($"Using PLCSIM API from settings: {settings.ApiDllPath}");
            }

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static string DetectAndSelectApi()
        {
            var searchRoots = new[]
            {
                @"C:\Program Files\Siemens",
                @"C:\Program Files\Common Files\Siemens"
            };

            var allPaths = new List<string>();

            foreach (var root in searchRoots)
            {
                if (Directory.Exists(root))
                {
                    allPaths.AddRange(SafeGetFiles(root, "Siemens.Simatic.Simulation.Runtime.Api.x64.dll"));
                }
            }

            if (allPaths.Count == 0) return null;

            Console.WriteLine("Found following PLCSIM Advanced API versions:");
            
            string bestPath = null;
            Version bestVersion = new Version(0, 0);

            foreach (var path in allPaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var info = FileVersionInfo.GetVersionInfo(path);
                    Console.WriteLine($" - {info.FileVersion} at {path}");
                    var v = new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
                    
                    if (v > bestVersion)
                    {
                        bestVersion = v;
                        bestPath = path;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" - Error reading {path}: {ex.Message}");
                }
            }

            if (bestPath != null)
            {
                Console.WriteLine($"Selected optimal version: {bestVersion}");
            }

            return bestPath;
        }

        private static List<string> SafeGetFiles(string rootPath, string pattern)
        {
            var result = new List<string>();
            var stack = new Stack<string>();
            stack.Push(rootPath);

            while (stack.Count > 0)
            {
                string dir = stack.Pop();
                try
                {
                    result.AddRange(Directory.GetFiles(dir, pattern));
                    foreach (string subDir in Directory.GetDirectories(dir))
                    {
                        stack.Push(subDir);
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (PathTooLongException) { }
                catch (IOException) { }
            }

            return result;
        }

        private static ServerSettings LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<ServerSettings>(json) ?? new ServerSettings();
                }
                catch
                {
                    // Ignore errors
                }
            }
            return new ServerSettings();
        }

        private static void SaveSettings(ServerSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("Siemens.Simatic.Simulation.Runtime.Api.x64"))
            {
                var settings = LoadSettings();
                if (!string.IsNullOrEmpty(settings.ApiDllPath) && File.Exists(settings.ApiDllPath))
                {
                    return Assembly.LoadFrom(settings.ApiDllPath);
                }
            }
            return null;
        }
    }
}
