# Problem Statement
I've not found a really easy way to push updated packages to NuGet.org. I've used CI solutions like [AppVeyor](https://www.appveyor.com/) successfully, but I find AppVeyor kind of hard to setup. It has a ton of settings -- and to me therefore a discoverability problem. Although I've gotten it to work for some things, I've also found myself unable to get other projects working, and I couldn't figure out why. I've ended up doing it manually through NuGet.org's manual upload UI. I got tired of doing it manually, so I wanted to take a fresh look automating it in a console app. I figured it should be straightforward to take advantage of Dotnet's post build event to run a console app to push updated packages. I do something like this already with [AzDeploy](https://github.com/adamfoneil/AzDeploy) for a few WinForms apps. I also use [Sleet](https://github.com/emgarten/Sleet) for pushing packages to a custom destination -- packages for personal use that are too specific for a wider NuGet.org audience. But lately I'm pivoting back to NuGet.org for some things, so Sleet is not ideal for that.

# Code Tour
- [Options](https://github.com/adamfoneil/NuGetPush/blob/master/NuGetPush.CLI/Options.cs) defines available command line options
- [Program.cs](https://github.com/adamfoneil/NuGetPush/blob/master/NuGetPush.CLI/Program.cs) shows the high-level flow
- [Program_methods.cs](https://github.com/adamfoneil/NuGetPush/blob/master/NuGetPush.CLI/Program_methods.cs) has the low-level implementation

# Remarks
I had a fair bit of ChatGPT help. See a couple convos I had:
- [this](https://chat.openai.com/share/a38c9aee-e6b8-413f-a651-a5daa0317c7b)
- and [this](https://chat.openai.com/share/af0af45d-25e9-466e-b366-bd9d0594546b)
This didn't give me fully working solutions, but it did give a lot of useful nudges and ideas.

Also, after some reflection, I'm not sure I want to use the Post Build event to trigger a NuGet push because -- as it is now -- it doesn't run any tests. That's a nice thing about CI tools like AppVeyor -- they run your tests before pushing packages.
