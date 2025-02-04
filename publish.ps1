param(
    [string]$Version = '1.0'
)

# build project
dotnet publish FluentFin\FluentFin.csproj --self-contained -c Release -r win-x64 -o FluentFin\bin\publish\ /property:BuildVersion=$Version
