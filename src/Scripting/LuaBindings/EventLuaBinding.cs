using MoonSharp.Interpreter;

namespace AxiomPlayground.Scripting.LuaBindings;

public sealed class EventLuaBinding(LuaEventBus eventBus)
{
    private readonly LuaEventBus _eventBus = eventBus;

    public void Register(Script script)
    {
        var eventsTable = new Table(script);

        eventsTable["Register"] = (Action<string, DynValue>)((eventName, fn) =>
        {
            if (fn.Type != DataType.Function)
                throw new ScriptRuntimeException("EventLuaBinding] Event handler must be a function.");

            _eventBus.Register(eventName, ScriptManager.Instance.CurrentExecutingModId, fn.Function);
        });

        script.Globals["Events"] = eventsTable;
    }
}
