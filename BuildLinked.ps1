# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/nights.test.wizemantransparencyfix/*" -Force -Recurse
dotnet publish "./nights.test.wizemantransparencyfix.csproj" -c Release -o "$env:RELOADEDIIMODS/nights.test.wizemantransparencyfix" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location
