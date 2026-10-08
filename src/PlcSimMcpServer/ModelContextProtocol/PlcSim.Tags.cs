using System;
using System.ComponentModel;
using System.Linq;
using ModelContextProtocol.Server;
using Siemens.Simatic.Simulation.Runtime;

namespace PlcSimMcpServer
{
    public static partial class PlcSimServer
    {
        [McpServerTool(Name = "plcsim_batch_read"), Description("Read multiple simulation tags in a single operation")]
        public static string PlcSimBatchRead() { return "Not implemented"; }

        [McpServerTool(Name = "plcsim_batch_write"), Description("Write multiple simulation tags in a single operation")]
        public static string PlcSimBatchWrite() { return "Not implemented"; }

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
                    return $"Tag '{tagName}' = Type: {val.Type}"; 
                } catch (Exception ex) {
                    return $"Failed to read tag: {ex.Message}";
                }
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
                    if (Enum.TryParse<EPrimitiveDataType>(dataType, true, out var eType))
                    {
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
                            default: return $"Writing for type '{dataType}' is not fully mapped in this stub yet.";
                        }
                        instance.Write(tagName, sdata);
                        return $"Successfully wrote '{value}' to tag '{tagName}'.";
                    }
                    return $"Invalid data type '{dataType}'.";
                } catch (Exception ex) {
                    return $"Failed to write tag: {ex.Message}";
                }
            }
            return $"Instance '{instanceName}' not found.";
        }
    }
}
