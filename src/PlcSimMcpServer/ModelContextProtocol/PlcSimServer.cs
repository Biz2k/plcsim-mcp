using System;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace PlcSimMcpServer
{
    [McpServerToolType]
    public static partial class PlcSimServer
    {
        public static ILogger? Logger { get; set; }
    }
}
