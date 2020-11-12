using ConsoleTables;
using MapEvents.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MapEvents.Processors
{
    public static class DllDiscovery
    {
        const int EVENTHANDLERS_START_ID = 50_000;
        const int EVENTS_START_ID        = 10_000;

        static List<DolittleEventHandler> _dolittleEventHandlers = new List<DolittleEventHandler>();
        static List<DolittleEvent>        _dolittleEvents        = new List<DolittleEvent>();
        static List<Assembly>             _loadedAssemblies      = new List<Assembly>();
        static EventTypeIdentity          _eventTypeIdentity;

        public static void LoadDllFiles(string inputFolder)
        {
            var stopwatch       = Stopwatch.StartNew();
            var realInputFolder = Directory.GetCurrentDirectory();

            if (!string.IsNullOrEmpty(inputFolder) && Directory.Exists(inputFolder))
            {
                realInputFolder = inputFolder;
            }
            foreach (var assemblyFile in Directory.GetFiles(realInputFolder, "*.dll"))
            {
                var fileInfo = new FileInfo(assemblyFile);
                if (fileInfo.Name.StartsWith("System.") || fileInfo.Name.StartsWith("Microsoft."))
                {
                    continue;
                }
                Assembly assembly = null;
                try
                {
                    assembly = Assembly.LoadFrom(assemblyFile);
                }
                catch{
                }
                if (assembly is { })
                {
                    var name = assembly.GetName().Name;
                    if (name.StartsWith("System") || name.StartsWith("Microsoft"))
                        continue;
                    _loadedAssemblies.Add(assembly);
                }
            }
            Out.Dump($"{_loadedAssemblies.Count} assemblies loaded from '{realInputFolder}' in {stopwatch.ElapsedMilliseconds}ms");
        }

        public static EventTypeIdentity FindEventTypeInDlls()
        {
            const string OldSchoolEventType = "IEvent";

            foreach (var assembly in _loadedAssemblies)
            {
                var assemblyName = assembly.GetName().Name;
                try
                {
                    foreach (var type in assembly.DefinedTypes)
                    {
                        if (IsEventType(type))
                        {
                            _eventTypeIdentity = new EventTypeIdentity(null);
                            Out.Dump($"Found EventType Attribute. Assuming solution uses Dolittle.SDK >= v6.0.0");
                            break;
                        }
                        else // No attribute, do we use oldschool IEvent inheritance?
                        {
                            if (type.Name.Equals(OldSchoolEventType, StringComparison.InvariantCultureIgnoreCase) && type.IsInterface)
                            {
                                Out.Dump($"Found OldSchool Type '{type.Name}'. Assuming solution uses Dolittle.SDK < v6.0.0");
                                _eventTypeIdentity = new EventTypeIdentity(type, oldSchool: true);
                                break;
                            }
                        }
                    }
                }
                catch{
                }
                if (_eventTypeIdentity is { })
                {
                    break;
                }
            }
            return _eventTypeIdentity ?? new EventTypeIdentity(null, found: false);
        }
        
        public static object FindAllEvents()
        {
            if (_eventTypeIdentity.IsOldSchool)
            {
                Out.Dump($"Searching for Events of type '{_eventTypeIdentity.EventType.Name}'", underline: true);
                DiscoverOldSchoolEvents();
            }
            else
            {
                Out.Dump("Searching for Events marked with 'EventType' attribute", underline: true);
                DiscoverEvents();
            }
            Out.Dump($"{_dolittleEvents.Count} Events discovered");
            return null;
        }
        
        public static void MapEventsToEventHandlers()
        {
            if (_eventTypeIdentity.IsOldSchool)
            {
                MapOldSchoolEventHandlers();
            }
            else
            {
                MapEventHandlers();
            }
            Out.Dump($"{_dolittleEventHandlers.Count} EventHandler{(_dolittleEventHandlers.Count == 1 ? "" : "s")} discovered");
        }
        
        public static void ReportIssues()
        {
            ReportUnusedEvents();
            ReportUnusedEventHandlers();
        }

        public static void WriteOutputFiles(string outputFolder, bool preferJson)
        {
            string realOutputFolder;

            if (string.IsNullOrEmpty(outputFolder))
            {
                realOutputFolder = Directory.GetCurrentDirectory();
            }
            else
            {
                realOutputFolder = outputFolder;
            }

            if (!Directory.Exists(realOutputFolder))
            {
                Out.Fail($"Destination folder '{realOutputFolder}' does not exist");
            }

            foreach (var evt in _dolittleEvents)
            {
                evt.Weight = evt.Handlers.Count;
            }

            foreach (var handler in _dolittleEventHandlers)
            {
                handler.Weight = handler.Events.Count;
            }

            var entities = _dolittleEvents.Select(e => (IdentifiedEntity) e).ToList();
            entities.AddRange(_dolittleEventHandlers.Select(h => h as IdentifiedEntity));

            if (preferJson)
            {
                OutputJsonFiles(realOutputFolder, entities);
            }
            else
            {
                OutputCsvFiles(realOutputFolder, entities);
            }
        }

        private static void DiscoverEvents()
        {
            var counter = EVENTS_START_ID;
            foreach (var assembly in _loadedAssemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types.Where(t => IsEventType(t)))
                    {
                        _dolittleEvents.Add(new DolittleEvent
                        {
                            Id = ++counter,
                            Name = type.Name,
                            NameSpace = type.Namespace,
                            AssemblyName = assembly.GetName().Name,
                            AssemblyPath = assembly.Location
                        });
                    }
                }
                catch{
                }
            }
        }

        private static void DiscoverOldSchoolEvents()
        {
            foreach (var assembly in _loadedAssemblies)
            {
                try
                {
                    var definedTypes = assembly.DefinedTypes;
                    int counter = EVENTS_START_ID;
                    foreach (var matchingType in definedTypes.Where(e => _eventTypeIdentity.EventType.IsAssignableFrom(e)))
                    {
                        if (!matchingType.IsInterface)
                        {
                            _dolittleEvents.Add(new DolittleEvent
                            {
                                Id = ++counter,
                                AssemblyName = assembly.GetName().Name,
                                AssemblyPath = assembly.Location,
                                Name = matchingType.Name,
                                NameSpace = matchingType.Namespace
                            });
                        }
                    }
                }
                catch{
                }
            }
        }

        private static List<Edge> GetAllEdges()
        {
            var edges = new List<Edge>();
            foreach (var handler in _dolittleEventHandlers)
            {
                foreach (var evt in handler.Events)
                {
                    edges.Add(new Edge
                    {
                        From = evt.Id,
                        To = handler.Id
                    });
                }
            }
            return edges;
        }

        private static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t is { });
            }
        }

        private static Type GetOldSchoolEventHandlerType()
        {
            const string eventHandlerInterface = "ICanHandleEvents";
            foreach (var assembly in _loadedAssemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    var foundHandler = types.FirstOrDefault(t => t.Name.Equals(eventHandlerInterface) && t.IsInterface);
                    if (foundHandler is { })
                    {
                        return foundHandler;
                    }
                }
                catch{
                }
            }
            return null;
        }
        
        private static bool IsEventType(Type type)
        {
            const string eventTypeAttributeName = "EventTypeAttribute";
            try
            {
                var typeName = type.Name;
                if (typeName.StartsWith("Microsoft") || typeName.StartsWith("System"))
                    return false;

                var allAttributes = type.GetCustomAttributes();
                return allAttributes.Any(a => a.TypeId.ToString().Contains(eventTypeAttributeName));
            }
            catch (Exception ex)
            {
                Out.Fail(ex.Message);
            }
            return false;
        }

        private static bool IsEventHandlerType(Type type)
        {
            const string eventTypeAttributeName = "EventHandlerAttribute";
            try
            {
                var typeName = type.Name;
                if (typeName.StartsWith("Microsoft") || typeName.StartsWith("System"))
                    return false;
                var allAttributes = type.GetCustomAttributes();
                return allAttributes.Any(a => a.TypeId.ToString().Contains(eventTypeAttributeName));
            }
            catch (Exception ex)
            {
                Out.Fail(ex.Message);
            }
            return false;
        }

        private static void MapEventHandlers()
        {
            int counter = EVENTHANDLERS_START_ID;
            foreach (var assembly in _loadedAssemblies)
            {
                try
                {
                    var types = assembly.GetLoadableTypes();
                    var assemblyName = assembly.GetName().Name;

                    foreach (var type in types)
                    {
                        if (type.IsGenericType || type.IsInterface)
                            continue;
                        var typeName = type.Name;

                        if (IsEventHandlerType(type))
                        {
                            var eventHandler = new DolittleEventHandler
                            {
                                Id = ++counter,
                                Name = type.Name,
                                NameSpace = type.Namespace,
                                AssemblyName = assembly.GetName().Name,
                                AssemblyPath = assembly.Location
                            };

                            MapEventsToEventHandler(eventHandler, type);

                            _dolittleEventHandlers.Add(eventHandler);
                        }
                    }
                }
                catch{
                }
            }
        }

        private static void MapEventsToEventHandler(DolittleEventHandler eventHandler, Type handlerType)
        {
            const string eventContext = "EventContext";
            foreach (var method in (MethodInfo[])handlerType.GetMethods())
            {
                var parameters = method.GetParameters();

                // Find a method that takes an EventContext as part of it's parameter list
                if (parameters.Any(p => p.ParameterType.Name.Equals(eventContext)))
                {
                    if (_eventTypeIdentity.EventType is { })
                    {
                        var matchingEvent = parameters.FirstOrDefault(p => _eventTypeIdentity.EventType.IsAssignableFrom(p.ParameterType));
                        if (matchingEvent is { })
                        {
                            var matchingEventName = matchingEvent.ParameterType.Name;
                            var dolittleEvent = _dolittleEvents.First(e => e.Name.Equals(matchingEventName, StringComparison.InvariantCultureIgnoreCase));

                            if (!dolittleEvent.Handlers.Any(h => h.Name.Equals(eventHandler.Name)))
                            {
                                dolittleEvent.Handlers.Add(eventHandler);
                                eventHandler.Events.Add(dolittleEvent);
                            }
                        }
                    }
                    else
                    {
                        var parameterTypeNames = parameters.Select(p => p.ParameterType.Name);
                        foreach (var typeName in parameterTypeNames)
                        {
                            var matchingEvent = _dolittleEvents.FirstOrDefault(e => e.Name.Equals(typeName));
                            if (matchingEvent is { })
                            {
                                if (!eventHandler.Events.Any(e => e.Name == eventHandler.Name))
                                    eventHandler.Events.Add(matchingEvent);

                                if (!matchingEvent.Handlers.Any(h => h.Name == eventHandler.Name))
                                    matchingEvent.Handlers.Add(eventHandler);
                            }
                        }
                    }
                }
            }
        }

        private static bool MapOldSchoolEventHandlers()
        {
            var eventHandlerType = GetOldSchoolEventHandlerType();
            if (eventHandlerType is null)
            {
                Out.Fail("No EventHandler type found");
                return false;
            }

            foreach (var assembly in _loadedAssemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    var matches = types.Where(t => eventHandlerType.IsAssignableFrom(t));
                    if (matches?.Any() ?? false)
                    {
                        int counter = EVENTHANDLERS_START_ID;
                        foreach (var match in matches)
                        {
                            if (!match.IsInterface)
                            {
                                var eventHandler = new DolittleEventHandler
                                {
                                    Id = ++counter,
                                    Name = match.Name,
                                    AssemblyName = assembly.GetName().Name,
                                    AssemblyPath = assembly.Location,
                                    NameSpace = match.Namespace
                                };
                                MapEventsToEventHandler(eventHandler, match);
                                _dolittleEventHandlers.Add(eventHandler);
                            }
                        }
                    }
                }
                catch{
                }
            }
            return true;
        }

        private static void OutputCsvFiles(string realOutputFolder, List<IdentifiedEntity> entities)
        {
            const string nodesFilename = "graphNodes.csv";
            const string edgesFilename = "graphEdges.csv";

            var fileName = Path.Combine(realOutputFolder, nodesFilename);

            var lines = new List<string>();
            lines.Add("Id;Name;Type;Weight");
            foreach (var entity in entities)
            {
                lines.Add($"{entity.Id};{entity.Name};{entity.Type};{entity.Weight}");
            }
            File.WriteAllLines(fileName, lines);
            Out.Dump($"Nodes exported to: {fileName}", 1);

            var edges = GetAllEdges();
            lines = new List<string>();
            lines.Add("From;To");
            foreach (var edge in edges)
            {
                lines.Add($"{edge.From};{edge.To}");
            }
            fileName = Path.Combine(realOutputFolder, edgesFilename);
            File.WriteAllLines(fileName, lines);
            Out.Dump($"Edges exported to: {fileName}", 1);
        }

        private static void OutputJsonFiles(string realOutputFolder, List<IdentifiedEntity> entities)
        {
            const string nodesFileName = "graphNodes.json";
            const string edgesFileName = "graphEdges.json";

            var newStuff = entities.Select(e => new IdentifiedEntity { Id = e.Id, Name = e.Name, Type = e.Type, Weight = e.Weight}).ToArray();
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            var jsonEntities = JsonConvert.SerializeObject(newStuff);
            var fileName = Path.Combine(realOutputFolder, nodesFileName);
            File.WriteAllText(fileName, jsonEntities);
            Out.Dump($"Nodes exported to: {fileName}", 1);

            var edges = GetAllEdges();
            var jsonEdges = JsonConvert.SerializeObject(edges, settings);
            fileName = Path.Combine(realOutputFolder, edgesFileName);
            File.WriteAllText(fileName, jsonEdges);
            Out.Dump($"Edges exported to: {fileName}", 1);
        }

        private static void ReportUnusedEventHandlers()
        {
            var unmappedEventHandlers = _dolittleEventHandlers.Where(e => e.Events.Count == 0);
            if (unmappedEventHandlers.Any())
            {
                Out.Warn($"{unmappedEventHandlers.Count()} EventHandlers do not appear to process any Events:");
                Console.WriteLine(Environment.NewLine);

                var consoleTable = new ConsoleTable("Id", "EventHandler", "Namespace", "Assembly");
                foreach (var unmappedEventHandler in unmappedEventHandlers)
                {
                    consoleTable.AddRow(
                        unmappedEventHandler.Id.ToString("######"),
                        unmappedEventHandler.Name,
                        unmappedEventHandler.NameSpace,
                        unmappedEventHandler.AssemblyName + ".dll");
                }
                consoleTable.Write(Format.Minimal);
            }
        }

        private static void ReportUnusedEvents()
        {
            var unmappedEvents = _dolittleEvents.Where(e => e.Handlers.Count == 0);
            if(unmappedEvents.Any())
            {
                Out.Warn($"{unmappedEvents.Count()} Events were not found in any EventHandlers:", 0);
                Console.WriteLine(Environment.NewLine);

                var consoleTable = new ConsoleTable("Id", "Event", "Namespace", "Assembly");
                foreach (var unmappedEvent in unmappedEvents)
                {
                    consoleTable.AddRow(
                        unmappedEvent.Id.ToString("######"),
                        unmappedEvent.Name,
                        unmappedEvent.NameSpace,
                        unmappedEvent.AssemblyName + ".dll");
                }
                consoleTable.Write(Format.Minimal);
            }
        }
    }
}