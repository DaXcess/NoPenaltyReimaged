using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace NoPenaltyReimagined;

[HarmonyPatch]
internal static class NoPenaltyPatches
{
    /// <summary>
    /// Handle the initial setup for the tax fraud, to make sure we're either the host or the host has the mod
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    [HarmonyPostfix]
    private static void OnGameEnter(StartOfRound __instance)
    {
        // Disabled by default
        Plugin.NoPenaltyEnabled = false;

        var isHost = __instance.NetworkManager.IsHost || __instance.NetworkManager.IsServer;

        // On LAN, disable NoPenalty if we're not the host
        // Any credit desync will be fixed by `SyncTerminalCredits` anyway (if the host has NoPenalty installed)
        if (GameNetworkManager.Instance.currentLobby is not { } lobby)
        {
            Plugin.NoPenaltyEnabled = isHost;
            return;
        }

        if (isHost)
        {
            // We're the host, enable no penalty
            Plugin.NoPenaltyEnabled = true;

            Logger.LogDebug("Setting lobby data \"NoPenaltyPresent\" = \"true\"");
            lobby.SetData("NoPenaltyPresent", "true");
            return;
        }

        Logger.LogDebug("Checking if host has NoPenalty installed");
        if (lobby.GetData("NoPenaltyPresent") != "true") return;
        
        // Host has no penalty installed, enable it locally as well
        Plugin.NoPenaltyEnabled = true;
        Logger.LogDebug("Host has NoPenalty installed, we good to commit tax fraud!");
    }

    /// <summary>
    /// Remove the penalty, UI and delay, as long as <see cref="Plugin.NoPenaltyEnabled"/> is set to <c>true</c>
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGame), MethodType.Enumerator)]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> RemovePenalty(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator)
            .MatchForward(false,
                [new CodeMatch(OpCodes.Callvirt, Method(typeof(HUDManager), nameof(HUDManager.ApplyPenalty)))])
            .Advance(-10);

        var position = matcher.Operand;
        return matcher.Advance(1).InsertAndAdvance(
                new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(Plugin), nameof(Plugin.NoPenaltyEnabled))),
                new CodeInstruction(OpCodes.Brtrue, position)
            )
            .InstructionEnumeration();
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