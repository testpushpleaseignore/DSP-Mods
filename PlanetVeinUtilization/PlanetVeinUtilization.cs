using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace PlanetVeinUtilization {
	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	public class PlanetVeinUtilization : BaseUnityPlugin {
		public const string pluginGuid = "testpostpleaseignore.dsp.planet_vein_utilization";
		public const string pluginName = "Planet Vein Utilization";
		public const string pluginVersion = "1.0.4";

		public class VeinTypeInfo {
			public EVeinType type;
			public string origLabel;
			public int numVeinGroups;
			public int numVeinGroupsWithCollector;

			public void Reset() {
				numVeinGroups = 0;
				numVeinGroupsWithCollector = 0;
			}
		}

		private static PlanetData currentPlanet;
		private static Dictionary<EVeinType, VeinTypeInfo> veinCount = new Dictionary<EVeinType, VeinTypeInfo>();

		Harmony harmony;
		internal static ManualLogSource logger;

		void Awake() {
			harmony = new Harmony(pluginGuid);
			harmony.PatchAll(typeof(PlanetVeinUtilization));

			logger = base.Logger;
		}

		[HarmonyPostfix, HarmonyPatch(typeof(UIPlanetDetail), "RefreshDynamicProperties")]
		public static void UIPlanetDetail_RefreshDynamicProperties_Postfix(UIPlanetDetail __instance) {


			if (__instance.planet != null && __instance.planet.factory != null && __instance.planet.runtimeVeinGroups != null) {
				if (currentPlanet != __instance.planet) {
					currentPlanet = __instance.planet;
					veinCount.Clear();
				}

				//find out which vein groups have miners attached to them
				bool[] veinGroupContainsMiner = new bool[__instance.planet.runtimeVeinGroups.Length];
				foreach (VeinData veinData in __instance.planet.factory.veinPool) {
					if (veinData.amount > 0 && veinData.minerCount > 0) {
						veinGroupContainsMiner[veinData.groupIndex] = true;
					}
				}

				foreach (KeyValuePair<EVeinType, VeinTypeInfo> vti in veinCount) {
					vti.Value.Reset();
				}

				//count up the total number of vein groups per resource type, as well as the total number of groups that have a miner attached
				for (int i = 0; i < __instance.planet.runtimeVeinGroups.Length; i++) {
					VeinGroup veinGroup = __instance.planet.runtimeVeinGroups[i];

					if (veinGroup.amount == 0)
						continue;

					if (!veinCount.ContainsKey(veinGroup.type)) {
						VeinTypeInfo v = new VeinTypeInfo {
							type = veinGroup.type,
							numVeinGroups = 1,
							numVeinGroupsWithCollector = veinGroupContainsMiner[i] ? 1 : 0
						};

						veinCount.Add(veinGroup.type, v);
					} else {
						veinCount[veinGroup.type].numVeinGroups++;
						veinCount[veinGroup.type].numVeinGroupsWithCollector += veinGroupContainsMiner[i] ? 1 : 0;
					}
				}

				//update each resource to show the following vein group info:
				//     Iron:  <number of vein groups with miners> / <total number of vein groups>
				int num = (__instance.planet == GameMain.localPlanet) ? 1 : 2;
				TextGenerator textGen = new TextGenerator();
				float maxResTextboxWidth = 0.0f;
				foreach (UIResAmountEntry uiresAmountEntry in __instance.entries) {
					string labelString;

					if (uiresAmountEntry.refId > 0 && GameMain.history.universeObserveLevel >= num && veinCount.ContainsKey((EVeinType) uiresAmountEntry.refId)) {
						VeinTypeInfo vt = veinCount[(EVeinType) uiresAmountEntry.refId];

						if (vt.origLabel == null || vt.origLabel.Equals("")) {
							vt.origLabel = uiresAmountEntry.labelText.text;
						}

						labelString = vt.origLabel + ":  " + vt.numVeinGroupsWithCollector + "/" + vt.numVeinGroups;
						uiresAmountEntry.overrideLabel = labelString;
					} else
						labelString = uiresAmountEntry.labelText.text;

					TextGenerationSettings generationSettings = uiresAmountEntry.labelText.GetGenerationSettings(uiresAmountEntry.labelText.rectTransform.rect.size);
					float labelWidth = textGen.GetPreferredWidth(labelString, generationSettings);



					if (labelWidth > maxResTextboxWidth)
						maxResTextboxWidth = labelWidth;
				}

				//resize window to fit all of the new text we've added
				//
				//hardcoding a value of 150 for now, since I'm too lazy right now to figure out
				//how much space the icon and the text of the right of the icon is currently taking up
				__instance.rectTrans.sizeDelta = new Vector2(maxResTextboxWidth + 150, __instance.rectTrans.sizeDelta.y);
			}
		}

		void OnDestroy() {
			harmony.UnpatchSelf();
		}
	}
}
