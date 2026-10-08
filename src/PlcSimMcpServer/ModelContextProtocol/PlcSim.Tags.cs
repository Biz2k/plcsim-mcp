using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text;
using ModelContextProtocol.Server;
using Siemens.Simatic.Simulation.Runtime;
using System.Collections.Generic;

namespace PlcSimMcpServer
{
    public static partial class PlcSimServer
    {
        public class TagWriteRequest
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string Type { get; set; }
        }

        private static string FormatDataValue(SDataValue val)
        {
            switch (val.Type)
            {
                case EPrimitiveDataType.Bool: return val.Bool.ToString();
                case EPrimitiveDataType.Int8: return val.Int8.ToString();
                case EPrimitiveDataType.UInt8: return val.UInt8.ToString();
                case EPrimitiveDataType.Int16: return val.Int16.ToString();
                case EPrimitiveDataType.UInt16: return val.UInt16.ToString();
                case EPrimitiveDataType.Int32: return val.Int32.ToString();
                case EPrimitiveDataType.UInt32: return val.UInt32.ToString();
                case EPrimitiveDataType.Int64: return val.Int64.ToString();
                case EPrimitiveDataType.UInt64: return val.UInt64.ToString();
                case EPrimitiveDataType.Float: return val.Float.ToString();
                case EPrimitiveDataType.Double: return val.Double.ToString();
                case EPrimitiveDataType.Char: return val.Char.ToString();
                case EPrimitiveDataType.WChar: return val.WChar.ToString();
                default: return $"[Unsupported Type: {val.Type}]";
            }
        }

        private static SDataValue ParseDataValue(string value, string dataType)
        {
            if (!Enum.TryParse<EPrimitiveDataType>(dataType, true, out var eType))
                throw new ArgumentException($"Invalid data type '{dataType}'");

            var sdata = new SDataValue { Type = eType };
            switch (eType)
            {
                case EPrimitiveDataType.Bool: sdata.Bool = bool.Parse(value); break;
                case EPrimitiveDataType.Int8: sdata.Int8 = sbyte.Parse(value); break;
                case EPrimitiveDataType.UInt8: sdata.UInt8 = byte.Parse(value); break;
                case EPrimitiveDataType.Int16: sdata.Int16 = short.Parse(value); break;
                case EPrimitiveDataType.UInt16: sdata.UInt16 = ushort.Parse(value); break;
                case EPrimitiveDataType.Int32: sdata.Int32 = int.Parse(value); break;
                case EPrimitiveDataType.UInt32: sdata.UInt32 = uint.Parse(value); break;
                case EPrimitiveDataType.Int64: sdata.Int64 = long.Parse(value); break;
                case EPrimitiveDataType.UInt64: sdata.UInt64 = ulong.Parse(value); break;
                case EPrimitiveDataType.Float: sdata.Float = float.Parse(value); break;
                case EPrimitiveDataType.Double: sdata.Double = double.Parse(value); break;
                default: throw new ArgumentException($"Writing for type '{dataType}' is not supported yet.");
            }
            return sdata;
        }

        [McpServerTool(Name = "plcsim_batch_read"), Description("Read multiple simulation tags. Pass a JSON array of tag names: [\"Tag1\", \"Tag2\"]")]
        public static string PlcSimBatchRead(
            [Description("Name of the instance")] string instanceName,
            [Description("JSON array of tag names")] string tagNamesJson)
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance == null) return $"Instance '{instanceName}' not found.";

            try
            {
                var tags = JsonSerializer.Deserialize<string[]>(tagNamesJson);
                if (tags == null || tags.Length == 0) return "No tags provided.";

                var signals = tags.Select(t => new SDataValueByName { Name = t }).ToArray();
                instance.ReadSignals(ref signals);

                var sb = new StringBuilder();
                foreach (var sig in signals)
                {
                    if (sig.ErrorCode == ERuntimeErrorCode.OK)
                    {
                        sb.AppendLine($"- {sig.Name} ({sig.DataValue.Type}) = {FormatDataValue(sig.DataValue)}");
                    }
                    else
                    {
                        sb.AppendLine($"- {sig.Name} = Error: {sig.ErrorCode}");
                    }
                }
                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                return $"Error reading tags: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_batch_write"), Description("Write multiple simulation tags. Pass JSON array: [{\"Name\":\"T1\",\"Value\":\"true\",\"Type\":\"Bool\"}]")]
        public static string PlcSimBatchWrite(
            [Description("Name of the instance")] string instanceName,
            [Description("JSON array of tag objects")] string tagsJson)
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance == null) return $"Instance '{instanceName}' not found.";

            try
            {
                var reqs = JsonSerializer.Deserialize<TagWriteRequest[]>(tagsJson);
                if (reqs == null || reqs.Length == 0) return "No tags provided.";

                var signals = new List<SDataValueByName>();
                foreach (var req in reqs)
                {
                    try {
                        signals.Add(new SDataValueByName {
                            Name = req.Name,
                            DataValue = ParseDataValue(req.Value, req.Type)
                        });
                    } catch (Exception ex) {
                        return $"Error parsing value for tag '{req.Name}': {ex.Message}";
                    }
                }

                var sigArray = signals.ToArray();
                instance.WriteSignals(ref sigArray);

                var sb = new StringBuilder();
                foreach (var sig in sigArray)
                {
                    if (sig.ErrorCode == ERuntimeErrorCode.OK)
                        sb.AppendLine($"- {sig.Name} = Successfully written");
                    else
                        sb.AppendLine($"- {sig.Name} = Error: {sig.ErrorCode}");
                }
                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                return $"Error writing tags: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_read_tag"), Description("Read the value of a single simulation tag")]
        public static string PlcSimReadTag(
            [Description("Name of the instance")] string instanceName,
            [Description("Tag name (e.g. \"=201+?-QF1:11\")")] string tagName)
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance != null)
            {
                try {
                    var val = instance.Read(tagName);
                    return $"Tag '{tagName}' ({val.Type}) = {FormatDataValue(val)}"; 
                } catch (Exception ex) {
                    return $"Failed to read tag: {ex.Message}";
                }
            }
            return $"Instance '{instanceName}' not found.";
        }

        [McpServerTool(Name = "plcsim_write_tag"), Description("Write a value to a single simulation tag")]
        public static string PlcSimWriteTag(
            [Description("Name of the instance")] string instanceName,
            [Description("Tag name")] string tagName,
            [Description("Value to write (must match the tag's data type, e.g. true for Bool)")] string value,
            [Description("Tag data type (e.g. Bool, Int32)")] string dataType = "Bool")
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance != null)
            {
                try {
                    var sdata = ParseDataValue(value, dataType);
                    instance.Write(tagName, sdata);
                    return $"Successfully wrote '{value}' to tag '{tagName}'.";
                } catch (Exception ex) {
                    return $"Failed to write tag: {ex.Message}";
                }
            }
            return $"Instance '{instanceName}' not found.";
        }

        [McpServerTool(Name = "plcsim_list_tags"), Description("List all tags available in the simulation")]
        public static string PlcSimListTags(
            [Description("Name of the instance")] string instanceName,
            [Description("Maximum tags to return")] int limit = 100)
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance != null)
            {
                instance.UpdateTagList();
                var tags = instance.TagInfos;
                if (tags == null || tags.Length == 0) return "No tags found.";
                return string.Join("\n", tags.Take(limit).Select(t => $"- {t.Name} ({t.PrimitiveDataType})"));
            }
            return $"Instance '{instanceName}' not found.";
        }

        [McpServerTool(Name = "plcsim_refresh_tags"), Description("Refresh the tag list from the simulation")]
        public static string PlcSimRefreshTags([Description("Name of the instance")] string instanceName)
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance != null)
            {
                instance.UpdateTagList();
                return $"Tags refreshed for '{instanceName}'. Total tags: {instance.TagInfos.Length}";
            }
            return $"Instance '{instanceName}' not found.";
        }
    }
}
