using MoonSharp.Interpreter;

namespace AxiomPlayground.Scripting.LuaBindings;

public sealed class LuaEventBus
{
    private readonly Dictionary<string, Dictionary<string, List<Closure>>> _handlers = [];

    public void Register(string eventName, string modId, Closure fn)
    {
        if (!_handlers.TryGetValue(eventName, out var modDict))
        {
            modDict = new Dictionary<string, List<Closure>>(StringComparer.OrdinalIgnoreCase);
            _handlers[eventName] = modDict;
        }

        if (!modDict.TryGetValue(modId, out var list))
        {
            list = [];
            modDict[modId] = list;
        }

        list.Add(fn);
    }


    /// <summary>
    /// Provides a read-only access to the handlers for a given event.
    /// Returns false if no handlers exist for the event.
    /// </summary>
    public bool TryGetHandlers(string eventName, out Dictionary<string, List<Closure>>? handlers)
    {
        return _handlers.TryGetValue(eventName, out handlers);
    }
}
