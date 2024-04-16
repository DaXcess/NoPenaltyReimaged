using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace NoPenaltyReimagined;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string PLUGIN_GUID = "io.daxcess.nopenaltyreimagined";
    private const string PLUGIN_NAME = "NoPenaltyReimaged";
    private const string PLUGIN_VERSION = "1.1.0";
    
    public static bool NoPenaltyEnabled { get; internal set; }

    private void Awake()
    {
        NoPenaltyReimagined.Logger.SetSource(Logger);
        
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        Logger.LogInfo("Let's commit some tax fraud!");
    }
}