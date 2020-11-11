using MapEvents.CommandLine;
using McMaster.Extensions.CommandLineUtils;

using System;

namespace MapEvents
{
    [Command(Name = "mapalittle", Description ="Utility for mapping node/edge data and detecting simple issues")]
    [VersionOption("mapalittle v1.0.1 - Copyright 2020 Dolittle AS")]
    [Subcommand(
        typeof(Events)
        )]
    public class Program 
    {
        public static int Main(string[] args) 
            => CommandLineApplication.Execute<Program>(args);

        private void OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            Console.WriteLine("Gotta choose a command!");
        }
    }
}
