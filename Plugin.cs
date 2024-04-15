using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace NoPenaltyReimagined;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string PLUGIN_GUID = "io.daxcess.nopenaltyreimagined";
    private const string PLUGIN_NAME = "NoPenaltyReimaged";
    private const string PLUGIN_VERSION = "1.0.0";

    private void Awake()
    {
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

        Logger.LogInfo("Let's commit some tax fraud!");
    }
}

[HarmonyPatch]
internal static class NoPenaltyPatches
{
    /// <summary>
    /// Remove the penalty, UI and delay
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGame), MethodType.Enumerator)]
    [HarmonyTranspiler]
    [HarmonyDebug]
    private static IEnumerable<CodeInstruction> RemovePenalty(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator)
            .MatchForward(false,
                [new CodeMatch(OpCodes.Callvirt, Method(typeof(HUDManager), nameof(HUDManager.ApplyPenalty)))])
            .Advance(-10);
            
        var position = matcher.Operand;
        return matcher.Advance(1).Insert(new CodeInstruction(OpCodes.Br, position)).InstructionEnumeration();
    }

    /// <summary>
    /// Force sync credits with clients that do not have the mod installed
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.AllPlayersHaveRevivedClientRpc))]
    [HarmonyPostfix]
    private static void SyncTerminalCredits(StartOfRound __instance)
    {
        if (!__instance.NetworkManager.IsHost && !__instance.NetworkManager.IsServer)
            return;
        
        var terminal = Object.FindObjectOfType<Terminal>();
        terminal.SyncGroupCreditsServerRpc(terminal.groupCredits, terminal.numberOfItemsInDropship);
    }
}