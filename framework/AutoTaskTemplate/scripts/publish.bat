::https://docs.microsoft.com/zh-cn/dotnet/core/tools/dotnet-publish
::关闭回显
@echo off

dotnet publish --configuration Release --self-contained false -o ./bin/Publish/net-dependent
echo "dotnet Ray.BiliBiliTool.Console.dll" > ./bin/Publish/net-dependent/start.bat

::dotnet publish --configuration Release --runtime win-x86 --self-contained true -o ./bin/Publish/win-x86-x64
::dotnet publish --configuration Release --runtime linux-x64 --self-contained true -o ./bin/Publish/linux-x64
::dotnet publish --configuration Release --runtime linux-arm --self-contained true -o ./bin/Publish/linux-arm
::dotnet publish --configuration Release --runtime linux-arm64 --self-contained true -o ./bin/Publish/linux-arm64
::dotnet publish --configuration Release --runtime osx-x64 --self-contained true -o ./bin/Publish/osx-x64

pause
