using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

[System.Serializable]
public class UserFolder {
    public string folderName;
    public bool isActive;
    public string folderPath;
    public List<UserTrial> folderTrials;
    public UserFolder(string folderName, string folderPath) {
        this.folderName = folderName;
        this.folderPath = folderPath;
        this.isActive = true;
        this.folderTrials = new List<UserTrial>();
    }
    public UserFolder(string folderName, string folderPath, string[] trials) {
        this.folderName = folderName;
        this.folderPath = folderPath;
        this.isActive = true;

        this.folderTrials = new List<UserTrial>();
        List<string> pathsToTrials = new List<string>(trials).Where(t=>!Path.GetFileName(t).StartsWith("Initial")).ToList();
        foreach(string trialPath in pathsToTrials) {
            this.folderTrials.Add(new UserTrial(trialPath));
        }
    }
}

[System.Serializable]
public class UserTrial {
    public string trialName;
    public string pathToTrial;
    public Dictionary<string,bool> fileIntegrityDict;
    public List<string> foundFiles;

    public UserTrial(string pathToTrial) {
        // Save a reference to this file
        this.trialName = Path.GetFileName(pathToTrial);
        this.pathToTrial = pathToTrial;

        // Check files
        CheckFileIntegrity();
    }

    public void CheckFileIntegrity() {
        // Load ALL files (excluding META files, since we don't care about those)
        this.foundFiles = new List<string>(Directory.GetFiles(this.pathToTrial)).Select(f=>Path.GetFileName(f)).Where(f=>f.Substring(f.LastIndexOf('.')+1)!="meta").ToList();
        // Reset fileIntegrityDict
        this.fileIntegrityDict = new Dictionary<string,bool>();

        // For now, know that a properly-loaded and processed user trial SHOULD have the following new files:
        // - "assumedAttempts.csv" - Offline
        this.fileIntegrityDict.Add("assumedAttempts.csv", this.foundFiles.IndexOf("assumedAttempts.csv") > -1);
        // - "averageFixations.csv" - ONLINE
        this.fileIntegrityDict.Add("averageFixations.csv", this.foundFiles.IndexOf("averageFixations.csv") > -1);
        // - "discretizedFixations.csv" - ONLINE
        this.fileIntegrityDict.Add("discretizedFixations.csv", this.foundFiles.IndexOf("discretizedFixations.csv") > -1);
        // - "fixationsByDirection.csv" - offline (but needs 'averageFixations.csv')
        this.fileIntegrityDict.Add("fixationsByDirection.csv", this.foundFiles.IndexOf("fixationsByDirection.csv") > -1);
        // - "gazeDurationsByAgent.csv" - offline
        this.fileIntegrityDict.Add("gazeDurationsByAgent.csv", this.foundFiles.IndexOf("gazeDurationsByAgent.csv") > -1);
        // - "gazeDurationsByHit.csv" - offline
        this.fileIntegrityDict.Add("gazeDurationsByHit.csv", this.foundFiles.IndexOf("gazeDurationsByHit.csv") > -1);
        // - "gazeDurationsByIndex.csv" - offline
        this.fileIntegrityDict.Add("gazeDurationsByIndex.csv", this.foundFiles.IndexOf("gazeDurationsByIndex.csv") > -1);

        // If any of these files are missing, they must be processed
    }

}

public class TempEditorWindow : EditorWindow
{
    string userDataDirectory = "UserData_ignore";
    string readFromDirectory;
    List<UserFolder> userFolders = new List<UserFolder>();

    bool groupEnabled;
    bool myBool = true;
    float myFloat = 1.23f;

    Vector2 scrollPos;

    // Add menu item named "My Window" into the `Window` menu in the inspector
    [MenuItem ("Window/My Window")]

    public static void ShowWindow() {
        EditorWindow.GetWindow(typeof(TempEditorWindow));
    }
    void OnGUI() {
        GUIStyle gs = new GUIStyle();
        gs.normal.background = MakeTex(600, 1, new Color(1.0f, 1.0f, 1.0f, 0.1f));

        GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        userDataDirectory = EditorGUILayout.TextField("Load From:", userDataDirectory);

        readFromDirectory = Path.Combine("Assets",userDataDirectory);
        if (GUILayout.Button("Scan for Folders")) {
            userFolders = new List<UserFolder>();
            foreach(string folder in Directory.GetDirectories(readFromDirectory)) {
                UserFolder tempUserFolder = new UserFolder(Path.GetFileName(folder), folder, Directory.GetDirectories(folder));
                userFolders.Add(tempUserFolder);
            }
        }

        if (userFolders.Count > 0) {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("# User Folders Found:");
            EditorGUILayout.LabelField(userFolders.Count.ToString());
            GUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width), GUILayout.Height(300));
            for(int i = 0; i < userFolders.Count; i++) {
                if (i % 2 == 0) GUILayout.BeginVertical(gs);
                else GUILayout.BeginVertical();

                userFolders[i].isActive = EditorGUILayout.BeginToggleGroup(userFolders[i].folderName, userFolders[i].isActive);
                    if (userFolders[i].isActive) {
                        foreach(UserTrial userTrial in userFolders[i].folderTrials) {
                            EditorGUILayout.LabelField(userTrial.trialName);
                            foreach(KeyValuePair<string,bool> kvp in userTrial.fileIntegrityDict) {
                                if (!kvp.Value) {
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField(kvp.Key);
                                    EditorGUILayout.LabelField(kvp.Value.ToString());
                                    GUILayout.EndHorizontal();
                                }
                            }
                        }
                    }
                    /*
                    ExperimentID curID = controller.gazeObjectTracking[i];
                    EditorGUILayout.LabelField("\""+curID.id+"\":");
                    if(GUILayout.Button("Gaze Heat Map")) {
                        // controller.TrackGazeOnObject(curID);
                        controller.TrackGazeOnObjectFromFile(curID);
                    }
                    if(GUILayout.Button("Track Gaze Groups")) {
                        controller.TrackGazeGroupsOnObject(curID);
                    }
                    */
                EditorGUILayout.EndToggleGroup();
                
                GUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }


        groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
            myBool = EditorGUILayout.Toggle("Toggle", myBool);
            myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
        EditorGUILayout.EndToggleGroup();
    }

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
