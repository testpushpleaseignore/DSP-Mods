
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using PowerNetworkManager.UI;

namespace PowerNetworkManager {

	[BepInDependency("me.xiaoye97.plugin.Dyson.LDBTool", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    [BepInProcess("DSPGame.exe")]
    public class PowerNetworkManager : BaseUnityPlugin {
        public const string pluginGuid = "testpostpleaseignore.dsp.powernetworkmanager";
        public const string pluginName = "Power_Network_Manager";
        public const string pluginVersion = "0.0.1";

        public static PowerNetworkManager instance;
        public static PowerData powerData = new PowerData();

        internal static ManualLogSource logger;
        new internal static BepInEx.Configuration.ConfigFile Config;

        static bool ignoreFirstReverseButtonOnPointerEnter = true;
        public static RectTransform launchButton;
        public static Sprite reverseSprite;

        public static PowerWindow powerWindow;

        public void Awake() {
            logger = base.Logger;
            Config = base.Config;

            instance = this;

            powerWindow = new PowerWindow(powerData);

            //Harmony.CreateAndPatchAll(typeof(PowerNetworkManager));
            Harmony.CreateAndPatchAll(typeof(PowerNetworkManager));

            logger.LogInfo("Load Complete");
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

            logger.LogDebug("GameMain_Begin_Prefix start");

            if (GameMain.instance != null) {
                if (GameObject.Find("Game Menu/button-1-bg")) {
                    if (!GameObject.Find("pnm-launch-button")) {
                        RectTransform prefab = GameObject.Find("Game Menu/button-1-bg").GetComponent<RectTransform>();
                        launchButton = GameObject.Instantiate<RectTransform>(prefab);
                        launchButton.gameObject.name = "pnm-launch-button";

                        logger.LogDebug("launchButton name: " + launchButton.gameObject.name);

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

            // Draw a plane like the one re[resending drones in the Mecha Panel...
            for (int x = 0; x < 48; x++) {
                for (int y = 0; y < 48; y++) {
                    if (((x >= 7) && (x <= 39) && (y >= 10) && (y <= 16)) ||  // top
                        ((x == 33) && (y >= 1) && (y <= 25)) ||
                        ((x == 34) && (y >= 2) && (y <= 24)) ||
                        ((x == 35) && (y >= 3) && (y <= 23)) ||
                        ((x == 36) && (y >= 4) && (y <= 22)) ||
                        ((x == 37) && (y >= 5) && (y <= 21)) ||
                        ((x == 38) && (y >= 6) && (y <= 20)) ||
                        ((x == 39) && (y >= 7) && (y <= 19)) ||
                        ((x == 40) && (y >= 8) && (y <= 18)) ||
                        ((x == 41) && (y >= 9) && (y <= 17)) ||
                        ((x == 42) && (y >= 10) && (y <= 16)) ||
                        ((x == 43) && (y >= 11) && (y <= 15)) ||
                        ((x == 44) && (y >= 12) && (y <= 14)) ||
                        ((x == 45) && (y == 13)) ||
                        ((x >= 8) && (x <= 40) && (y >= 31) && (y <= 37)) ||  // bottom
                        ((x == 2) && (y == 34)) ||
                        ((x == 3) && (y >= 33) && (y <= 35)) ||
                        ((x == 4) && (y >= 32) && (y <= 36)) ||
                        ((x == 5) && (y >= 31) && (y <= 37)) ||
                        ((x == 6) && (y >= 30) && (y <= 38)) ||
                        ((x == 7) && (y >= 29) && (y <= 39)) ||
                        ((x == 8) && (y >= 28) && (y <= 40)) ||
                        ((x == 9) && (y >= 27) && (y <= 41)) ||
                        ((x == 10) && (y >= 26) && (y <= 42)) ||
                        ((x == 11) && (y >= 25) && (y <= 43)) ||
                        ((x == 12) && (y >= 24) && (y <= 44)) ||
                        ((x == 13) && (y >= 23) && (y <= 45)) ||
                        ((x == 14) && (y >= 22) && (y <= 46))) {
                        tex.SetPixel(x, y, color);
                    }
                    else {
                        tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
            }

            tex.name = "greyhak-reverse-icon";
            tex.Apply();

            return Sprite.Create(tex, new Rect(0f, 0f, 48f, 48f), new Vector2(0f, 0f), 1000);
        }

        static void ButtonClick() {
            UIPowerNodeWindow powerWindow = UIRoot.instance.uiGame.nodeWindow;
            PowerSystem powerSystem = powerWindow.powerSystem;
            int selectedPowerNodeID = powerWindow.nodeId;
            PowerNodeComponent selectedPowerNode = powerSystem.nodePool[selectedPowerNodeID];

            PowerData.currentPowerNetworkID = selectedPowerNode.networkId;
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
