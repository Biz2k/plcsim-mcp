# PLCSIM Advanced MCP Server

This repository provides an [MCP (Model Context Protocol)](https://modelcontextprotocol.io) server for interacting with **Siemens PLCSIM Advanced**. It enables AI coding assistants (like Cursor, Windsurf, RooCode, Claude Desktop, Antigravity) to autonomously create, manage, and interact with virtual S7-1500 PLC instances.

## Features

- **Lifecycle Management:** Create, delete, and list PLCSIM instances.
- **Operating States:** Power on/off, Run/Stop, and Memory Reset operations.
- **Networking:** Map IP suites to PLC interfaces and configure gateway settings.
- **Tag Access:** Read and write PLC tags (single or bulk operations) with automatic data type handling.
- **Profiles & Memory:** Save/load snapshots and manage virtual SD cards.
- **Host Control:** Cleanly start and stop the PLCSIM Advanced host process and manage garbage collection.

## Requirements

- Windows OS
- Siemens S7-PLCSIM Advanced V5.0 (or newer) installed and licensed.
- .NET Framework 4.8 runtime (included in modern Windows by default).

## Installation

You can install the pre-compiled server directly into your AI assistant.

1. Locate the built release inside the `Install/PlcSimMcpServer` folder.
2. In your AI Assistant's MCP configuration file (e.g. `mcp_config.json` or `claude_desktop_config.json`), add the server definition:

```json
{
  "mcpServers": {
    "plcsim-mcp-server": {
      "command": "C:\\Absolute\\Path\\To\\Install\\PlcSimMcpServer\\PlcSimMcpServer.exe",
      "args": [],
      "env": {}
    }
  }
}
```

> **Note:** Replace `C:\\Absolute\\Path\\To...` with the actual path to the executable on your system.

## Usage

Once configured, the AI assistant will have access to the `plcsim_...` tools (e.g. `plcsim_create_instance`, `plcsim_read_tag`). Ask the AI to:
- "Create a PLCSIM instance named MyTestPLC"
- "Set its IP to 192.168.0.1 and power it on"
- "Read all tags from DB1"
- "Start the PLCSIM Advanced service"

## Development & Smoke Tests

Smoke tests verify the health of the JSON-RPC communication layer.
To run the automated smoke test:
```powershell
powershell -ExecutionPolicy Bypass -File tools\smoke.ps1
```
