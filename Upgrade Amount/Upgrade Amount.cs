
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System;

namespace UpgradeAmount {

	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	[BepInProcess("DSPGame.exe")]
	public class UpgradeAmount : BaseUnityPlugin {
		public const string pluginGuid = "testpostpleaseignore.dsp.upgradeamount";
		public const string pluginName = "Upgrade_Amount";
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

		[HarmonyPostfix, HarmonyPatch(typeof(BuildTool_Upgrade), "DeterminePreviews")]
		public static void BuildTool_Upgrade_DeterminePreview_Patch(BuildTool_Upgrade __instance) {
			/*  cursorType
			 *    0: Single mode (F1 option in game)
			 *    1: Area Select mode (F2 option in game)
			 */
			if (__instance.cursorType == 0) {

				//castObjectId != 0 means that we have an object highlighted
				if (__instance.castObjectId != 0) {
					ItemProto itemProto = __instance.GetItemProto(__instance.castObjectId);

					if (itemProto != null && itemProto.Grade > 0 && itemProto.Upgrades.Length != 0) {
						PrefabDesc prefabDesc = __instance.GetPrefabDesc(__instance.castObjectId);

						if (prefabDesc.isBelt && __instance.filterBelt) {
							BuildPreview buildPreview = __instance.buildPreviews[__instance.buildPreviews.Count - 1];

							if ((buildPreview.lpos - __instance.player.position).sqrMagnitude > __instance.player.mecha.buildArea * __instance.player.mecha.buildArea) {
								//DO NOTHING FOR NOW, PLAYER OUT OF RANGE
							}
							else {
								if (__instance.chainReaction) {
									__instance.actionBuild.model.cursorText = "升级".Translate() + buildPreview.item.name + "\r\n" + $"{__instance.buildPreviews.Count} Selected";
								}
							}
						}
					}
				}
			}
		}

		private void OnDestroy() {
			harmony.UnpatchSelf();
			instance = null;
		}

		void Update() {

		}

		void OnGUI() {
		}
	}
}
