@echo off
cd ..\framework

echo "Cleaning"
for /d /r %%c in (obj) do @if exist "%%c" (@rd /s /q "%%c" & echo Delete %%c)
for /d /r %%c in (bin) do @if exist "%%c" (@rd /s /q "%%c" & echo Delete %%c)
for /d /r %%c in (.vs) do @if exist "%%c" (@rd /s /q "%%c" & echo Delete %%c)
echo Deletion complete.

echo "Installing"
dotnet new install .\ --force
pause