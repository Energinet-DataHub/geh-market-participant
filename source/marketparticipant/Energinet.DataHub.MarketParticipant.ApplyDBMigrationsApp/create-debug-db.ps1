
param (
    [string]$ConnectionString
)

# Check if connection string is provided
if (-not $ConnectionString) {
    Write-Host "Please provide a connection string."
    exit
}

# Get the directory where the script is located
$ScriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path

# Construct the path to DatabaseMigration.exe
$MigrationToolPath = Join-Path -Path $ScriptDirectory -ChildPath "\bin\Debug\net7.0\Energinet.DataHub.MarketParticipant.ApplyDBMigrationsApp.exe"

# Check if DatabaseMigration.exe exists
if (-not (Test-Path $MigrationToolPath)) {
    Write-Host "Energinet.DataHub.MarketParticipant.ApplyDBMigrationsApp.exe not found in the expected location (\bin\Debug\net7.0\)."
    exit
}

# Call Migration with the provided connection string
Write-Host "Running Energinet.DataHub.MarketParticipant.ApplyDBMigrationsApp.exe with connection string: $ConnectionString :: Path: $MigrationToolPath" 
& $MigrationToolPath $ConnectionString "LOCALDEV" "includeSeedData" -NoNewWindow -Wait

# End of script
