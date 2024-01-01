using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetVeinUtilization {
	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	public class PlanetVeinUtilization : BaseUnityPlugin {
		public const string pluginGuid = "testpostpleaseignore.dsp.planet_vein_utilization";
		public const string pluginName = "Planet Vein Utilization";
		public const string pluginVersion = "1.0.6";

		private static bool planetPanelInitialized = false;
		private static bool starPanelInitialized = false;
		private static VeinTypeInfo[] planetVeinCount = new VeinTypeInfo[(int) EVeinType.Max];
		private static VeinTypeInfo[] starVeinCount = new VeinTypeInfo[(int) EVeinType.Max];
		private static Dictionary<int, bool> tmpGroups = new Dictionary<int, bool>();

		Harmony harmony;
		internal static ManualLogSource logger;

		void Awake()
		{
			harmony = new Harmony(pluginGuid);
			harmony.PatchAll(typeof(PlanetVeinUtilization));

			InitializeVeinCountArray(planetVeinCount);
			InitializeVeinCountArray(starVeinCount);
		}

		void OnDestroy()
		{
			harmony.UnpatchSelf();
		}

		#region Helper functions
		private static void ProcessVeinData(VeinTypeInfo[] veinCount, VeinData[] veinPool)
		{
			lock (veinPool)
			{
				foreach (VeinData veinData in veinPool)
				{
					if (veinData.groupIndex == 0 || veinData.amount == 0) continue;
					if (tmpGroups.TryGetValue(veinData.groupIndex, out bool hasMiner))
					{
						if (hasMiner) continue;
						hasMiner = veinData.minerCount > 0;
						if (!hasMiner) continue;
						tmpGroups[veinData.groupIndex] = true;
						VeinTypeInfo vti = veinCount[(int) veinData.type];
						vti.numVeinGroupsWithCollector++;
					} else
					{
						hasMiner = veinData.minerCount > 0;
						tmpGroups.Add(veinData.groupIndex, hasMiner);
						VeinTypeInfo vti = veinCount[(int) veinData.type];
						vti.numVeinGroups++;
						if (hasMiner)
						{
							vti.numVeinGroupsWithCollector++;
						}
					}
				}
			}
			tmpGroups.Clear();
		}

		private static void FormatResource(int refId, UIResAmountEntry uiresAmountEntry, VeinTypeInfo vt)
		{
			if (vt.textCtrl == null)
			{
				vt.textCtrl = Object.Instantiate(uiresAmountEntry.valueText, uiresAmountEntry.labelText.transform.parent);
				vt.textCtrl.font = uiresAmountEntry.labelText.font;
				RectTransform trans = vt.textCtrl.rectTransform;
				Vector3 pos = uiresAmountEntry.iconImage.rectTransform.position;
				pos.x -= 0.22f;
				trans.position = pos;
				Vector2 size = trans.sizeDelta;
				size.x = 40f;
				trans.sizeDelta = size;
			} else if (refId >= (int) EVeinType.Oil)
			{
				RectTransform trans = vt.textCtrl.rectTransform;
				Vector3 pos = trans.position;
				pos.y = uiresAmountEntry.iconImage.rectTransform.position.y;
				trans.position = pos;
			}
			vt.textCtrl.text = vt.numVeinGroupsWithCollector + "/" + vt.numVeinGroups;
		}

		private static void InitializeVeinCountArray(VeinTypeInfo[] veinCountArray)
		{
			for (int i = 0; i < veinCountArray.Length; i++)
			{
				veinCountArray[i] = new VeinTypeInfo();
			}
		}

		private static Vector2 GetAdjustedSizeDelta(Vector2 origSizeDelta)
		{
			return new Vector2(origSizeDelta.x + 45f, origSizeDelta.y);
		}
		#endregion

		#region UIPlanetDetail patches
		[HarmonyPrefix, HarmonyPatch(typeof(UIPlanetDetail), "OnPlanetDataSet")]
		public static void UIPlanetDetail_OnPlanetDataSet_Prefix(UIPlanetDetail __instance) {
			if (!planetPanelInitialized) {
				planetPanelInitialized = true;
				__instance.rectTrans.sizeDelta = GetAdjustedSizeDelta(__instance.rectTrans.sizeDelta);
			}
			foreach (VeinTypeInfo vti in planetVeinCount) {
				vti.Reset();
			}
		}

		[HarmonyPostfix, HarmonyPatch(typeof(UIPlanetDetail), "RefreshDynamicProperties")]
		public static void UIPlanetDetail_RefreshDynamicProperties_Postfix(UIPlanetDetail __instance) {
			PlanetData planet = __instance.planet;
			if (planet == null || planet.runtimeVeinGroups == null) { return; }

			int observeLevelCheck = __instance.planet == GameMain.localPlanet ? 1 : 2;
			if (GameMain.history.universeObserveLevel < observeLevelCheck) { return; }

			foreach (VeinTypeInfo vti in planetVeinCount) {
				vti.numVeinGroups = 0;
				vti.numVeinGroupsWithCollector = 0;
			}
			//count up the total number of vein groups per resource type, as well as the total number of groups that have a miner attached
			PlanetFactory factory = planet.factory;
			if (factory != null) {
				ProcessVeinData(planetVeinCount, factory.veinPool);
			} else {
				VeinGroup[] veinGroups = planet.runtimeVeinGroups;
				lock (planet.veinGroupsLock) {
					for (int i = 1; i < veinGroups.Length; i++) {
						planetVeinCount[(int) veinGroups[i].type].numVeinGroups++;
					}
				}
			}

			//update each resource to show the following vein group info:
			//     Iron:  <number of vein groups with miners> / <total number of vein groups>
			foreach (UIResAmountEntry uiresAmountEntry in __instance.entries) {
				int refId = uiresAmountEntry.refId;
				VeinTypeInfo vt;
				if (refId > 0 && refId < (int) EVeinType.Max && (vt = planetVeinCount[refId]).numVeinGroups > 0) {
					FormatResource(refId, uiresAmountEntry, vt);
				}
			}
		}
		#endregion

		#region UIStarDetail patches
		[HarmonyPrefix, HarmonyPatch(typeof(UIStarDetail), "OnStarDataSet")]
		public static void UIStaretail_OnStarDataSet_Prefix(UIStarDetail __instance) {
			if (!starPanelInitialized) {
				starPanelInitialized = true;
				__instance.rectTrans.sizeDelta = GetAdjustedSizeDelta(__instance.rectTrans.sizeDelta);
			}
			foreach (VeinTypeInfo vti in starVeinCount) {
				vti.Reset();
			}
		}

		[HarmonyPostfix, HarmonyPatch(typeof(UIStarDetail), "RefreshDynamicProperties")]
		public static void UIStarDetail_RefreshDynamicProperties_Postfix(UIStarDetail __instance) {
			if (__instance.star == null) { return; }
			if (GameMain.history.universeObserveLevel < 2) { return; }

			foreach (VeinTypeInfo vti in starVeinCount) {
				vti.numVeinGroups = 0;
				vti.numVeinGroupsWithCollector = 0;
			}
			foreach (PlanetData planet in __instance.star.planets) {
				if (planet.runtimeVeinGroups == null) { continue; }
				PlanetFactory factory = planet.factory;
				if (factory != null) {
					ProcessVeinData(starVeinCount, factory.veinPool);
				} else {
					VeinGroup[] veinGroups = planet.runtimeVeinGroups;
					lock (planet.veinGroupsLock) {
						for (int i = 1; i < veinGroups.Length; i++) {
							starVeinCount[(int) veinGroups[i].type].numVeinGroups++;
						}
					}
				}

				//update each resource to show the following vein group info:
				//     Iron:  <number of vein groups with miners> / <total number of vein groups>
				foreach (UIResAmountEntry uiresAmountEntry in __instance.entries) {
					int refId = uiresAmountEntry.refId;
					VeinTypeInfo vt;
					if (refId > 0 && refId < (int) EVeinType.Max && (vt = starVeinCount[refId]).numVeinGroups > 0) {
						FormatResource(refId, uiresAmountEntry, vt);
					}
				}
			}
		}
		#endregion
	}

	public class VeinTypeInfo
	{
		public int numVeinGroups;
		public int numVeinGroupsWithCollector;
		public Text textCtrl;

		public void Reset()
		{
			numVeinGroups = 0;
			numVeinGroupsWithCollector = 0;
			if (textCtrl != null) textCtrl.text = "";
		}
	}
}
