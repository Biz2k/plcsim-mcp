# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]
### Added
- Initial extraction of `PlcSimMcpServer` from `tiaportal-mcp` repository.
- Basic stubs for 31 MCP tools using Siemens.Simatic.Simulation.Runtime.Api.
- Directory structure mirrored from `tiaportal-mcp` for future merging.
- Implemented `plcsim_deploy_instance` tool for one-step instance creation, mapping, power on, and IP assignment.
- Added automatic parsing of primitive data types (Bool, Int, Float, etc.) for `plcsim_write_tag` and `plcsim_batch_write`.
- Successfully validated complete pipeline: Autonomous PLCSIM deployment, automated TIA Portal project hardware updates, PLC password protection handling, project compilation, automatic download to the virtual PLC, and tag verification.
- Added documentation on avoiding rapid bulk API calls to prevent PLCSIM Advanced UI freezing.

### Changed
- Improved context-aware logging when configuring instance gateway (`0.0.0.0` replacement).
