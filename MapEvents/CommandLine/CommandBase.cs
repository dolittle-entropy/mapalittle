using McMaster.Extensions.CommandLineUtils;
using System;

namespace MapEvents.CommandLine
{
    public abstract class CommandBase
    {
        [Option(Description = "Specify folder with Dolittle application DLLs. Defaults to current folder", ShortName = "d")]
        [DirectoryExists]
        public string InputFolder { get; }

        [Option(Description = "Output folder for Node and Edge files")]
        [DirectoryExists]
        public string OutputFolder { get; }

        [Option(Description = "Set this flag to produce JSON output. If unset, CSV files will be produced instead", ShortName = "j")]
        public bool PreferJson { get; }

        private void OnExecute()
        {
            Console.WriteLine("Gotta choose a command!");
        }
    }
}
