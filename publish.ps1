param(
    [string]$Version = '1.0'
)

# build project
dotnet publish FluentFin\FluentFin.csproj --self-contained -c Release -r win-x64 -o FluentFin\bin\publish\ /property:BuildVersion=$Version

# copy ffmpeg binaries, needed for flyleaf media player
xcopy Installer\FFmpeg FluentFin\bin\publish\FFmpeg /s /e /h
