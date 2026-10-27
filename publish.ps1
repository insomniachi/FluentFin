param(
    [string]$Version = '1.0',
    [ValidateSet('x64','arm64')][string]$Arch = 'x64'
)

# build project
dotnet publish FluentFin\FluentFin.csproj --self-contained -c Release -r win-$Arch -p:Platform=$Arch -o FluentFin\bin\$(if ($Arch -eq 'arm64') { 'publish-arm64' } else { 'publish' })\ /property:BuildVersion=$Version
