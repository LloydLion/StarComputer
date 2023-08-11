mkdir publish
dotnet build StarComputer.Client.UI.Avalonia/StarComputer.Client.UI.Avalonia.csproj -o publish/Client -c Release
dotnet build StarComputer.Server.UI.Avalonia/StarComputer.Server.UI.Avalonia.csproj -o publish/Server -c Release

:: Plugins
dotnet build QuickChatPlugin/QuickChatPlugin.csproj -o publish/plugins/QuickChatPlugin -c Release

:: Client assemble
mkdir publish\Client\plugins
xcopy "publish\plugins\" "publish\Client\plugins\" /S /Y /R

:: Server assemble
mkdir publish\Server\plugins
xcopy "publish\plugins\" "publish\Server\plugins\" /S /Y /R