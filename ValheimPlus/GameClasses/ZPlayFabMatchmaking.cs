using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    [HarmonyPatch(typeof(ZPlayFabMatchmaking), "CreateLobby")]
    public static class ZPlayFabMatchmaking_CreateLobby_Transpiler
    {
        /// <summary>
        /// Alter playfab server player limit
        /// Must be between 2 and 32
        /// </summary>
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!Configuration.Current.Server.IsEnabled) return instructions;

            try
            {
                var ldc = instructions.Single(inst => inst.LoadsConstant() && inst.OperandIs(10U));
                ldc.operand = Helper.Clamp(Configuration.Current.Server.maxPlayers, 2, 32);
            }
            catch (Exception)
            {
                ZLog.LogError("Failed to alter lobby player limit (ZPlayFabMatchmaking_CreateLobby_Transpiler)");
            }
            return instructions;
        }
    }

    [HarmonyPatch(typeof(ZPlayFabMatchmaking), "CreateAndJoinNetwork")]
    public static class ZPlayFabMatchmaking_CreateAndJoinNetwork_Transpiler
    {
        /// <summary>
        /// Alter playfab network configuration player limit
        /// Must be between 2 and 32
        /// </summary>
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!Configuration.Current.Server.IsEnabled) return instructions;

            try
            {
                var ldc = instructions.Single(inst => inst.LoadsConstant() && inst.OperandIs(10U));
                ldc.operand = Helper.Clamp(Configuration.Current.Server.maxPlayers, 2, 32);
            }
            catch (Exception)
            {
                ZLog.LogError("Failed to alter network player limit (ZPlayFabMatchmaking_CreateAndJoinNetwork_Transpiler)");
            }

            return instructions;
        }
    }
}
