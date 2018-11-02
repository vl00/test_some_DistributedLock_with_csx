@echo off & title %~dp0 & cd /d %~dp0

dotnet tool install -g dotnet-script 1>nul 2>nul
dotnet-script run3.csx