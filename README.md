[![Nuget](https://img.shields.io/nuget/v/NuGetPushTool)](https://www.nuget.org/packages/NuGetPushTool/)

# Problem Statement
I've not found a really easy way to push updated packages to NuGet.org. You can of course follow [Microsoft's own guidance](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package#use-the-dotnet-cli). I feel like there's too much admin and manual steps in this approach.

I've used CI solutions like [AppVeyor](https://www.appveyor.com/) successfully, but I find AppVeyor kind of hard to setup. It has a maze settings -- I struggle with it. Although I've gotten it to work for some things, I've also found myself unable to get other projects working, and I couldn't figure out why. I've ended up doing it manually through NuGet.org's manual upload UI. I got tired of doing that, so I wanted to take a fresh look automating it in a console app. I'd like to be able to navigate to a package build directory and enter a command like this:

```cmd
nugetpush
```
The program should find the packages in the current directory along with your API key and push your packages and symbols, if present.

# Get Started
1. Install the tool package globally:
```
dotnet tool install --global NuGetPushTool --version 1.0.1
```
2. If you don't have one, create an API key at NuGet.org. Info about this is [here](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package#create-an-api-key).

3. Create a json file called `nugetpush.json` in a directory above your .NET projects. For example, if your projects are in directories below `%userprofile%\source\repos`, then create `nugetpush.json` in that folder. At minimum your file should look like the example below. This file is based on this [Options model](https://github.com/adamfoneil/NuGetPush/blob/master/NuGetPush.CLI/Options.cs). Put your API key in the `ApiKey` property.

<details>
  <summary>Example</summary>

```
{
    "ApiKey": "<your key>"
}
```
</details>

You should now be able to push packages from any directory on your machine that has NuGet packages. Navigate to the directory in a terminal window and type `nugetpush`.

# Code Tour
- [Options](https://github.com/adamfoneil/NuGetPush/blob/master/NuGetPush.CLI/Options.cs) defines available command line options
- [Program.cs](https://github.com/adamfoneil/NuGetPush/blob/master/NuGetPush.CLI/Program.cs) shows the high-level flow
- [Program_methods.cs](https://github.com/adamfoneil/NuGetPush/blob/master/NuGetPush.CLI/Program_methods.cs) has the low-level implementation
