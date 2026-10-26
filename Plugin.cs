using BepInEx;
using BepInEx.Configuration;

using HarmonyLib;

using Jotunn.Managers;
using Jotunn.Utils;

namespace BenchLink;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid)]
[NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
public class BenchLinkPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "javidahmed64592.benchlink";
    public const string PluginName = "Bench Link";
    public const string PluginVersion = "0.1.0";

    internal static BepInEx.Logging.ManualLogSource Log { get; private set; }

    private readonly RangeIndicator _indicator = new();

    private Harmony _harmony;

    private void Awake()
    {
        Log = Logger;
        BenchLinkConfig.Bind(Config);

        _harmony = new Harmony(PluginGUID);
        _harmony.PatchAll();

        CommandManager.Instance.AddConsoleCommand(new BenchLinkCommand());
    }

    private void Update()
    {
        _indicator.Tick(UnityEngine.Time.deltaTime);
        PlacementNotice.Tick();
    }

    private void OnDestroy()
    {
        _indicator.Hide();
        _harmony?.UnpatchSelf();
    }
}

internal enum IndicatorMode
{
    Off = 0,
    WhileBuilding = 1
}

internal static class BenchLinkConfig
{
    public static ConfigEntry<float> LinkSlack;
    public static ConfigEntry<IndicatorMode> Indicator;

    public static void Bind(ConfigFile config)
    {
        LinkSlack = config.Bind("General", "LinkSlack", 0f,
            new ConfigDescription("Extra metres added when deciding whether two stations are linked. " +
                "Stations are linked when they are closer than the larger of their two build ranges plus this.",
                new AcceptableValueRange<float>(0f, 20f)));
        Indicator = config.Bind("Look", "Indicator", IndicatorMode.WhileBuilding,
            "Show the build range of every linked station while the hammer is out. Off removes it.");
    }
}
