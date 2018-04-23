cd .\Stardust.Aadb2c.AuthenticationFilter
\nuget\nuget pack Stardust.Aadb2c.AuthenticationFilter.nuspec -OutputDirectory .\pkg
\nuget\nuget pack Stardust.Aadb2c.AuthenticationFilter.nuspec -Symbols -OutputDirectory .\sym

\nuget\nuget push .\pkg\*.nupkg -Source https://api.nuget.org/v3/index.json
\nuget\nuget push .\sym\*.symbols.nupkg -Source https://nuget.smbsrc.net/

rmdir pkg /S /Q
rmdir sym /S /Q

SET /P key=Press enter to continue

cd ..\Stardust.Aadb2c.AuthenticationFilter.Core
dotnet build --configuration Release 
dotnet pack --configuration Release --output nupkgs --include-symbols

\nuget\nuget push .\nupkgs\*.nupkg -Source https://api.nuget.org/v3/index.json
\nuget\nuget push .\nupkgs\*.symbols.nupkg -Source https://nuget.smbsrc.net/

rmdir nupkgs /S /Q
SET /P key=Press enter to continue