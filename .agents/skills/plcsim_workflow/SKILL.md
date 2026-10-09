---
name: plcsim_workflow
description: Explains the correct sequence for starting, configuring, and connecting a PLCSIM instance using MCP tools.
---

# PLCSIM Instance Creation & Configuration Workflow

## 🚨 HARD RULE: CONTEXT FIRST
When interacting with the MCP server, always check the current state first. If there is an existing PLCSIM window running (started by the user or previously via MCP), you MUST first read the current state (e.g., using `plcsim_get_instances`) to understand what is currently deployed, and work within that existing context.

When requested to create, deploy, or run a PLCSIM instance so that TIA Portal can discover it, you must follow a STRICT sequence of tool calls. This sequence simulates the underlying API logic required by Siemens PLCSIM Advanced.

## Strict Execution Sequence

1. **Set Global Network Mode**
   Use `plcsim_set_global_network_mode` to configure how the instances will connect to the host network.
   - For TIA Portal discovery on a single host, typically use `"TCPIPSingleAdapter"` (Virtual Switch).
   - If using multiple physical adapters, use `"TCPIPMultipleAdapter"`.
   - *Example:* `plcsim_set_global_network_mode(mode: "TCPIPSingleAdapter")`

2. **Clean Up Old Instance (Optional but recommended)**
   If an instance with the same name might already exist, delete it first to ensure a clean state.
   - *Example:* `plcsim_delete_instance(instanceName: "MyPLC")`

3. **Create the Instance**
   Use `plcsim_create_instance` to register the new PLC.
   - *Example:* `plcsim_create_instance(instanceName: "MyPLC", cpuType: "CPU1500_Unspecified")`

4. **Map the Network Interface (CRITICAL BEFORE POWER ON)**
   You MUST map the interface (e.g., `IE1`) to a PC host interface index *before* powering on the instance. If you don't map it, the Power On command will fail with `Error Code: -28, InvalidConfiguration`.
   - For `TCPIPSingleAdapter` (Virtual Switch), the `pcInterfaceIndex` is usually `0`.
   - *Example:* `plcsim_set_instance(instanceName: "MyPLC", plcInterface: "IE1", pcInterfaceIndex: 0)`

5. **Power ON the Instance**
   Use `plcsim_instance_control` to start the virtual PLC.
   - *Example:* `plcsim_instance_control(instanceName: "MyPLC", action: "PowerOn")`

6. **Wait for Boot (Optional)**
   The system may take a few seconds to boot up the instance.

7. **Set IP Address (CRITICAL AFTER POWER ON)**
   You MUST set the IP Address *after* the instance is powered on. If you try to set it before PowerOn, it will fail with `Error Code: -14, InstanceNotRunning`. (Note: This step can be skipped if you intend to let TIA Portal assign the IP address via DCP during the first download).
   - *Example:* `plcsim_set_instance(instanceName: "MyPLC", portId: 1, ipAddress: "192.168.0.1", subnetMask: "255.255.255.0", defaultGateway: "0.0.0.0")`

## Summary Checklist for AI:
[ ] Mode set?
[ ] Created?
[ ] Mapped?
[ ] Powered On?
[ ] (Optional) IP Configured?

*Note: In the future, these steps will be consolidated into a single `plcsim_deploy_instance` tool.*

## ⚠️ Known Issues & API Instability
- **UI Freeze / Crash on Rapid API Calls:** Performing bulk operations on instances (especially rapid creation or deletion of multiple instances) while the graphical `Siemens.Simatic.PlcSim.Advanced.UserInterface` is open can cause a race condition in the UI's event loop, leading to a freeze. **RULE:** Do not perform mass operations rapidly. Enforce a minimum delay of 500ms between instance state changes or deletions.
- **Ghost Manager State:** If the UI freezes and the user forcibly closes it, or if `.NET` holds onto COM `IInstance` objects via GC, the `Manager.exe` might refuse to shut down. This causes the UI to freeze on exit or fail to open.
- **Fix:** We now use `System.GC.Collect()` internally to release these COM wrappers. However, if the ghost state persists, use the `plcsim_host_control(action: "Stop")` tool or `plcsim_cleanup_memory` tool.
- **CRITICAL:** Because the Siemens API DLL drops its IPC connection completely when `Manager.exe` dies, if `Manager.exe` is killed (or dies naturally during UI closure), the active MCP Server will get a `ConnectionError` indefinitely. The user MUST restart the MCP Server in their IDE settings to re-initialize the DLL.
