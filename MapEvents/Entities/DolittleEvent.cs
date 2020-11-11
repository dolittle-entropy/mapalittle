using System.Collections.Generic;

namespace MapEvents.Entities
{
    public class DolittleEvent : IdentifiedEntity
    {
        public DolittleEvent()
        {
            Type = "Event";
        }

        public string NameSpace { get; set; }

        public string AssemblyPath { get; set; }

        public string AssemblyName { get; set; }

        public List<DolittleEventHandler> Handlers { get; set; } = new List<DolittleEventHandler>();
    }
}
