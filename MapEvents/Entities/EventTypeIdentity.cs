using System;

namespace MapEvents.Entities
{
    public class EventTypeIdentity
    {
        public bool IsOldSchool { get; }

        public Type EventType { get; }        

        public bool Found { get; }

        public EventTypeIdentity(Type eventType, bool oldSchool = false, bool found = true)
        {
            IsOldSchool = oldSchool;
            EventType   = eventType;
            Found       = found;
        }
    }
}
