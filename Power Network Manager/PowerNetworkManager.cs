
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using PowerNetworkManager.UI;
using PowerNetworkManager.Data;
using System;

namespace PowerNetworkManager {

	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	[BepInProcess("DSPGame.exe")]
	public class PowerNetworkManager : BaseUnityPlugin {
		public const string pluginGuid = "testpostpleaseignore.dsp.powernetworkmanager";
		public const string pluginName = "Power_Network_Manager";
		public const string pluginVersion = "0.0.5";

		private Harmony harmony;

		public static PowerNetworkManager instance;
		public static PowerDataCalc powerData = new PowerDataCalc();

		internal static ManualLogSource logger;
		new internal static BepInEx.Configuration.ConfigFile Config;

		static bool ignoreFirstReverseButtonOnPointerEnter = true;
		public static RectTransform launchButton;
		public static Sprite launchSprite;

		public static PowerWindow powerWindow;

		private void Awake() {
			logger = base.Logger;
			Config = base.Config;

			Assert.Null(instance, $"An instance of {nameof(PowerNetworkManager)} has already been created!");
			instance = this;

			powerWindow = new PowerWindow(powerData);

			harmony = new Harmony(pluginGuid);

			try { harmony.PatchAll(typeof(PowerNetworkManager)); }
			catch (Exception e) { Logger.LogError($"Harmony patching failed: {e.Message}"); }

			logger.LogInfo("Load Complete");
		}

		private void OnDestroy() {
			if (launchButton != null) {
				Destroy(launchButton.gameObject);
				Destroy(launchSprite);
			}

			harmony.UnpatchSelf();
			instance = null;
		}

		// The first call happens for some reason after every game load.
		// This causes the tip to be displayed without a mouse-over when the BeltWindow opens for the first time.
		[HarmonyPrefix, HarmonyPatch(typeof(UIButton), "OnPointerEnter")]
		public static bool UIButton_OnPointerEnter_Prefix(UIButton __instance) {

			if (launchButton != null && __instance == launchButton.GetComponent<UIButton>() && ignoreFirstReverseButtonOnPointerEnter) {
				ignoreFirstReverseButtonOnPointerEnter = false;
				return false;
			}

			return true;
		}

		[HarmonyPrefix, HarmonyPatch(typeof(GameMain), "Begin")]
		public static void GameMain_Begin_Prefix() {
			ignoreFirstReverseButtonOnPointerEnter = true;

			if (GameMain.instance != null) {
				if (GameObject.Find("Game Menu/button-1-bg")) {
					if (!GameObject.Find("pnm-launch-button")) {
						RectTransform prefab = GameObject.Find("Game Menu/button-1-bg").GetComponent<RectTransform>();
						launchButton = GameObject.Instantiate<RectTransform>(prefab);
						launchButton.gameObject.name = "pnm-launch-button";

						UIButton uiButton = launchButton.GetComponent<UIButton>();
						uiButton.tips.tipTitle = "Power Network Manager";
						uiButton.tips.tipText = "Click to launch the Power Network Manager window for this network.";
						uiButton.tips.delay = 0f;

						launchButton.transform.Find("button-1/icon").GetComponent<Image>().sprite = GetSprite();
						launchButton.SetParent(UIRoot.instance.uiGame.nodeWindow.transform);
						launchButton.localScale = new Vector3(0.5f, 0.5f, 0.5f);
						launchButton.localPosition = new Vector3(30, -100, 0);

						uiButton.OnPointerDown(null);
						uiButton.OnPointerEnter(null);
						uiButton.button.onClick.AddListener(() => {
							ButtonClick();
						});
					}
				}
			}
		}

		public static Sprite GetSprite() {
			Texture2D tex = new Texture2D(48, 48, TextureFormat.RGBA32, false);
			Color color = new Color(1, 1, 1, 1);

			int o = 10;

			// Draw a plane like the one re[resending drones in the Mecha Panel...
			for (int x = 0; x < 48; x++) {
				for (int y = 0; y < 48; y++) {
					if (((x >= 7) && (x <= 39) && (y >= o + 10) && (y <= o + 16)) ||
						((x == 33) && (y >= o + 1) && (y <= o + 25)) ||
						((x == 34) && (y >= o + 2) && (y <= o + 24)) ||
						((x == 35) && (y >= o + 3) && (y <= o + 23)) ||
						((x == 36) && (y >= o + 4) && (y <= o + 22)) ||
						((x == 37) && (y >= o + 5) && (y <= o + 21)) ||
						((x == 38) && (y >= o + 6) && (y <= o + 20)) ||
						((x == 39) && (y >= o + 7) && (y <= o + 19)) ||
						((x == 40) && (y >= o + 8) && (y <= o + 18)) ||
						((x == 41) && (y >= o + 9) && (y <= o + 17)) ||
						((x == 42) && (y >= o + 10) && (y <= o + 16)) ||
						((x == 43) && (y >= o + 11) && (y <= o + 15)) ||
						((x == 44) && (y >= o + 12) && (y <= o + 14)) ||
						((x == 45) && (y == o + 13))) {
						tex.SetPixel(x, y, color);
					}
					else {
						tex.SetPixel(x, y, new Color(0, 0, 0, 0));
					}
				}
			}

			tex.name = "power-manager-launch-icon";
			tex.Apply();

			return Sprite.Create(tex, new Rect(0f, 0f, 48f, 48f), new Vector2(0f, 0f), 1000);
		}

		static void ButtonClick() {
			UIPowerNodeWindow powerWindow = UIRoot.instance.uiGame.nodeWindow;
			PowerSystem powerSystem = powerWindow.powerSystem;
			int selectedPowerNodeID = powerWindow.nodeId;
			PowerNodeComponent selectedPowerNode = powerSystem.nodePool[selectedPowerNodeID];

			PowerDataCalc.currentPowerNetworkID = selectedPowerNode.networkId;
			PowerWindow.Show = true;
		}

		void Update() {

		}

		void OnGUI() {
			if (PowerWindow.Show) {
				powerWindow.OnGUI();
			}
		}

		[HarmonyPostfix, HarmonyPatch(typeof(GameData), "GameTick")]
		public static void GameData_GameTick(long time, GameData __instance) {
			if (PowerWindow.Show) {
				powerData.onGameData_GameTick(time, __instance);
			}
		}
	}
}
