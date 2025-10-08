* Package publishing instructions:
    * Navigate into class library project where you want to publish package
    * Then pack this project in release mode:
      *     dotnet pack --configuration Release
    * Then publish this package into nuget provider server like this:
      *     dotnet nuget push .\bin\Release\DotNetCore.SharpStreamer.1.0.0.nupkg -s https://api.nuget.org/v3/index.json -k {Your_API_Key}
        * Verbose example:
          *     dotnet nuget push ./bin/Release/YourPackageName.1.0.0.nupkg \
                --source https://api.nuget.org/v3/index.json \
                --api-key {YOUR_API_KEY}
    * This will publish this package into nuget server provided in this script
    * Change versions in csproj files.