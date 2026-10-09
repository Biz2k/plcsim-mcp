[CmdletBinding()]
param()

$dllPath = "C:\Users\Biz\Antigravity\LocalData\Siemens.Simatic.Simulation.Runtime.Api.x64.dll"
Add-Type -Path $dllPath

$version = [Siemens.Simatic.Simulation.Runtime.SimulationRuntimeManager]::Version
Write-Host "PLCSIM API Version: $version"

$instances = [Siemens.Simatic.Simulation.Runtime.SimulationRuntimeManager]::RegisteredInstanceInfo
if ($instances -and $instances.Count -gt 0) {
    Write-Host "Found $($instances.Count) instances:"
    foreach ($inst in $instances) {
        $interface = [Siemens.Simatic.Simulation.Runtime.SimulationRuntimeManager]::CreateInterface($inst.Name)
        Write-Host " - Name: $($inst.Name), State: $($interface.OperatingState), IP: $($interface.IP)"
    }
} else {
    Write-Host "No registered instances found."
}
