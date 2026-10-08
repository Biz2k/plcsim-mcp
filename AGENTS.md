# Agents Guide

This repository can be used with agentic coding assistants. Follow these guidelines to collaborate safely and efficiently.

## Start Here

Development of this server is continued from a handoff package. Before any work, read
[`docs/handoff/README.md`](docs/handoff/README.md): it leads to the project context, the rules
that protect the user's PLCSIM Advanced instance, the open tasks, and the scripts in `tools/`.

## Test Execution Policy

- Offer to run tests, but only run them after explicit user confirmation.
- Tests may require user‑specific environment conditions (e.g., installed PLCSIM Advanced, licenses), so do not assume they will pass in your environment.
- When offering to run tests, clearly state prerequisites and potential side effects.
- If the user declines or does not respond, provide concise instructions for the user to run tests locally instead of running them yourself.

### Standard Commands

```powershell
dotnet test
```

## How To Ask For Confirmation

Use clear, actionable language. For example:

- "I can run `dotnet test` to validate the changes. Do you want me to run it now?"
- If approved: proceed and summarize results. If not approved: provide steps the user can run.

## PLCSIM Execution Policy

- The user manually launches PLCSIM Advanced and configuring basic system limits before we connect.
- Our MCP server creates instances, configures them, and powers them on.

## Formatting & Encoding

- Preserve existing indentation style (spaces).
- Do not modify file encodings; keep UTF-8 BOM where present.
- Ensure Windows CRLF line endings are retained when editing files.
