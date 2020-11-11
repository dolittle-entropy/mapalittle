using System.Collections.Generic;

namespace MapEvents.Entities
{
    public class DolittleEventHandler : IdentifiedEntity
    {
        public DolittleEventHandler()
        {
            Type = "EventHandler";
        }

        public string NameSpace { get; set; }

        public string AssemblyPath { get; set; }

        public string AssemblyName { get; set; }

        public List<DolittleEvent> Events { get; set; } = new List<DolittleEvent>();
    }
}
