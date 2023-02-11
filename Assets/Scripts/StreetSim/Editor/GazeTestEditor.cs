using System.Collections.Generic;
using UnityEngine;
using Helpers;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[CustomEditor(typeof(GazeTest))]
public class GazeTestEditor : Editor
{

    public override void OnInspectorGUI() {
        GazeTest controller = (GazeTest)target;

        DrawDefaultInspector();

        DrawUILine(Color.grey, 2, 10);    

        if(GUILayout.Button("Load Data")) {
            LoadData(controller);
        }
    }

    private void LoadData(GazeTest controller) {
        string[] pr = SaveSystemMethods.ReadCSV(controller.loadedFile);
        List<GazeDataStatistics> dataFormatted = new List<GazeDataStatistics>();
        int numHeaders = GazeDataStatistics.Headers.Count;
        int tableSize = pr.Length/numHeaders - 1;
      
        for(int i = 0; i < tableSize; i++) {
            int rowKey = numHeaders*(i+1);
            string[] row = pr.RangeSubset(rowKey,numHeaders);
            dataFormatted.Add(new GazeDataStatistics(row));
        } 

        controller.SetAggregatedData(dataFormatted);
        return;
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
