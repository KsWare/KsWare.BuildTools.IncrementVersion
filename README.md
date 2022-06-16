# KsWare.BuildTools.IncrementVersion

A tool which increments the build number bevor build.

With this tool you can apply [semantic versioning](https://semver.org/lang/de/).

The idea behind is to define the Major/Minor version on demand in a centrazied location and increment the Build number on each (release) build without the need to change each version property in each project.

Because you (should) use always a change log file, we use this to define the version number. 

Supports projects with the SDK format, including [MAUI projects](#MAUI-projects).  
See also [Identify the project format](https://docs.microsoft.com/en-us/nuget/resources/check-project-format).

Currently it is provided to works with single project solutions, or more precisely, with one change log file per project. 
This means you cannot increment multiple projects with only one ChangeLog file.

And Yes, IncrementVersion uses IncrementVersion to increment the version. 😆

## Usage

### 1. Adapt project file

After [installing](#installation) as a global tool you can add a build target

```xml
<Target Name="IncrementVersion" BeforeTargets="PrepareForBuild" Condition="$(Configuration)=='Release'">
    <Exec Command="KsWare.BuildTools.IncrementVersion $(ProjectPath) -f $(SolutionDir)..\ChangeLog.md" />
</Target>
```
Exec Command: `KsWare.BuildTools.IncrementVersion <project> [-f <path>][-t]`

| Parameter | Description
|:--|:---|
| \<project>    | path to the project file
| -f \<path>   | path to file which contains the version
| -t or -test  | No changes will be writen. Use this for test purposes.

Don't forget to add/configure any of the version properties in your project file. 
I suggest to use `VersionPrefix`. 
Only properties that are present are changed! 
Properties containing macros will not be changed.

### 2. The ChangeLog file
 - Create a simple Changelog.md file.  
 - Check if the  `-f path` in the target points to this file!  
 - Specify the major/minor version on top of file.

```
# 1.0
- first public version
```

As soon as you set the configuration to "Release", the version is incremented ONCE and also written into the ChangeLog.md.

For the next build number simple add your changes on top of the ChangeLog.md file. 
```
- bugfixes
# 1.0.0
```
As soon as a new release version is created, the current version is added.
```
# 1.0.1
- bugfixes
# 1.0.0
```
To setup a new major/minor version, simple add the version on top, like in the first step.
```
# 2.0
- A new major version is born
# 1.0.1
...
```
#### Options


## Installation

Install from NuGet.org

```CLI
dotnet tool install KsWare.BuildTools.IncrementVersion -g
```

Install from local directory

```CLI
dotnet tool install KsWare.BuildTools.IncrementVersion -g --add-source .
```
For more options see [dotnet tool install](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install)

Global tools are installed in the following directories by default when you specify the -g or --global option:

| Operating system |	Path
|:--|:--
| Linux/macOS |	$HOME/.dotnet/tools
| Windows |	%USERPROFILE%\.dotnet\tools

## MAUI Projects

MAUI Properties are using other version properties. 

 - `ApplicationDisplayVersion` contains the version number like '1.0.1' and is processed as usual like the `VersionPrefix`. 

 - `ApplicationVersion` is a integer. This is also incremented with each release build.

 ## Additional usage

 You can use this tool per CLI also without a change log file.

 `KsWare.BuildTools.IncrementVersion.exe <project> [-v <version>][-t]`

 | Parameter | Description
|:--|:---|
| \<project>    | path to the project file
| -v \<version>   | the desired version (major/minor)
| -t or -test  | No changes will be writen. Use this for test purposes.

# Naming
For reasons of uniqueness and to avoid naming conflicts with other tools I finally decided to use the full name `KsWare.BuildTools.IncrementVersion` also for the tool name and for the binary file.
As a short name I will remember `KsIncVer` but maybe I can think of a better one.