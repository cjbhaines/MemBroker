src\.nuget\NuGet.exe update -self

mkdir build

C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe src\MemBroker.sln /t:Clean,Rebuild /p:Configuration=Release /fileLogger
src\.nuget\NuGet.exe pack src\MemBroker\MemBroker.nuspec -OutputDirectory build

pause