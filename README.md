# Mapalittle
Command line tool to generate nodes and edges data files from a Dolittle solution. Also reports on simple issues.
The tool is written with multi-platform targets in mind, and should run fine on windows, linux and mac.
Please report any issues and/or feature requests here. 

## Getting started

### Requirements
- [Dotnet 5.0](https://dotnet.microsoft.com/download/dotnet/5.0)
- Some form of C# compiler and IDE

### Instructions
- Clone the Repository to your computer
- build the project with `dotnet build -c release`
- Include the released outputfolder `bin\release\<your platform>\Net5.0` in your path
- Start using `mapalittle` :
```
C:\> mapalittle --help
```

#### Sample Usage
Map events in the `MyProject` outputfolder and output the datafiles to the folder `C:\ReportData`. The output is written as JSON files.
```
C:\> mapalittle events -i "E:\dev\MyProject\bin\Debug\.Net5.0" -o "C:\ReportData" --prefer-json
```
The command above produces two files: 
* `C:\ReportData\graphNodes.json`
* `C:\ReportData\graphEdges.json`

> **NOTE on Assembly Name** <br />
> The assembly name was changed from **MapEvens** to **mapalittle**, which is why there is a small discrepancy between the project name and the output assembly name. <br />

> **NOTE on .Net 5.0** <br />
> This project is now built for `.Net 5.0` <br />
> At present, this means that the application cannot run as a self-contained application. This may change in the future.
