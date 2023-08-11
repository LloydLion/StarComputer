mkdir publish
dotnet build StarComputer.Client.UI.Avalonia/StarComputer.Client.UI.Avalonia.csproj -o publish/Client -c Release
dotnet build StarComputer.Server.UI.Avalonia/StarComputer.Server.UI.Avalonia.csproj -o publish/Server -c Release

# Plugins
dotnet build QuickChatPlugin/QuickChatPlugin.csproj -o publish/plugins/QuickChatPlugin -c Release

# Client assemble
mkdir publish\Client\plugins
cp "publish\plugins\" "publish\Client\plugins\" -ruf

# Server assemble
mkdir publish\Server\plugins
cp "publish\plugins\" "publish\Server\plugins\" -ruf