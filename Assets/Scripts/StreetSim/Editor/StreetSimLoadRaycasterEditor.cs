using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[CustomEditor(typeof(StreetSimRaycaster))]
public class StreetSimRaycasterEditor : Editor
{

    public override void OnInspectorGUI() {
        StreetSimRaycaster controller = (StreetSimRaycaster)target;

        DrawDefaultInspector();

        if (!controller.initialized) return;
        if (!StreetSim.S.initialized) return;
        if (StreetSimLoadSim.LS.participantData.Count == 0) return;
        if (StreetSimLoadSim.LS.currentParticipant == null || StreetSimLoadSim.LS.currentParticipant.Length == 0) return;
        
        EditorGUILayout.LabelField("Gaze Controls", EditorStyles.boldLabel);
        string cubeText = (controller.showCubeGaze) ? "Hide Cube Points" : "Show Cube Points";
        if (GUILayout.Button(cubeText)) {
            controller.ToggleCubeGaze();
        }
        string rectText = (controller.showRectGaze) ? "Hide Rect Points" : "Show Rect Points";
        if (GUILayout.Button(rectText)) {
            controller.ToggleRectGaze();
        }
        string sphereText = (controller.showSphereGaze) ? "Hide Sphere Points" : "Show Sphere Points";
        if (GUILayout.Button(sphereText)) {
            controller.ToggleSphereGaze();
        }
    }

    // Code attributed to: https://forum.unity.com/threads/horizontal-line-in-editor-window.520812/
    public static void DrawUILine(Color color, int thickness = 2, int padding = 10) {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding+thickness));
        r.height = thickness;
        r.y+=padding/2;
        r.x-=2;
        r.width +=6;
        EditorGUI.DrawRect(r, color);
    }

    public static void DrawPadding(int padding = 10) {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding));
        r.height = padding;
        r.x-=2;
        r.width += 6;
        EditorGUI.DrawRect(r,Color.clear);
    }

    // Code attributed to: https://forum.unity.com/threads/changing-the-background-color-for-beginhorizontal.66015/
    private Texture2D MakeTex(int width, int height, Color col) {
        Color[] pix = new Color[width*height];
        for(int i = 0; i < pix.Length; i++) {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
