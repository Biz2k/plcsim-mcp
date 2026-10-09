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

        private static int ParseSiemensTime(string value)
        {
            if (int.TryParse(value, out int ms)) return ms;
            
            value = value.Replace("T#", "").Replace("t#", "").ToLowerInvariant();
            TimeSpan ts = TimeSpan.Zero;
            
            var regex = new System.Text.RegularExpressions.Regex(@"(?:(\d+)d)?(?:(\d+)h)?(?:(\d+)m(?!s))?(?:(\d+)s)?(?:(\d+)ms)?");
            var match = regex.Match(value);
            if (match.Success && match.Length == value.Length)
            {
                int d = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
                int h = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
                int m = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
                int s = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;
                int mls = match.Groups[5].Success ? int.Parse(match.Groups[5].Value) : 0;
                ts = new TimeSpan(d, h, m, s, mls);
                return (int)ts.TotalMilliseconds;
            }
            throw new ArgumentException("Invalid Siemens Time format. Use ms or formats like '10s', '1h20m'.");
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
            if (dataType.Equals("Time", StringComparison.OrdinalIgnoreCase))
            {
                return new SDataValue { Type = EPrimitiveDataType.Int32, Int32 = ParseSiemensTime(value) };
            }

            if (!Enum.TryParse<EPrimitiveDataType>(dataType, true, out var eType))
                throw new ArgumentException($"Invalid data type '{dataType}'");

            var culture = System.Globalization.CultureInfo.InvariantCulture;
            var sdata = new SDataValue { Type = eType };
            switch (eType)
            {
                case EPrimitiveDataType.Bool: sdata.Bool = bool.Parse(value); break;
                case EPrimitiveDataType.Int8: sdata.Int8 = sbyte.Parse(value, culture); break;
                case EPrimitiveDataType.UInt8: sdata.UInt8 = byte.Parse(value, culture); break;
                case EPrimitiveDataType.Int16: sdata.Int16 = short.Parse(value, culture); break;
                case EPrimitiveDataType.UInt16: sdata.UInt16 = ushort.Parse(value, culture); break;
                case EPrimitiveDataType.Int32: sdata.Int32 = int.Parse(value, culture); break;
                case EPrimitiveDataType.UInt32: sdata.UInt32 = uint.Parse(value, culture); break;
                case EPrimitiveDataType.Int64: sdata.Int64 = long.Parse(value, culture); break;
                case EPrimitiveDataType.UInt64: sdata.UInt64 = ulong.Parse(value, culture); break;
                case EPrimitiveDataType.Float: sdata.Float = float.Parse(value.Replace(",", "."), culture); break;
                case EPrimitiveDataType.Double: sdata.Double = double.Parse(value.Replace(",", "."), culture); break;
                default: throw new ArgumentException($"Writing for type '{dataType}' is not supported via SDataValue.");
            }
            return sdata;
        }

        [McpServerTool(Name = "plcsim_batch_read"), Description("Read multiple simulation tags. Pass JSON array of tag names: [\"Tag1\", \"Tag2\"]. Does not support String/WString.")]
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
                if (tags.Length > 500) return "Error: Maximum 500 tags allowed per batch to prevent context window overflow.";

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

        [McpServerTool(Name = "plcsim_batch_write"), Description("Write multiple simulation tags. Pass JSON array: [{\"Name\":\"T1\",\"Value\":\"10s\",\"Type\":\"Time\"}, {\"Name\":\"T2\",\"Value\":\"Hello\",\"Type\":\"String\"}]. The server automatically parses Siemens Time formats (like '10s' or '1m') into milliseconds, and handles String/WString natively.")]
        public static string PlcSimBatchWrite(
            [Description("Name of the instance")] string instanceName,
            [Description("JSON array of tag objects. Allowed types: Bool, Int32, Float, Time, String, WString, etc.")] string tagsJson)
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance == null) return $"Instance '{instanceName}' not found.";

            try
            {
                var reqs = JsonSerializer.Deserialize<TagWriteRequest[]>(tagsJson);
                if (reqs == null || reqs.Length == 0) return "No tags provided.";

                var signals = new List<SDataValueByName>();
                var errors = new List<string>();
                int successCount = 0;

                foreach (var req in reqs)
                {
                    try 
                    {
                        if (req.Type.Equals("String", StringComparison.OrdinalIgnoreCase))
                        {
                            instance.WriteString(req.Name, req.Value);
                            successCount++;
                        }
                        else if (req.Type.Equals("WString", StringComparison.OrdinalIgnoreCase))
                        {
                            instance.WriteWString(req.Name, req.Value);
                            successCount++;
                        }
                        else
                        {
                            signals.Add(new SDataValueByName {
                                Name = req.Name,
                                DataValue = ParseDataValue(req.Value, req.Type)
                            });
                        }
                    } 
                    catch (Exception ex) 
                    {
                        errors.Add($"- {req.Name} = Error parsing/writing: {ex.Message}");
                    }
                }

                if (signals.Count > 0)
                {
                    var sigArray = signals.ToArray();
                    instance.WriteSignals(ref sigArray);

                    foreach (var sig in sigArray)
                    {
                        if (sig.ErrorCode == ERuntimeErrorCode.OK)
                            successCount++;
                        else
                            errors.Add($"- {sig.Name} = Error: {sig.ErrorCode}");
                    }
                }
                
                var result = new StringBuilder();
                result.AppendLine($"Successfully written {successCount} tags.");
                if (errors.Count > 0)
                {
                    result.AppendLine($"Errors ({errors.Count}):");
                    foreach (var err in errors.Take(50)) result.AppendLine(err);
                    if (errors.Count > 50) result.AppendLine($"... and {errors.Count - 50} more errors.");
                }
                
                return result.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                return $"Error writing tags: {ex.Message}";
            }
        }

        [McpServerTool(Name = "plcsim_read_tag"), Description("Read the value of a single simulation tag (including String, WString)")]
        public static string PlcSimReadTag(
            [Description("Name of the instance")] string instanceName,
            [Description("Tag name (e.g. \"=201+?-QF1:11\")")] string tagName,
            [Description("Tag data type (required for String/WString, optional for others)")] string dataType = "")
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance != null)
            {
                try {
                    if (dataType.Equals("String", StringComparison.OrdinalIgnoreCase))
                    {
                        string strVal = instance.ReadString(tagName);
                        return $"Tag '{tagName}' (String) = {strVal}";
                    }
                    else if (dataType.Equals("WString", StringComparison.OrdinalIgnoreCase))
                    {
                        string strVal = instance.ReadWString(tagName);
                        return $"Tag '{tagName}' (WString) = {strVal}";
                    }
                    
                    var val = instance.Read(tagName);
                    return $"Tag '{tagName}' ({val.Type}) = {FormatDataValue(val)}"; 
                } catch (Exception ex) {
                    return $"Failed to read tag: {ex.Message}";
                }
            }
            return $"Instance '{instanceName}' not found.";
        }

        [McpServerTool(Name = "plcsim_write_tag"), Description("Write a value to a single simulation tag. Server parses Time formats (e.g. '10s') automatically.")]
        public static string PlcSimWriteTag(
            [Description("Name of the instance")] string instanceName,
            [Description("Tag name")] string tagName,
            [Description("Value to write")] string value,
            [Description("Tag data type (e.g. Bool, Int32, Time, String)")] string dataType = "Bool")
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance != null)
            {
                try {
                    if (dataType.Equals("String", StringComparison.OrdinalIgnoreCase))
                    {
                        instance.WriteString(tagName, value);
                    }
                    else if (dataType.Equals("WString", StringComparison.OrdinalIgnoreCase))
                    {
                        instance.WriteWString(tagName, value);
                    }
                    else
                    {
                        var sdata = ParseDataValue(value, dataType);
                        instance.Write(tagName, sdata);
                    }
                    return $"Successfully wrote '{value}' to tag '{tagName}'.";
                } catch (Exception ex) {
                    return $"Failed to write tag: {ex.Message}";
                }
            }
            return $"Instance '{instanceName}' not found.";
        }

        [McpServerTool(Name = "plcsim_list_tags"), Description("List tags available in the simulation (with pagination)")]
        public static string PlcSimListTags(
            [Description("Name of the instance")] string instanceName,
            [Description("Offset (start index)")] int offset = 0,
            [Description("Maximum tags to return")] int limit = 500)
        {
            var instance = SimulationRuntimeManager.CreateInterface(instanceName);
            if (instance != null)
            {
                instance.UpdateTagList();
                var tags = instance.TagInfos;
                if (tags == null || tags.Length == 0) return "No tags found.";
                
                var selectedTags = tags.Skip(offset).Take(limit).ToArray();
                string res = $"Showing {offset + 1} to {offset + selectedTags.Length} of {tags.Length} tags:\n";
                res += string.Join("\n", selectedTags.Select(t => $"- {t.Name} ({t.PrimitiveDataType})"));
                
                if (offset + selectedTags.Length < tags.Length) 
                {
                    res += $"\n... (Use offset={offset + limit} to see more)";
                }
                return res;
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
