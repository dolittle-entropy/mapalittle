using MapEvents.Processors;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.Threading.Tasks;

namespace MapEvents.CommandLine
{
    [Command("events", Description ="Map all events to their EventHandlers into the selected output format")]
    [VersionOption("mapalittle events v1.0.2")]
    public class Events : CommandBase
    {
        [Option(Description = "Set this flag to ignore issues found")]
        public bool IgnoreEvents { get; set; }

        public Task OnExecute(CommandLineApplication app)
        {
            app.ShowVersion();
            Console.WriteLine(Environment.NewLine);
            PrintChoicesMade();
            Out.Dump($"Performing DLL discovery...");
            DllDiscovery.LoadDllFiles(InputFolder);


            var eventType = DllDiscovery.FindEventTypeInDlls();
            if (!eventType.Found)
            {
                Out.Fail("No event types found. Is this a Dolittle project output folder?");
                return Task.CompletedTask;
            }

            DllDiscovery.FindAllEvents();
            DllDiscovery.MapEventsToEventHandlers();

            if (!IgnoreEvents)
            {
                DllDiscovery.ReportIssues();
            }

            DllDiscovery.WriteOutputFiles(OutputFolder, PreferJson);
            Out.Dump("Event mapping completed", overline: true);

            return Task.CompletedTask;
        }

        private void PrintChoicesMade()
        {                                                          
            Out.Dump(PreferJson ? "Output type  : JSON" : "Output type  : CSV");
            if (string.IsNullOrEmpty(InputFolder))
            {
                         
                Out.Dump("Input Folder : Current Directory");
            }
            else
            {
                Out.Dump($"Input Folder : {InputFolder}");
            }

            if (string.IsNullOrEmpty(OutputFolder))
            {
                Out.Dump("Output folder: Current Directory");
            }
            else
            {
                Out.Dump($"Output folder: {OutputFolder}", underline: true);
            }            
        }
    }
}
