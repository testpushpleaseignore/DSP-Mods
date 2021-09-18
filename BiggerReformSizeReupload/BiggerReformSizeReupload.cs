using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace BiggerReformSizeReupload {
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    [BepInProcess("DSPGame.exe")]
    public class BiggerReformSize : BaseUnityPlugin {
        public const string pluginGuid = "me.xiaoye97.plugin.Dsyon.BiggerReformSize";
        public const string pluginName = "BiggerReformSize";
        public const string pluginVersion = "1.2";

        private const int size = 20;

        private Harmony harmony;
        internal static ManualLogSource logger;
        public static BiggerReformSize instance;

        private void Awake() {
            logger = base.Logger;
            instance = this;
            harmony = new Harmony(pluginGuid);

            try { harmony.PatchAll(typeof(BiggerReformSize)); }
            catch (Exception e) { Logger.LogError($"Harmony patching failed: {e.Message}"); }
        }

        private void OnDestroy() {
            harmony.UnpatchSelf();
            instance = null;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BuildTool_Reform), MethodType.Constructor)]
        public static void SizePatch(BuildTool_Reform __instance) {
            __instance.cursorIndices = new int[size * size];
            __instance.cursorPoints = new UnityEngine.Vector3[size * size];
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(BuildTool_Reform), "ReformAction")]
        public static IEnumerable<CodeInstruction> SizePatch2(IEnumerable<CodeInstruction> instructions) {
            UnityEngine.Debug.Log("[BiggerReformSize]Patch BuildTool_Reform.ReformAction");
            List<CodeInstruction> codes = instructions.ToList();

            codes[24].opcode = OpCodes.Ldc_I4_S;
            codes[24].operand = size;
            codes[27].opcode = OpCodes.Ldc_I4_S;
            codes[27].operand = size;
            codes[75].opcode = OpCodes.Ldc_I4_S;
            codes[75].operand = size;
            return codes.AsEnumerable();
        }
    }
}
