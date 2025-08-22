# Getting Started

Welcome to the Uno Platform!

To discover how to get started with your new app: https://aka.platform.uno/get-started

For more information on how to use the Uno.Sdk or upgrade Uno Platform packages in your solution: https://aka.platform.uno/using-uno-sdk

## Create an unsigned packaged app

### Package the unsigned app

`PublishTrimmed` option doesn't work, so create a package with `PublishTrimmed=false`

```Shell
cd "./src/Eppie.Prototypes/AccountSettings/AccountSettings"

msbuild /r /p:TargetFramework=net9.0-windows10.0.26100 /p:Configuration=Release /p:Platform=x64 /p:GenerateAppxPackageOnBuild=true /p:UapAppxPackageBuildMode=Sideloading /p:AppxPackageDir="./bin/publish/" /p:AppxPackageSigningEnabled=false /p:PublishTrimmed=false /p:PublishReadyToRun=true
```

### Install the unsigned Windows app

Start the PowerShell command prompt as administrator and run the following commands:

```PowerShell
cd "<publish dir>"
Add-AppPackage -AllowUnsigned ".\AccountSettings_<version>.appx"
```
