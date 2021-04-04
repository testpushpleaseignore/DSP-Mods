using System;
using UnityEngine;
using PowerNetworkManager.Data;

namespace PowerNetworkManager.UI {
	public class PowerWindow {

        private const string WindowName = "Network Power Info";

        public PowerDataCalc powerData;

        public static bool HighlightButton = false;
        public static bool Show = false;
        private static Rect winRect = new Rect(0, 0, 1015, 650);

        public static int UILayoutHeight { get; set; } = 1080;

        public static int ScaledScreenWidth { get; set; } = 1920;
        public static int ScaledScreenHeight { get; set; } = 1080;
        public static float ScaleRatio { get; set; } = 1.0f;
        const float FixedSizeAdjustOriginal = 0.9f;
        public static float FixedSizeAdjust { get; set; } = FixedSizeAdjustOriginal;

        private static Vector2 sv;


        private static bool isInit = false;

        public const int valueBoxWidth = 300;

        public PowerWindow(PowerDataCalc powerData) {
            this.powerData = powerData;
        }

        public void OnGUI() {
            var uiGame = BGMController.instance.uiGame;
            var shouldShowByGameState = DSPGame.GameDesc != null && uiGame != null && uiGame.gameData != null && uiGame.guideComplete && DSPGame.IsMenuDemo == false && DSPGame.Game.running
                && (UIGame.viewMode == EViewMode.Normal || UIGame.viewMode == EViewMode.Sail)
                && !(uiGame.techTree.active || uiGame.dysonmap.active || uiGame.starmap.active || uiGame.escMenu.active || uiGame.hideAllUI0 || uiGame.hideAllUI1)
                && uiGame.gameMenu.active;

            if (!shouldShowByGameState) {
                return;
            }

            if (!isInit && GameMain.isRunning) {
                UITheme.Init();
                //InitSources();
                isInit = true;
            }

            AutoResize(DSPGame.globalOption.uiLayoutHeight, applyCustomScale: false);

            /*
            if (ShowButton && shouldShowByGameState) {
                DrawMenuButton();
            }
            */

            AutoResize(UILayoutHeight);

            if (Show) {
                winRect = GUILayout.Window(6549813, winRect, WindowFunc, WindowName);
                EatInputInRect(winRect);
            }
        }

        public static void AutoResize(int designScreenHeight, bool applyCustomScale = true) {
            if (applyCustomScale) {
                designScreenHeight = (int)Math.Round((float)designScreenHeight / FixedSizeAdjust);
            }

            ScaledScreenHeight = designScreenHeight;
            ScaleRatio = (float)Screen.height / designScreenHeight;

            // Vector2 resizeRatio = new Vector2((float)Screen.width / screenWidth, (float)Screen.height / screenHeight);
            ScaledScreenWidth = (int)Math.Round(Screen.width / ScaleRatio);
            UnityEngine.GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(ScaleRatio, ScaleRatio, 1.0f));
        }

        public void WindowFunc(int id) {
			#region upper-right buttons
			GUILayout.BeginArea(new Rect(winRect.width - 22f, 2f, 20f, 17f));
            if (GUILayout.Button("X")) {
                Show = false;
            }
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(winRect.width - 45f, 2f, 20f, 17f));
            if (GUILayout.Button("+")) {
                FixedSizeAdjust = Mathf.Min(FixedSizeAdjustOriginal + 0.8f, FixedSizeAdjust + 0.1f);
            }
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(winRect.width - 64f, 2f, 20f, 17f));
            if (GUILayout.Button("1")) {
                FixedSizeAdjust = FixedSizeAdjustOriginal;
            }
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(winRect.width - 83f, 2f, 20f, 17f));
            if (GUILayout.Button("-")) {
                FixedSizeAdjust = Mathf.Max(FixedSizeAdjustOriginal - 0.5f, FixedSizeAdjust - 0.1f);
            }
            GUILayout.EndArea();
            #endregion

            #region main window area
			GUILayout.BeginVertical(UITheme.layoutBackgroundColor);
            sv = GUILayout.BeginScrollView(sv, UnityEngine.GUI.skin.box);

			#region summary rows
			GUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
            GUILayout.Label($"<b>Max Power Usage for Network: {PowerDataCalc.maxNetworkPowerUsageString}</b>", UITheme.TextAlignStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
            GUILayout.Label($"<b>Max Power Usage for Network (sans transport stations): {PowerDataCalc.maxNetworkPowerUsageSansTransportsString}</b>", UITheme.TextAlignStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
            GUILayout.Label($"<b>Consumption Demand: {PowerDataCalc.powerDemandString}</b>", UITheme.TextAlignStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
            GUILayout.Label($"<b>Consumer ratio: {Math.Round(PowerDataCalc.consumerRatio,3)};  Generator ratio: {Math.Round(PowerDataCalc.generatorRatio, 3)}</b>", UITheme.TextAlignStyle);
            GUILayout.EndHorizontal();
            #endregion

            GUILayout.Space(20);

			#region Exchangers
			GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
            GUILayout.Label($"<b>Exchangers</b>", UITheme.TextAlignStyle);

            GUILayout.BeginVertical(UnityEngine.GUI.skin.box);

            foreach (int excProtoID in PowerDataCalc.curDischExchangersPerType.Keys) {
                PowerExcData data = PowerDataCalc.curDischExchangersPerType[excProtoID];

                GUILayout.Label($"{LDB.items.Select(excProtoID).name} (Discharging)");

                GUILayout.BeginHorizontal();
                GUILayout.Box($"Max Power: {data.maxPowerString}", GUILayout.Width(valueBoxWidth));
                GUILayout.Box($"Current Power: {data.curPowerString}", GUILayout.Width(valueBoxWidth));
                GUILayout.EndHorizontal();
            }

            foreach (int excProtoID in PowerDataCalc.curChargingExchangersPerType.Keys) {
                PowerExcData data = PowerDataCalc.curDischExchangersPerType[excProtoID];

                GUILayout.Label($"{LDB.items.Select(excProtoID).name} (Charging)");

                GUILayout.BeginHorizontal();
                GUILayout.Box($"Max Power: {data.maxPowerString}", GUILayout.Width(valueBoxWidth));
                GUILayout.Box($"Current Power: {data.curPowerString}", GUILayout.Width(valueBoxWidth));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            GUILayout.EndVertical();
            #endregion

            GUILayout.Space(20);

            #region Generators
            GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
            GUILayout.Label($"<b>Generators</b>", UITheme.TextAlignStyle);

            foreach(int genProtoID in PowerDataCalc.curGenerationData.Keys) {
                PowerGenData data = PowerDataCalc.curGenerationData[genProtoID];

                GUILayout.BeginVertical(UnityEngine.GUI.skin.box);

                GUILayout.Label($"{LDB.items.Select(genProtoID).name}");

                GUILayout.BeginHorizontal();

                GUILayout.Box($"Max Power: {data.maxPowerString}", GUILayout.Width(valueBoxWidth));
                GUILayout.Box($"Current Power: {data.genPowerString}", GUILayout.Width(valueBoxWidth));
                GUILayout.Box($"Power Being Used: {data.curPowerString}", GUILayout.Width(valueBoxWidth));

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
            #endregion

            GUILayout.Space(20);

            #region Accumulators
            GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
            GUILayout.Label($"<b>Accumulators</b>", UITheme.TextAlignStyle);

            GUILayout.BeginVertical(UnityEngine.GUI.skin.box);

            foreach (int accProtoID in PowerDataCalc.curAccPerType.Keys) {
                PowerAccData data = PowerDataCalc.curAccPerType[accProtoID];
                string status = data.curPower < 0 ? "Discharging" : data.curPower > 0 ? "Charging" : PowerDataCalc.currentAccumulatedEnergy == 0 ? "Empty" : "Full";
                string maxPower = data.curPower < 0 ? data.maxDiscPowerString : data.curPower > 0 ? data.maxChgPowerString : data.maxDiscPowerString;

                GUILayout.Label($"{LDB.items.Select(accProtoID).name} ({status})");

                GUILayout.BeginHorizontal();
                GUILayout.Box($"Max Power: {maxPower}", GUILayout.Width(valueBoxWidth));
                GUILayout.Box($"Current Power: {Math.Abs(data.curPower)}", GUILayout.Width(valueBoxWidth));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            GUILayout.EndVertical();
            #endregion

            GUILayout.Space(20);

            #region Consumers
            GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
            GUILayout.Label($"<b>Consumers</b>", UITheme.TextAlignStyle);

            

            foreach (int consProtoID in PowerDataCalc.curConsPerType.Keys) {
                PowerConsData data = PowerDataCalc.curConsPerType[consProtoID];

                GUILayout.BeginVertical(UnityEngine.GUI.skin.box);

                GUILayout.Label($"{LDB.items.Select(consProtoID).name}");

                GUILayout.BeginHorizontal();
                GUILayout.Box($"Max Power: {data.maxPowerString}", GUILayout.Width(valueBoxWidth));
                GUILayout.Box($"Current Power: {data.currPowerString}", GUILayout.Width(valueBoxWidth));
                GUILayout.Box($"Minimum (Idle) Power: {data.idlePowerString}", GUILayout.Width(valueBoxWidth));
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            

            GUILayout.EndVertical();
            #endregion

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
			#endregion

			UnityEngine.GUI.DragWindow();

            // Always close window on Escape for now
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape) {
                Show = false;
            }
        }

        public static void EatInputInRect(Rect eatRect) {
            var scaledEatRect = new Rect(UnityEngine.GUI.matrix.lossyScale.x * eatRect.x, UnityEngine.GUI.matrix.lossyScale.y * eatRect.y,
                UnityEngine.GUI.matrix.lossyScale.x * eatRect.width, UnityEngine.GUI.matrix.lossyScale.y * eatRect.height);
            if (scaledEatRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y))) {
                // Ideally I want to only block mouse events from going through.
                bool isMouseInput = Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.mouseScrollDelta.y != 0;

                if (!isMouseInput) {
                    // UnityEngine.Debug.Log("Canceling capture due to input not being mouse");
                    return;
                }
                else {
                    Input.ResetInputAxes();
                }

            }
        }
    }
}
