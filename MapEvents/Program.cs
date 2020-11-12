using MapEvents.CommandLine;
using MapEvents.Processors;
using McMaster.Extensions.CommandLineUtils;

namespace MapEvents
{
    [Command(Name = "mapalittle", Description ="Utility for mapping node/edge data and detecting simple issues")]
    [VersionOption("mapalittle v1.0.1 - Copyright 2020 Dolittle AS")]
    [Subcommand(typeof(Events))]
    public class Program
    {
        public static int Main(string[] args) 
            => CommandLineApplication.Execute<Program>(args);

        private void OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            Out.Fail("No command given.");
        }
    }
}
