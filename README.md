# mapalittle
Command line tool to generate nodes and edges data files from a Dolittle solution. Also reports on simple issues.

## Getting started
- Clone the Repository to your computer
- build the project with `dotnet build -c release`
- Include the released outputfolder `bin\release\<your platform>\Net5.0` in your path
- Start using `mapalittle` :
```
fancyPrompt> mapalittle --help
```

> **NOTE on Assembly Name** <br />
> The assembly name was changed from **MapEvens** to **mapalittle**, which is why there is a small discrepancy between the project name and the output assembly name. <br />

> **NOTE on .Net 5.0** <br />
> This project is now built for `.Net 5.0` <br />
> At present, this means that the application cannot run as a self-contained application. This may change in the future.
