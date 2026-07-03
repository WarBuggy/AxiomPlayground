using AxiomPlayground.Modding.Discovery;
using Launcher.ModManagement;
using Launcher.Controls;

namespace Launcher;

public partial class Launcher : Form
{
    private List<ModGroup> _groups = [];

    public Launcher()
    {
        InitializeComponent();

        Shown += Launcher_Shown;

        panelAvailableMods.Resize += (_, _) =>
        {
            RelayoutModGroups();
        };
    }

    private void Launcher_Shown(object? sender, EventArgs e)
    {
        var discovery = new ModDiscovery();

        var mods = discovery.Discover();

        _groups = ModGroupBuilder.Build(mods);
        ValidateCoreExists(_groups);
        _groups = ModGroupOrdering.Order(_groups);
        // Console.WriteLine("=== Mod Groups ===");
        // foreach (var group in groups)
        // {
        //     Console.WriteLine($"Group: {group.ModId}");

        //     foreach (var mod in group.Mods)
        //     {
        //         Console.WriteLine($"    {mod.Info.Name} ({mod.Source})");
        //     }
        // }
        // Console.WriteLine();
        // Console.WriteLine("=== Saved Mod Selection States ===");

        var stateLookup = ModSelectionStore.Load();
        // if (stateLookup.Count == 0)
        // {
        //     Console.WriteLine("(none)");
        // }
        // else
        // {
        //     foreach (var (id, state) in stateLookup)
        //     {
        //         Console.WriteLine(
        //             $"ModId={id}, " +
        //             $"Source={state.SelectedSource}, " +
        //             $"Enabled={state.IsEnabled}");
        //     }
        // }
        foreach (var group in _groups)
        {
            stateLookup.TryGetValue(group.ModId, out var state);
            group.ResolveSelection(state);
        }

        RenderModGroups(_groups);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        var states = new Dictionary<string, ModSelectionState>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in _groups)
        {
            states[group.ModId] = new ModSelectionState
            {
                ModId = group.ModId,
                SelectedSource = group.SelectedSource,
                IsEnabled = group.IsEnabled
            };
        }

        ModSelectionStore.Save(states);
    }

    private void RenderModGroups(IEnumerable<ModGroup> groups)
    {
        panelAvailableMods.SuspendLayout();
        panelAvailableMods.Controls.Clear();

        int y = 0;
        int width = panelAvailableMods.ClientSize.Width;

        foreach (var group in groups)
        {
            var control = new ModGroupControl();
            control.Bind(group);

            control.Width = width;
            control.Location = new Point(0, y);
            control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            panelAvailableMods.Controls.Add(control);

            y += control.Height;
        }

        panelAvailableMods.ResumeLayout();
    }

    private static void ValidateCoreExists(IEnumerable<ModGroup> groups)
    {
        bool hasCore = groups.Any(g =>
            g.ModId.Equals(ModSystemPolicy.CORE_MOD_ID, StringComparison.OrdinalIgnoreCase));

        if (!hasCore)
        {
            throw new InvalidOperationException(Shared.T("errorLauncherNoCore", ModSystemPolicy.CORE_MOD_ID));
        }
    }

    private void RelayoutModGroups()
    {
        int y = 0;
        int width = panelAvailableMods.ClientSize.Width;

        foreach (Control c in panelAvailableMods.Controls)
        {
            c.Width = width;
            c.Location = new Point(0, y);

            y += c.Height;
        }
    }
}