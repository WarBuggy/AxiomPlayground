namespace AxiomPlayground.GameFlag;

public interface IGameFlag
{
    string Name { get; }
}

public abstract class GameFlag(string name) : IGameFlag
{
    public string Name { get; } = name.ToLowerInvariant();

    public override string ToString() => Name;
}