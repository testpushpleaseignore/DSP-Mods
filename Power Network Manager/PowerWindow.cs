using System;
using UnityEngine;

namespace PowerNetworkManager.UI {
	public class PowerWindow {

        private const string WindowName = "Network Power Info";

        public PowerData powerData;

        public static bool HighlightButton = false;
        //public static bool ShowButton = true;
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

        public PowerWindow(PowerData powerData) {
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
			GUILayout.BeginVertical();
            sv = GUILayout.BeginScrollView(sv, UnityEngine.GUI.skin.box);

			#region summary rows
			GUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
            GUILayout.Label($"<b>Max Power Usage for Network: {PowerData.convertPowerToString(PowerData.maxNetworkPowerUsage)}</b>", UITheme.TextAlignStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
            GUILayout.Label($"<b>Max Power Usage for Network (sans transport stations): {PowerData.convertPowerToString(PowerData.maxNetworkPowerUsageSansTransports)}</b>", UITheme.TextAlignStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
            GUILayout.Label($"<b>Consumption Demand: {PowerData.convertPowerToString(PowerData.powerDemand)}</b>", UITheme.TextAlignStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
            GUILayout.Label($"<b>Consumer ratio: {Math.Round(PowerData.consumerRatio,3)};  Generator ratio: {Math.Round(PowerData.generatorRatio, 3)}</b>", UITheme.TextAlignStyle);
            GUILayout.EndHorizontal();
			#endregion

			#region Exchangers
			GUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
            GUILayout.Label($"<b>Exchangers: {PowerData.convertPowerToString(PowerData.exchangerMaxPower)}</b>", UITheme.TextAlignStyle);
            GUILayout.EndHorizontal();

            foreach (int excProtoID in PowerData.curDischExchangersPerType.Keys) {
                GUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
                GUILayout.Label($"<b>    {LDB.items.Select(excProtoID).name} (Discharging) -- max power: {PowerData.convertPowerToString(PowerData.curDischExchangersPerType[excProtoID].maxPower)} ;  cur power: {PowerData.convertPowerToString(PowerData.curDischExchangersPerType[excProtoID].curPower)}</b>", UITheme.TextAlignStyle);
                GUILayout.EndHorizontal();
            }

            foreach (int excProtoID in PowerData.curChargingExchangersPerType.Keys) {
                GUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
                GUILayout.Label($"<b>    {LDB.items.Select(excProtoID).name} (Charging) -- max power: {PowerData.convertPowerToString(PowerData.curChargingExchangersPerType[excProtoID].maxPower)} ;  cur power: {PowerData.convertPowerToString(PowerData.curChargingExchangersPerType[excProtoID].curPower)}</b>", UITheme.TextAlignStyle);
                GUILayout.EndHorizontal();
            }
            #endregion

            #region Generators
            GUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
            GUILayout.Label($"<b>Generation capacity: {PowerData.convertPowerToString(PowerData.generatorOutputCapacity)} ({PowerData.generatorCount})</b>", UITheme.TextAlignStyle);
            GUILayout.EndHorizontal();

            foreach(int genProtoID in PowerData.curGenerationData.Keys) {
                PowerGenData data = PowerData.curGenerationData[genProtoID];

                GUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
                GUILayout.Label($"<b>    {LDB.items.Select(genProtoID).name} --  max power: {PowerData.convertPowerToString(data.maxPower)};  genPower: {PowerData.convertPowerToString(data.genPower)};  used: {PowerData.convertPowerToString(data.curPower)}</b>", UITheme.TextAlignStyle);
                GUILayout.EndHorizontal();
            }
            #endregion

            #region Accumulators
            foreach (int accProtoID in PowerData.curAccPerType.Keys) {
                PowerAccData data = PowerData.curAccPerType[accProtoID];
                string status = data.curPower < 0 ? "Discharging" : data.curPower > 0 ? "Charging" : "Full";
                long maxPower = data.curPower < 0 ? data.maxDiscPower : data.curPower > 0 ? data.maxChgPower : data.maxDiscPower;

                GUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
                GUILayout.Label($"<b>{LDB.items.Select(accProtoID).name} ({status}) --  Max {status} Power: {PowerData.convertPowerToString(maxPower)};  Current Power: {PowerData.convertPowerToString(Math.Abs(data.curPower))}</b>", UITheme.TextAlignStyle);
                GUILayout.EndHorizontal();
            }
            #endregion

            #region Consumers
            GUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
            GUILayout.Label($"<b>Consumers</b>", UITheme.TextAlignStyle);
            GUILayout.EndHorizontal();

            foreach (int consProtoID in PowerData.curConsPerType.Keys) {
                PowerConsData data = PowerData.curConsPerType[consProtoID];

                GUILayout.BeginHorizontal(UnityEngine.GUI.skin.box);
                GUILayout.Label($"<b>    {LDB.items.Select(consProtoID).name} --  max power: {PowerData.convertPowerToString(data.maxPower)};  current power: {PowerData.convertPowerToString(data.currPower)};  minimum (idle) power: {PowerData.convertPowerToString(data.idlePower)}</b>", UITheme.TextAlignStyle);
                GUILayout.EndHorizontal();
            }
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
