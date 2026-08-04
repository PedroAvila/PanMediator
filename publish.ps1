param(
    [Parameter(Mandatory = $false)]
    [string]$Version,

    [Parameter(Mandatory = $false)]
    [string]$ApiKey = $env:NUGET_API_KEY,

    [Parameter(Mandatory = $false)]
    [switch]$SkipPush
)

$ErrorActionPreference = "Stop"

$project = "PAN.Mediator/PAN.Mediator.csproj"
$output = "artifacts"
$source = "https://api.nuget.org/v3/index.json"

if (-not $Version) {
    $Version = Select-String -Path $project -Pattern '<Version>([^<]+)</Version>' | ForEach-Object { $_.Matches[0].Groups[1].Value }
}
Write-Host "Empaquetando PAN.Mediator $Version ..."

dotnet pack $project -c Release -o $output -p:Version=$Version
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($SkipPush) {
    Write-Host "Paquete listo: $output/PAN.Mediator.$Version.nupkg"
    exit 0
}

if (-not $ApiKey) {
    Write-Error "No se encontro una API key de NuGet. Configurala con la variable de entorno NUGET_API_KEY o pasala con -ApiKey."
    exit 1
}

Write-Host "Publicando PAN.Mediator.$Version.nupkg en $source ..."
dotnet nuget push "$output/PAN.Mediator.$Version.nupkg" --api-key $ApiKey --source $source
exit $LASTEXITCODE
