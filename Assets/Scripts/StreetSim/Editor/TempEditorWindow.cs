using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

using Helpers;

[System.Serializable]
public class UserFolder {
    public string folderName;
    public bool isActive;
    public string folderPath;
    public Dictionary<string, List<TrialOmit>> omits;
    public List<UserTrial> folderTrials;
    
    public bool allValid;
    public bool showFolders;

    public UserFolder(string folderName, string folderPath, string[] trials, Dictionary<string, List<TrialOmit>> omits) {
        this.folderName = folderName;
        this.folderPath = folderPath;
        this.omits = omits;
        this.isActive = true;

        this.folderTrials = new List<UserTrial>();
        bool tempAllValid = true;
        List<string> pathsToTrials = new List<string>(trials).Where(t=>!Path.GetFileName(t).StartsWith("Initial")).ToList();
        foreach(string trialPath in pathsToTrials) {
            string trialName = Path.GetFileName(trialPath);
            List<TrialOmit> trialOmits = new List<TrialOmit>();
            if (omits.ContainsKey(trialName)) trialOmits = this.omits[trialName];

            UserTrial newTrial = new UserTrial(trialName, trialPath, trialOmits);
            this.folderTrials.Add(newTrial);
            tempAllValid = tempAllValid && newTrial.allValid;
        }

        this.allValid = tempAllValid;
        this.showFolders = false;
    }

    public void ProcessOffline() {
        foreach(UserTrial trial in folderTrials) {
            trial.ProcessOffline();
        }
    }
}

[System.Serializable]
public class UserTrial {
    public string trialName;
    public string pathToTrial;
    TrialData trialData;
    public Dictionary<string,bool> fileIntegrityDict;
    public List<string> foundFiles;
    public bool allValid;
    public List<TrialOmit> omits;

    public UserTrial(string trialName, string pathToTrial, List<TrialOmit> omits) {
        // Save a reference to this file
        this.trialName = trialName;
        this.pathToTrial = pathToTrial;
        this.omits = omits;
        this.allValid = false;
        if (!GetTrialData(out trialData)) {
            Debug.LogError("COULD NOT LOAD TRIAL \"" + this.trialName + "\"");
            return;
        }

        // Check files
        CheckFileIntegrity();
    }

    public void CheckFileIntegrity() {
        // Load ALL files (excluding META files, since we don't care about those)
        this.foundFiles = new List<string>(Directory.GetFiles(this.pathToTrial)).Select(f=>Path.GetFileName(f)).Where(f=>f.Substring(f.LastIndexOf('.')+1)!="meta").ToList();
        // Reset fileIntegrityDict
        this.fileIntegrityDict = new Dictionary<string,bool>();

        bool tempAllValid = true;

        // For now, know that a properly-loaded and processed user trial SHOULD have the following new files:
        // - "correctedAttempts.csv" - Offline
        tempAllValid = tempAllValid && CheckFile("correctedAttempts.csv");
        // - "averageFixations.csv" - ONLINE
        tempAllValid = tempAllValid && CheckFile("averageFixations.csv");
        // - "discretizedFixations.csv" - ONLINE
        tempAllValid = tempAllValid && CheckFile("discretizedFixations.csv");
        // - "fixationsByDirection.csv" - offline (but needs 'averageFixations.csv')
        tempAllValid = tempAllValid && CheckFile("fixationsByDirection.csv");
        // - "gazeDurationsByAgent.csv" - offline
        tempAllValid = tempAllValid && CheckFile("gazeDurationsByAgent.csv");
        // - "gazeDurationsByHit.csv" - offline
        tempAllValid = tempAllValid && CheckFile("gazeDurationsByHit.csv");
        // - "gazeDurationsByIndex.csv" - offline
        tempAllValid = tempAllValid && CheckFile("gazeDurationsByIndex.csv");

        // If any of these files are missing, they must be processed
        this.allValid = tempAllValid;
    }

    public bool CheckFile(string filename) {
        bool valid = this.foundFiles.IndexOf(filename) > -1;
        if (this.fileIntegrityDict.ContainsKey(filename)) this.fileIntegrityDict[filename] = valid;
        else this.fileIntegrityDict.Add(filename, valid);
        return valid;
    }


    public void ProcessOffline() {
        ProcessAttempts();
    }


    public bool GetTrialData(out TrialData trial) {
        trial = null;
        string trialDataPath = Path.Combine(pathToTrial,"trial.json");
        if (!SaveSystemMethods.CheckFileExists(trialDataPath)) {
            Debug.LogError("Unable to find trial file \""+trialDataPath+"\"");
            return false;
        }
        if (!SaveSystemMethods.LoadJSON<TrialData>(trialDataPath, out trial)) {
            Debug.LogError("Unable to load json file \""+trialDataPath+"\"...");
            return false;
        }
        return true;
    }
    public bool GetPositionsData(out LoadedPositionData positions) {
        
        // Inner Function Declaration
        List<StreetSimTrackable> ParseData(string[] data){
            List<StreetSimTrackable> dataFormatted = new List<StreetSimTrackable>();
            int numHeaders = StreetSimTrackable.Headers.Count;
            int tableSize = data.Length/numHeaders - 1;
        
            for(int i = 0; i < tableSize; i++) {
                int rowKey = numHeaders*(i+1);
                string[] row = data.RangeSubset(rowKey,numHeaders);
                dataFormatted.Add(new StreetSimTrackable(row));
            }
            return dataFormatted;
        }

        // Variable Initialization
        positions = null;
        string positionsPath = Path.Combine(pathToTrial,"positions.csv");

        // Check if `positions.csv` exists
        if (!SaveSystemMethods.CheckFileExists(positionsPath)) {
            Debug.LogError("Cannot load textasset \""+positionsPath+"\"!");
            return false;
        }

        // Load data
        TextAsset ta = (TextAsset)AssetDatabase.LoadAssetAtPath(positionsPath, typeof(TextAsset));
        string[] pr = SaveSystemMethods.ReadCSV(ta);
        List<StreetSimTrackable> p = ParseData(pr);
        Debug.Log("Loaded " + p.Count.ToString() + " raw positions");
        
        // Filter out all StreetSimTrackables in `p` that might exist in trial.trialOmits
        if (omits.Count == 0) {
            Debug.Log("\""+trialName+"\": No omits detected, current positions count to " + p.Count.ToString() + " positions");
            positions = new LoadedPositionData(trialName, ta, p);
        } else {
            List<StreetSimTrackable> p2 = new List<StreetSimTrackable>();
            foreach(StreetSimTrackable sst in p) {
                bool validSST = true;
                foreach(TrialOmit omit in omits) {
                    if (sst.timestamp >= omit.startTimestamp && sst.timestamp < omit.endTimestamp) {
                        validSST = false;
                        break;
                    }
                }
                if (validSST) p2.Add(sst);
            }
            Debug.Log("[\""+trialName+"\": After omits, current positions count to " + p2.Count.ToString() + " positions");
            positions = new LoadedPositionData(trialName, ta, p2);
        }
        return true;
    }

    public bool GetAttemptsData(out List<TrialAttempt> attempts, out bool userFound) {

        // Variable initialization
        attempts = new List<TrialAttempt>();
        userFound = false;
        string originalAttemptsPath = Path.Combine(pathToTrial,"attempts.csv");

        // We can't do anything if we can't that there's a missing attempts.csv in this trial...
        if (!SaveSystemMethods.CheckFileExists(originalAttemptsPath)) {
            Debug.LogError("\"attempts.csv\" file not found for " + trialName);
            return false;
        }

        TextAsset ta = (TextAsset)AssetDatabase.LoadAssetAtPath(originalAttemptsPath, typeof(TextAsset));
        string[] pr = SaveSystemMethods.ReadCSV(ta);
        int numHeaders = TrialAttempt.Headers.Count;
        int tableSize = pr.Length/numHeaders - 1;
        for(int i = 0; i < tableSize; i++) {
            int rowKey = numHeaders*(i+1);
            string[] row = pr.RangeSubset(rowKey, numHeaders);
            TrialAttempt newAttempt = new TrialAttempt(row);
            // Filter based on `omits`            
            if (omits.Count > 0) {
                bool isValid = true;
                foreach(TrialOmit omit in omits) {
                    if (newAttempt.startTime >= omit.startTimestamp && newAttempt.startTime < omit.endTimestamp) {
                        isValid = false;
                        break;
                    }
                }
                if (isValid) {
                    attempts.Add(newAttempt);
                    if (newAttempt.id == "User") userFound = true;
                }
            } else {
                attempts.Add(newAttempt);
                if (newAttempt.id == "User") userFound = true;
            }
            attempts.Add(newAttempt);
            if (newAttempt.id == "User") userFound = true;
        }

        return true;
    }

    public void ProcessAttempts() {

        // Confirm that we have 1) positions.csv data, and 2) attempts.csv data
        LoadedPositionData positions;
        List<TrialAttempt> attempts;
        string newAttemptsPath = Path.Combine(pathToTrial,"correctedAttempts.csv");   
        bool userFound = false;     

        if (!GetPositionsData(out positions)) {
            Debug.LogError("Could not load positions data for \"" + trialName + "\"");
            return;
        }

        if (!GetAttemptsData(out attempts, out userFound)) {
            Debug.LogError("Could not load original attempts for \"" + trialName + "\"");
            return;
        }
        
        // If "User" is among the ids in our attempts list, we just need to port this data into a new "assumedAttempts.csv" file
        // However, if "User" is NOT among the ids in our attempts list, we need to interpret them from existing position data
        if (!userFound) {
            Debug.LogError("Could not find a User inside attempts for " + trialName + ". Deriving from positional data...");
            Dictionary<string, TrialAttempt> currentAttempts = new Dictionary<string, TrialAttempt>();
            StreetSimTrackable trackable, prevTrackable;
            for(int i = 1; i < positions.rawPositionsList.Count; i++) {
                trackable = positions.rawPositionsList[i];
                prevTrackable = positions.rawPositionsList[i-1];
                if (trackable.id != "User") continue;
                if (omits.Count > 0) {
                    bool isValid = true;
                    foreach(TrialOmit omit in omits) {
                        if (prevTrackable.timestamp >= omit.startTimestamp && prevTrackable.timestamp < omit.endTimestamp) {
                            isValid = false;
                            break;
                        }
                    }
                    if (!isValid) continue;
                }
                if (Mathf.Abs(trackable.localPosition_z)<2.75f && !currentAttempts.ContainsKey(trackable.id)) {
                    // This means that the agent has moved onto the crosswalk, yet we don't have an attempt linked to it.
                    currentAttempts.Add(trackable.id, new TrialAttempt(trackable.id, trialData.direction, prevTrackable.timestamp));
                    continue;
                }
                if(trialData.direction == "NorthToSouth") {
                    // Start is when z > 2.25
                    // End is when z < -2.25f
                    if (trackable.localPosition_z >= 2.75f && currentAttempts.ContainsKey(trackable.id)) {
                        // In this case, the person returned to the start sidewalk. This is a failed attempt
                        TrialAttempt cAttempt = currentAttempts[trackable.id];
                        cAttempt.endTime = prevTrackable.timestamp;
                        cAttempt.successful = false;
                        cAttempt.reason = "[ASSUMED] Returned to start sidewalk";
                        attempts.Add(cAttempt);
                        currentAttempts.Remove(trackable.id);
                        continue;
                    }
                    if (trackable.localPosition_z <= -2.75f && currentAttempts.ContainsKey(trackable.id)) {
                        // In this case, the person got to the other end of the sidewalk. This is a successful attempt
                        TrialAttempt cAttempt = currentAttempts[trackable.id];
                        cAttempt.endTime = prevTrackable.timestamp;
                        cAttempt.successful = true;
                        cAttempt.reason = "[ASSUMED] Successfully reached the destination sidewalk";
                        attempts.Add(cAttempt);
                        currentAttempts.Remove(trackable.id);
                        continue;
                    }
                } else {
                    // Start is when z < -2.75
                    // End is when z > 2.75f
                    if (trackable.localPosition_z <= -2.75f && currentAttempts.ContainsKey(trackable.id)) {
                        // In this case, the person returned to the start sidewalk. This is a failed attempt
                        TrialAttempt cAttempt = currentAttempts[trackable.id];
                        cAttempt.endTime = prevTrackable.timestamp;
                        cAttempt.successful = false;
                        cAttempt.reason = "[ASSUMED] Returned to start sidewalk";
                        attempts.Add(cAttempt);
                        currentAttempts.Remove(trackable.id);
                        continue;
                    }
                    if (trackable.localPosition_z >= 2.75f && currentAttempts.ContainsKey(trackable.id)) {
                        // In this case, the person got to the other end of the sidewalk. This is a successful attempt
                        TrialAttempt cAttempt = currentAttempts[trackable.id];
                        cAttempt.endTime = prevTrackable.timestamp;
                        cAttempt.successful = true;
                        cAttempt.reason = "[ASSUMED] Successfully reached the destination sidewalk";
                        attempts.Add(cAttempt);
                        currentAttempts.Remove(trackable.id);
                        continue;
                    }
                }
                return;
            }
            // We need to clean up any attempts remaining. We automatically label them as successful
            prevTrackable = positions.rawPositionsList[positions.rawPositionsList.Count-1];
            foreach(KeyValuePair<string, TrialAttempt> kvp in currentAttempts) {
                TrialAttempt cAttempt = kvp.Value;
                cAttempt.endTime = prevTrackable.timestamp;
                cAttempt.successful = true;
                cAttempt.reason = "[ASSUMED] End of Trial";
                attempts.Add(cAttempt);
            }
        }

        // Now we save the file
        if (SaveSystemMethods.SaveCSV<TrialAttempt>(newAttemptsPath,TrialAttempt.Headers,attempts)) {
            Debug.Log(trialName + ": Saved Assumed Trial Attempts");
        } else {
            Debug.LogError(trialName + ": Could not save Assumed Trial Attempts");
        }
    }
}

public class TempEditorWindow : EditorWindow
{
    string userDataDirectory = "UserData_ignore";
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

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan for Folders")) userFolders = ScanForFolders(userDataDirectory);
        if (GUILayout.Button("Empty Cache")) userFolders = new List<UserFolder>();
        GUILayout.EndHorizontal();

        DrawPadding(10);

        if (userFolders.Count > 0) {
            GUILayout.Label("# User Folders Found: " + userFolders.Count.ToString(), EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width), GUILayout.Height(300));
            for(int i = 0; i < userFolders.Count; i++) {
                if (i % 2 == 0) GUILayout.BeginVertical(gs);
                else GUILayout.BeginVertical();

                userFolders[i].isActive = EditorGUILayout.BeginToggleGroup(userFolders[i].folderName, userFolders[i].isActive);
                    if (userFolders[i].isActive) {

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Show/Hide Trials")) {
                            userFolders[i].showFolders = !userFolders[i].showFolders;
                        }
                        if (GUILayout.Button("Offline Process")) {
                            userFolders[i].ProcessOffline();
                        }
                        GUILayout.EndHorizontal();
                        
                        if (userFolders[i].showFolders) {
                            foreach(UserTrial userTrial in userFolders[i].folderTrials) {
                                if (userTrial.allValid) {
                                    GUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField(userTrial.trialName);
                                    EditorGUILayout.LabelField("All files found!");
                                    GUILayout.EndHorizontal();
                                    continue;
                                }
                                EditorGUILayout.LabelField(userTrial.trialName);
                                foreach(KeyValuePair<string,bool> kvp in userTrial.fileIntegrityDict) {
                                    if (!kvp.Value) {
                                        GUILayout.BeginHorizontal();
                                        EditorGUILayout.LabelField("\t"+kvp.Key);
                                        EditorGUILayout.LabelField(kvp.Value.ToString());
                                        GUILayout.EndHorizontal();
                                    }
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



        /*
        groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
            myBool = EditorGUILayout.Toggle("Toggle", myBool);
            myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
        EditorGUILayout.EndToggleGroup();
        */
    }

    private List<UserFolder> ScanForFolders(string dir) {
        // Initialize the path to the user data
        string readFromDirectory = Path.Combine("Assets",dir);
        string pathToOmits = Path.Combine(readFromDirectory,"omits.csv");

        // Before anything... we have to look for omits
        Dictionary<string, Dictionary<string, List<TrialOmit>>> omits = new Dictionary<string, Dictionary<string, List<TrialOmit>>>();
        if (SaveSystemMethods.CheckFileExists(pathToOmits)) {
            Debug.Log("Loading omits data");
            List<TrialOmit> loadedOmits = LoadOmits(pathToOmits);
            foreach(TrialOmit omit in loadedOmits) {
                Debug.Log("New Omit: start=" + omit.startTimestamp + "\tend=" + omit.endTimestamp);
                if (!omits.ContainsKey(omit.participantName)) omits.Add(omit.participantName, new Dictionary<string, List<TrialOmit>>());
                if (!omits[omit.participantName].ContainsKey(omit.trialName)) omits[omit.participantName].Add(omit.trialName, new List<TrialOmit>());
                omits[omit.participantName][omit.trialName].Add(omit);
            }
        } else {
            Debug.Log("`omits.csv` does not exist inside the designated directory.");
        }

        // now load individual users and their folders
        List<UserFolder> newUserFolders = new List<UserFolder>();
        foreach(string folder in Directory.GetDirectories(readFromDirectory)) {
            // Each UserFolder comes with a `folderName` that technically equals participants' names.
            // If there's a matching participant name inside of `omits`, then we need to add those omits to this trial data.
            string participantName = Path.GetFileName(folder);
            Dictionary<string, List<TrialOmit>> participantOmits = new Dictionary<string, List<TrialOmit>>();
            if (omits.ContainsKey(participantName)) participantOmits = omits[participantName];

            UserFolder tempUserFolder = new UserFolder(participantName, folder, Directory.GetDirectories(folder), participantOmits);
            newUserFolders.Add(tempUserFolder);
        }
        return newUserFolders;
    }
    private List<TrialOmit> LoadOmits(string p) {
        TextAsset ta = (TextAsset)AssetDatabase.LoadAssetAtPath(p, typeof(TextAsset));
        List<TrialOmit> omits = new List<TrialOmit>();
        int numHeaders = TrialOmit.Headers.Count;
        string[] omitsData = SaveSystemMethods.ReadCSV(ta);
        int tableSize = omitsData.Length/numHeaders - 1;
        for(int i = 0; i < tableSize; i++) {
            int rowKey = numHeaders*(i+1);
            string[] row = omitsData.RangeSubset(rowKey,numHeaders);
            omits.Add(new TrialOmit(row));
        }
        return omits;
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

    public static void DrawPadding(int padding = 10) {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding));
        r.height = padding;
        r.x-=2;
        r.width += 6;
        EditorGUI.DrawRect(r,Color.clear);
    }
}
