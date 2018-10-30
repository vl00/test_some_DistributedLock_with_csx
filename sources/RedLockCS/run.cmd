@echo off & title %~dp0 & cd /d %~dp0

dotnet tool install -g dotnet-script 1>nul 2>nul

set _cd=%cd%

cd RedlockCSharp
dotnet publish -c release -o bin/publish 

cd %_cd%
dotnet-script run.csx