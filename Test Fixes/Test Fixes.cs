
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace TestFixes {

	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	[BepInProcess("DSPGame.exe")]
	[module: UnverifiableCode]
	[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
	public class UpgradeAmount : BaseUnityPlugin {
		public const string pluginGuid = "testpostpleaseignore.dsp.testfixes";
		public const string pluginName = "Test_Fixes";
		public const string pluginVersion = "0.0.1";

		private Harmony harmony;

		public static UpgradeAmount instance;

		internal static ManualLogSource logger;
		new internal static BepInEx.Configuration.ConfigFile Config;

		private void Awake() {
			logger = base.Logger;
			Config = base.Config;

			Assert.Null(instance, $"An instance of {nameof(UpgradeAmount)} has already been created!");
			instance = this;

			harmony = new Harmony(pluginGuid);

			try { harmony.PatchAll(typeof(UpgradeAmount)); }
			catch (Exception e) { Logger.LogError($"Harmony patching failed: {e.Message}"); }

			logger.LogInfo("Load Complete");
		}

		private void OnDestroy() {
			harmony.UnpatchSelf();
			instance = null;
		}

		/*
		 * Currently have been getting exceptions when trying to access recipe protoIds that are not in range of the unlockedRecipeSnapShot array
		 * for now just short-circuit that array in case it's out of range
		 */
		[HarmonyPrefix, HarmonyPatch(typeof(RecipeCheck), "AfterTickUnlockedRecipeCheck")]
		public static bool RecipeCheck_AfterTickUnlockedRecipeCheck_Patch(RecipeCheck __instance) {
			HashSet<int> recipeUnlocked = __instance.abnormalityCheck.gameData.history.recipeUnlocked;
			if (recipeUnlocked.Count > __instance.unlockCount) {
				foreach (int num in recipeUnlocked) {
					/*
					 * CODE MODIFICATION
					 * 
					 * added "num >= __instance.unlockedRecipeSnapShot.Length"
					 */
					if (num < __instance.unlockedRecipeSnapShot.Length && __instance.unlockedRecipeSnapShot[num] == null) {
						RecipeProto recipeProto = LDB.recipes.Select(num);
						if (recipeProto == null) {
							Debug.LogWarning(string.Format("公式解锁异常, 公式不存在! 公式id:{0}", num));
							__instance.abnormalityCheck.NotifyAbnormalityChecked(2, false);
							break;
						}
						if (recipeProto.preTech != null && !__instance.abnormalityCheck.gameData.history.TechUnlocked(recipeProto.preTech.ID)) {
							Debug.LogWarning(string.Format("公式解锁异常, 前置科技未解锁! 公式id:{0} 前置科技id:{1}", num, recipeProto.preTech.ID));
							__instance.abnormalityCheck.NotifyAbnormalityChecked(2, false);
							break;
						}
						__instance.unlockedRecipeSnapShot[num] = recipeProto;
						__instance.unlockCount++;
					}
				}
			}

			return false;
		}
	}
}
