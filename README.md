# Problem Statement
I've not found a really easy way to push updated packages to NuGet.org. You can of course follow [Microsoft's own guidance](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package#use-the-dotnet-cli). This is a bit complicated IMO because you need to have the package filename and your API key handy. You can put these in a batch file, but there's too much admin there. (The package version number will keep changing, and you'd have to repeat this for all of your NuGet-hosted projects.)

I've used CI solutions like [AppVeyor](https://www.appveyor.com/) successfully, but I find AppVeyor kind of hard to setup. It has a ton of settings -- and to me therefore a discoverability problem. Although I've gotten it to work for some things, I've also found myself unable to get other projects working, and I couldn't figure out why. I've ended up doing it manually through NuGet.org's manual upload UI. I got tired of doing that, so I wanted to take a fresh look automating it in a console app. I'd like to be able to navigate to a package build directory and enter a command like this:

```cmd
nugetpush
```
The program should find the packages in the current directory along with your API key, which you've placed in a single, defined location out of source control (much like [Sleet](https://github.com/emgarten/Sleet) does). It should push your packages and symbols, if present.

# Get Started

# Code Tour
- [Options](https://github.com/adamfoneil/NuGetPush/blob/master/NuGetPush.CLI/Options.cs) defines available command line options
- [Program.cs](https://github.com/adamfoneil/NuGetPush/blob/master/NuGetPush.CLI/Program.cs) shows the high-level flow
- [Program_methods.cs](https://github.com/adamfoneil/NuGetPush/blob/master/NuGetPush.CLI/Program_methods.cs) has the low-level implementation
