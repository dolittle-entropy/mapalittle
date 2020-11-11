# mapalittle
Command line tool to generate nodes and edges data files from a Dolittle solution. Also reports on simple issues.

## Getting started
- Clone the Repository to your computer
- Edit the `MapEvents/MapEvents.csproj`file and update the `RuntimeIdentifier` flag to match your runtime
- build the project with `dotnet build`
- publish the project file using `dotnet publish -c release`
- Copy the single executable to somewhere in your **PATH**: 
```
fancyPrompt> copy bin\release\<your runtime identifier>\mapalittle(.exe) <yourtoolingpath>
```
- Start using `mapalittle`
```
fancyPrompt> mapalittle --help
```
