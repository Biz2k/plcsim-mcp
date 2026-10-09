# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [0.1.0] - 2026-10-09
### Added
- Initial extraction of `PlcSimMcpServer` from `tiaportal-mcp` repository.
- Structured MCP tools using Siemens.Simatic.Simulation.Runtime.Api.
- Lifecycle management (`plcsim_create_instance`, `plcsim_delete_instance`, `plcsim_instance_control`).
- Network configuration tools (`plcsim_set_global_network_mode`, `plcsim_set_instance`).
- Implemented `plcsim_deploy_instance` tool for one-step instance creation, mapping, power on, and IP assignment.
- Fully functioning tag operations: `plcsim_read_tag`, `plcsim_write_tag`, `plcsim_batch_read`, `plcsim_batch_write` with automatic type conversions.
- Profile management tools (`plcsim_save_profile`, `plcsim_load_profile`, `plcsim_list_profiles`, `plcsim_delete_profile`, `plcsim_update_profile`).
- Smoke testing scripts and JSON-RPC test automation using `tools/mcp-call.ps1` and `tools/smoke.ps1`.
- Built release binary in `Install/PlcSimMcpServer` and created `README.md` for users.

### Changed
- Improved context-aware logging when configuring instance gateway (`0.0.0.0` replacement).
