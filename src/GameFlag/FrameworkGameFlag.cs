namespace AxiomPlayground.GameFlag;

public sealed class FrameworkGameFlag : GameFlag
{
    private FrameworkGameFlag(string name) : base(name) { }

    public static readonly FrameworkGameFlag Debug = new("debug");

    // Future framework flags go here
}