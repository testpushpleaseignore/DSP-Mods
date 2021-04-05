using UnityEngine;

namespace PowerNetworkManager.UI {
	public class UITheme {
		public static GUIStyle TextAlignStyle;
		public static GUIStyle layoutBackgroundColor;
		public static GUILayoutOption[] VeinIconLayoutSmallOptions;

		public static void Init() {
			TextAlignStyle = new GUIStyle(UnityEngine.GUI.skin.label);
			TextAlignStyle.alignment = TextAnchor.MiddleLeft;

			layoutBackgroundColor = new GUIStyle(UnityEngine.GUI.skin.box);
			layoutBackgroundColor.normal.background = Texture2D.whiteTexture;

			VeinIconLayoutSmallOptions = new GUILayoutOption[] { GUILayout.Height(30), GUILayout.MaxWidth(30) };
		}
		
	}
}
