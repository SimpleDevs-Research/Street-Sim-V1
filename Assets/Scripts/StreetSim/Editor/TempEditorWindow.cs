using System;
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

    public SimulationData simData;

    public UserFolder(string folderName, string folderPath, string[] trials, Dictionary<string, List<TrialOmit>> omits) {
        this.folderName = folderName;
        this.folderPath = folderPath;
        this.omits = omits;
        this.isActive = true;

        if (!GetSimulationMetadata()) {
            this.allValid = false;
            return;
        }

        this.folderTrials = new List<UserTrial>();
        bool tempAllValid = true;
        List<string> pathsToTrials = new List<string>(trials).Where(t=>!Path.GetFileName(t).StartsWith("Initial")).ToList();
        foreach(string trialPath in pathsToTrials) {
            string trialName = Path.GetFileName(trialPath);
            List<TrialOmit> trialOmits = new List<TrialOmit>();
            if (omits.ContainsKey(trialName)) trialOmits = this.omits[trialName];

            UserTrial newTrial = new UserTrial(trialName, trialPath, trialOmits, this.simData);
            this.folderTrials.Add(newTrial);
            tempAllValid = tempAllValid && newTrial.isValid;
        }

        this.allValid = tempAllValid;
        this.showFolders = false;
    }

    public bool GetSimulationMetadata() {
        // Find files that follow the pattern "simulationMetadata_#"\
        List<string> pathsToMetadata = new List<string>(Directory.GetFiles(folderPath)).Where(f=>Path.GetFileName(f).StartsWith("simulationMetadata_") && f.Substring(f.LastIndexOf(".")+1) != "meta").ToList();

        this.simData = new SimulationData(this.folderName, -1, "", "");
        SimulationData tempSimData = new SimulationData(this.folderName, -1, "", "");

        foreach(string metadataPath in pathsToMetadata) {
            if (!SaveSystemMethods.CheckFileExists(metadataPath)) {
                Debug.LogError("Cannot load simulation metadata \""+metadataPath+"\"!");
                continue;
            }
            if (!SaveSystemMethods.LoadJSON<SimulationData>(metadataPath, out tempSimData)) {
                Debug.LogError("Unable to load json file \""+metadataPath+"\"...");
                continue;
            }

            this.simData.duration += tempSimData.duration;
            this.simData.trials = this.simData.trials.Concat(tempSimData.trials).ToList();
            this.simData.version = tempSimData.version;
        }

        return this.simData.duration > 0f && this.simData.trials.Count > 0;
    }

    public void ProcessOffline(List<ExperimentID> ids) {
        foreach(UserTrial trial in folderTrials) {
            trial.ProcessOffline(ids);
        }
    }
    public void ProcessOnline(List<ExperimentID> ids) {
        /*
        if (StreetSimLoadSim.LS == null) {
            Debug.LogError("Make sure that the scene is running first!");
            return;
        }
        */
        foreach(UserTrial trial in folderTrials) {
            trial.ProcessOnline(ids);
        }
    }
}

[System.Serializable]
public class UserTrial {
    // Basic Info
    public string trialName;
    public string pathToTrial;
    public string simVersion;
    TrialData trialData;
    
    // File Integrity
    public Dictionary<string,bool> fileIntegrityDict;
    public List<string> foundFiles;
    public bool isValid, filesValid;
    public List<TrialOmit> omits;

    public bool firstSet;
    public Vector3 firstOrigin, firstDirTo, firstPos, firstGazeDir, firstTargetPos;

    public UserTrial(string trialName, string pathToTrial, List<TrialOmit> omits, SimulationData simData) {
        // Save a reference to this file
        this.trialName = trialName;
        this.pathToTrial = pathToTrial;
        this.omits = omits;
        this.filesValid = false;
        this.firstSet = false;
        this.simVersion = simData.version;
        this.isValid = GetTrialData(out trialData);
        if (!this.isValid) {
            Debug.LogError("COULD NOT LOAD TRIAL \"" + this.trialName + "\"");
            return;
        }

        // Check files
        CheckFileIntegrity();
    }

    public void CheckFileIntegrity() {
        // Load ALL files (excluding META files, since we don't care about those)
        this.foundFiles = new List<string>(Directory.GetFiles(this.pathToTrial)).Select(f=>Path.GetFileName(f)).Where(f=>f.Substring(f.LastIndexOf('.')+1)!="meta").ToList();

        // Even if a trial is "valid", it does not mean that all the post-processed files are there.
        // For now, know that a properly-loaded and processed user trial SHOULD have the following new files:
        // - "correctedAttempts.csv" - Offline
        // - "averageFixations.csv" - ONLINE
        // - "discretizedFixations.csv" - ONLINE
        // - "fixationsByDirection.csv" - offline (but needs 'averageFixations.csv')
        // - "gazeDurationsByAgent.csv" - offline
        // - "gazeDurationsByHit.csv" - offline
        // - "gazeDurationsByIndex.csv" - offline

        List<string> filesToCheck = new List<string>{
            "correctedAttempts.csv",
            "averageFixations.csv",
            "discretizedFixations.csv",
            "fixationsByDirection.csv",
            "gazeDurationsByAgent.csv",
            "gazeDurationsByHit.csv",
            "gazeDurationsByIndex.csv"
        };
        this.fileIntegrityDict = new Dictionary<string,bool>();
        bool tempAllValid = true;
        foreach(string fileToCheck in filesToCheck) {
            bool thisFileIsValid = CheckFile(fileToCheck, this.foundFiles);
            this.fileIntegrityDict.Add(fileToCheck, thisFileIsValid);
            tempAllValid = tempAllValid && thisFileIsValid;
        }

        // If any of these files are missing, they must be processed
        this.filesValid = tempAllValid;
    }

    public bool CheckFile(string filename, List<string> files) {
        return files.IndexOf(filename) > -1;
    }

    public void ProcessOffline(List<ExperimentID> ids) {
        ProcessAttempts(ids);
        ProcessGazeDurationsByHit();
    }
    public void ProcessOnline(List<ExperimentID> ids) {
        // We can check if the scene is running if there is a `StreetSimLoadSim.LS` that is not 
        ProcessDiscretizedFixations();
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
    public bool GetPositionsData(out List<StreetSimTrackable> positions) {
        
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
        //Debug.Log("Loaded " + p.Count.ToString() + " raw positions");
        
        // Filter out all StreetSimTrackables in `p` that might exist in trial.trialOmits
        if (omits.Count == 0) {
            //Debug.Log("\""+trialName+"\": No omits detected, current positions count to " + p.Count.ToString() + " positions");
            positions = p;
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
            //Debug.Log("[\""+trialName+"\": After omits, current positions count to " + p2.Count.ToString() + " positions");
            positions = p2;
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
        }

        return true;
    }
    public bool GetGazeData(out List<RaycastHitRow> gazes) {

        List<RaycastHitRow> ParseGazeData(string[] data){
            List<RaycastHitRow> dataFormatted = new List<RaycastHitRow>();
            int numHeaders = (this.simVersion != "Version3")
                ? RaycastHitRow.HeadersOld.Count 
                : RaycastHitRow.Headers.Count;
            int tableSize = data.Length/numHeaders - 1;
            for(int i = 0; i < tableSize; i++) {
                int rowKey = numHeaders*(i+1);
                string[] row = data.RangeSubset(rowKey,numHeaders);
                dataFormatted.Add(new RaycastHitRow(row));
            }
            return dataFormatted;
        }

        string assetPath = Path.Combine(pathToTrial,"gaze.csv");
        gazes = null;
        if (!SaveSystemMethods.CheckFileExists(assetPath)) {
            Debug.LogError("Cannot load gaze data from \""+assetPath+"\"!");
            return false;
        }

        TextAsset ta = (TextAsset)AssetDatabase.LoadAssetAtPath(assetPath, typeof(TextAsset));
        string[] pr = SaveSystemMethods.ReadCSV(ta);
        List<RaycastHitRow> p = ParseGazeData(pr);
        //Debug.Log("\""+trialName+"\": Loaded " + p.Count.ToString() + " raw gazes");

        if (omits.Count == 0) {
            gazes = p;
        } else {
            List<RaycastHitRow> p2 = new List<RaycastHitRow>();
            foreach(RaycastHitRow rhr in p) {
                bool validRHR = true;
                foreach(TrialOmit omit in omits) {
                    if (rhr.timestamp >= omit.startTimestamp && rhr.timestamp < omit.endTimestamp) {
                        validRHR = false;
                        break;
                    }
                }
                if (validRHR) p2.Add(rhr);
            }
            gazes = p2;
        }
        //Debug.Log("\""+trialName+"\": gaze points count to " + gazes.Count.ToString() + " / " + p.Count.ToString() + " rows");
        return true;
    }

    public void ProcessAttempts(List<ExperimentID> ids) {
        // Confirm that we have 1) positions.csv data, and 2) attempts.csv data
        List<StreetSimTrackable> positions;
        List<TrialAttempt> attempts;  
        List<TrialAttempt> newAttempts;
        bool userFound = false;
        string newAttemptsPath = Path.Combine(pathToTrial,"correctedAttempts.csv");      

        if (!GetPositionsData(out positions)) {
            Debug.LogError("Could not load positions data for \"" + trialName + "\"");
            return;
        }

        if (!GetAttemptsData(out attempts, out userFound)) {
            Debug.LogError("Could not load original attempts for \"" + trialName + "\"");
            return;
        }

        Debug.Log("Original Attempts Size: " + attempts.Count.ToString());
        
        // If "User" is among the ids in our attempts list, we just need to port this data into a new "assumedAttempts.csv" file
        // However, if "User" is NOT among the ids in our attempts list, we need to interpret them from existing position data
        if (!userFound) {
            Debug.LogError("Could not find a User inside attempts for " + trialName + ". Deriving from positional data...");
            Dictionary<string, TrialAttempt> currentAttempts = new Dictionary<string, TrialAttempt>();
            newAttempts = attempts;
            StreetSimTrackable trackable, prevTrackable;
            for(int i = 1; i < positions.Count; i++) {
                trackable = positions[i];
                prevTrackable = positions[i-1];
                if (trackable.id != "User") continue;
                if (omits.Count > 0) {
                    bool trackableIsValid = true;
                    foreach(TrialOmit omit in omits) {
                        if (prevTrackable.timestamp >= omit.startTimestamp && prevTrackable.timestamp < omit.endTimestamp) {
                            trackableIsValid = false;
                            break;
                        }
                    }
                    if (!trackableIsValid) continue;
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
                        newAttempts.Add(cAttempt);
                        currentAttempts.Remove(trackable.id);
                        continue;
                    }
                    if (trackable.localPosition_z <= -2.75f && currentAttempts.ContainsKey(trackable.id)) {
                        // In this case, the person got to the other end of the sidewalk. This is a successful attempt
                        TrialAttempt cAttempt = currentAttempts[trackable.id];
                        cAttempt.endTime = prevTrackable.timestamp;
                        cAttempt.successful = true;
                        cAttempt.reason = "[ASSUMED] Successfully reached the destination sidewalk";
                        newAttempts.Add(cAttempt);
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
                        newAttempts.Add(cAttempt);
                        currentAttempts.Remove(trackable.id);
                        continue;
                    }
                    if (trackable.localPosition_z >= 2.75f && currentAttempts.ContainsKey(trackable.id)) {
                        // In this case, the person got to the other end of the sidewalk. This is a successful attempt
                        TrialAttempt cAttempt = currentAttempts[trackable.id];
                        cAttempt.endTime = prevTrackable.timestamp;
                        cAttempt.successful = true;
                        cAttempt.reason = "[ASSUMED] Successfully reached the destination sidewalk";
                        newAttempts.Add(cAttempt);
                        currentAttempts.Remove(trackable.id);
                        continue;
                    }
                }
            }
            // We need to clean up any attempts remaining. We automatically label them as successful
            prevTrackable = positions[positions.Count-1];
            foreach(KeyValuePair<string, TrialAttempt> kvp in currentAttempts) {
                TrialAttempt cAttempt = kvp.Value;
                cAttempt.endTime = prevTrackable.timestamp;
                cAttempt.successful = true;
                cAttempt.reason = "[ASSUMED] End of Trial";
                newAttempts.Add(cAttempt);
            }
        } else {
            Debug.Log("User found inside attempts for " + trialName + ".");
            newAttempts = attempts;
        }

        Debug.Log("New Corrected Attempts Count: " + newAttempts.Count.ToString());

        // Now we save the file
        if (SaveSystemMethods.SaveCSV<TrialAttempt>(newAttemptsPath,TrialAttempt.Headers,newAttempts)) {
            Debug.Log(trialName + ": Saved Assumed Trial Attempts");
        } else {
            Debug.LogError(trialName + ": Could not save Assumed Trial Attempts");
        }

        // Recheck file integrity
        CheckFileIntegrity();
    }
    public void ProcessGazeDurationsByHit() {

        int prevFrameIndex = -1;
        int curFrameIndex = -1;
        List<RaycastHitRow> gazes;
        Dictionary<string, Dictionary<string,RaycastHitDurationRow>> cached = new Dictionary<string, Dictionary<string,RaycastHitDurationRow>>();
        Dictionary<string, Dictionary<string, bool>> cachedFound = new Dictionary<string, Dictionary<string,bool>>();
        List<RaycastHitDurationRow> durationRows = new List<RaycastHitDurationRow>();

        if (!GetGazeData(out gazes)) {
            Debug.LogError("Could not load gaze data for \"" + trialName + "\"");
            return;
        }

        for(int i = 0; i < gazes.Count; i++) {
            RaycastHitRow row = gazes[i];
            curFrameIndex = row.frameIndex;
            if (curFrameIndex != prevFrameIndex) {
                if (cachedFound.Count > 0) {
                    // delete any entries in `cached` whose results in `cachedFound` are false
                    foreach(KeyValuePair<string,Dictionary<string,bool>> kvp1 in cachedFound) {
                        if (kvp1.Value.Count == 0) continue;
                        foreach(KeyValuePair<string,bool> kvp2 in kvp1.Value) {
                            if (!kvp2.Value) {
                                // This is false from our last index. This means that this gaze fixation has ended becaus the previous timestep did not have this hitID recorded
                                durationRows.Add(cached[kvp1.Key][kvp2.Key]);
                                cached[kvp1.Key].Remove(kvp2.Key);
                            }
                        }
                        if (cached[kvp1.Key].Count == 0) cached.Remove(kvp1.Key);
                    }
                }
                cachedFound = new Dictionary<string, Dictionary<string,bool>>();
                foreach(string key1 in cached.Keys) { 
                    cachedFound.Add(key1,new Dictionary<string,bool>()); 
                    foreach(string key2 in cached[key1].Keys) {
                        cachedFound[key1][key2] = false;
                    }
                }
                prevFrameIndex = curFrameIndex;
            }
            if (!cached.ContainsKey(row.agentID)) {
                cached.Add(row.agentID, new Dictionary<string, RaycastHitDurationRow>());
                cached[row.agentID].Add(row.hitID, new RaycastHitDurationRow(row.agentID, row.hitID, -1, row.timestamp, row.frameIndex, i));
            } else {
                if (!cached[row.agentID].ContainsKey(row.hitID)) {
                    cached[row.agentID].Add(row.hitID, new RaycastHitDurationRow(row.agentID, row.hitID, -1, row.timestamp, row.frameIndex, i));
                } else {
                    cached[row.agentID][row.hitID].endTimestamp = row.timestamp;
                    cached[row.agentID][row.hitID].endFrameIndex = row.frameIndex;
                    cached[row.agentID][row.hitID].AddIndex(i);
                }
            }
            if(cachedFound.ContainsKey(row.agentID) && cachedFound[row.agentID].ContainsKey(row.hitID)) {
                cachedFound[row.agentID][row.hitID] = true;
            }
            
        }
        // Get the last few cached RaycastHitDurationRows
        foreach(Dictionary<string,RaycastHitDurationRow> rows in cached.Values) {
            foreach(RaycastHitDurationRow row in rows.Values) {
                durationRows.Add(row);
            }
        }

        // Now we save the file
        string gazeDurationsByHitPath = Path.Combine(pathToTrial,"gazeDurationsByHit.csv");
        if (SaveSystemMethods.SaveCSV<RaycastHitDurationRow>(gazeDurationsByHitPath,RaycastHitDurationRow.Headers,durationRows)) {
            Debug.Log(trialName + ": Saved gaze durations by hit");
        } else {
            Debug.LogError(trialName + ": Could not save gaze durations by hit...");
        }

        // Recheck file integrity
        CheckFileIntegrity();
    }
    public void ProcessDiscretizedFixations() {
        Vector3 GetClosestPointToLine(Vector3 origin, Vector3 direction, Vector3 point, out float dist) {
            Vector3 lhs = point - origin;
            float dotP = Vector3.Dot(lhs, direction);
            Vector3 closestPoint = origin + direction * dotP;
            dist = (closestPoint-point).magnitude;
            return closestPoint;
        }
        bool PointOnSphere(Vector3 participantPos, Vector3 origin, Vector3 gazeDir, float radius, out Vector3 estimatedPointOnSphere) {

            //Debug.Log(participantPos.ToString() + " | " + origin.ToString() + " | " + gazeDir.ToString());
            estimatedPointOnSphere = participantPos;
            float intersectionDistance;
            Vector3 intersectionDotPoint = GetClosestPointToLine(participantPos, gazeDir, origin, out intersectionDistance);
            estimatedPointOnSphere = intersectionDotPoint;

            /*
            // There are some assumptions we are running with. Namely that the participant is INSIDE the sphere.
            Debug.Log(radius.ToString() + " | " + intersectionDistance.ToString());
            float x1_before = Mathf.Pow(radius,2f) - Mathf.Pow(intersectionDistance,2f);
            if (x1_before < 0f) {
                Debug.LogError("\"x1_before\": " + x1_before.ToString() + " cannot be negative!");
                return false;
            }
            float x1 = Mathf.Sqrt(x1_before);

            float distanceBetweenParticipantAndOrigin = Vector3.Distance(participantPos, origin);
            float x2_before = Mathf.Pow(distanceBetweenParticipantAndOrigin,2f) - Mathf.Pow(intersectionDistance,2f);
            if (x2_before < 0f) {
                Debug.LogError("\"x2_before\": " + x2_before.ToString() + " cannot be negative!");
                return false;
            }
            float x2 = Mathf.Sqrt(x2_before);

            float totalDistance = x1+x2;

            estimatedPointOnSphere = participantPos + gazeDir.normalized * totalDistance;
            */
            return true;
        }

        List<RaycastHitRow> gazes;
        if (!GetGazeData(out gazes)) {
            Debug.LogError("\""+trialName+"\": Could not get gaze data when calculating discretized fixations");
            return;
        }

        // Because I was a dingus, we didn't store the position of the user at the moment of the gaze. HOWEVER, we can solve that by getting the user's position from `GetPositionsData()`.
        List<StreetSimTrackable> allPositions;
        if (!GetPositionsData(out allPositions)) {
            Debug.LogError("\""+trialName+"\": Could not get positions data when calculating discretized fixations");
            return;
        }
        List<StreetSimTrackable> playerPositions = allPositions.Where(p=>p.id == "User").ToList();
        if (playerPositions.Count == 0) {
            Debug.LogError("\""+trialName+"\": No positiosn attributed to the user themselves was found among position data");
            return;
        }

        // Let's get a dictionary that finds the player's position relative to a frame index
        Dictionary<int, StreetSimTrackable> playerPositionsByFrameIndex = new Dictionary<int, StreetSimTrackable>();
        foreach(StreetSimTrackable trackable in playerPositions) {
            if (!playerPositionsByFrameIndex.ContainsKey(trackable.frameIndex)) playerPositionsByFrameIndex.Add(trackable.frameIndex, trackable);
            else {
                Debug.LogError("FOUND A DUPLICATE POSITION > OVERWRITING...");
                playerPositionsByFrameIndex[trackable.frameIndex] = trackable;
            }
        }

        Vector3 positionMultiplier = Vector3.one;
        if (this.trialData.direction == "NorthToSouth") {
            // We flip everything around
            positionMultiplier = new Vector3(-1f,1f-1f);
        }
        float radius = Mathf.Sqrt(Mathf.Pow(0.5f,2f) + Mathf.Pow(0.5f,2f));

        Vector3 estimatedOrigin = Vector3.zero;
        Vector3 estimatedDir = Vector3.zero;
        Vector3 estimatedPos = Vector3.zero;
        Vector3 estimatedGazeDir = Vector3.zero;
        Vector3 estimatedTargetPos = Vector3.zero;

        for (int i = 0; i < gazes.Count; i++) {
            // For now, let's just focus on the first valid gaze point
            RaycastHitRow first = gazes[i];
            // Find associated player position at this gaze's frameIndex.
            if (!playerPositionsByFrameIndex.ContainsKey(first.frameIndex)) {
                continue;
            }
            StreetSimTrackable firstPos = playerPositionsByFrameIndex[first.frameIndex];
            // Since we want orientations to always be in the SouthToNorth orientation, if the trial is set to NorthToSouth, then we rotate everything.
            Vector3 participantPos = Vector3.Scale(firstPos.localPosition, positionMultiplier);
            Vector3 gazeDir = new Vector3(first.raycastDirection_x * positionMultiplier.x, first.raycastDirection_y, first.raycastDirection_z * positionMultiplier.z);
            // Discretization
            Vector3 origin = new Vector3(
                Mathf.Round(participantPos.x),
                1f,
                Mathf.Round(participantPos.z)
            );
            // Do the calculation
            Vector3 targetPos;
            if (PointOnSphere(participantPos, origin, gazeDir, radius, out targetPos)) {
                Debug.Log("BLKASJGLJKSHFGJKLSHKLGHNSFLJKGFSDLK");
                estimatedOrigin = origin;
                estimatedDir = (targetPos - origin).normalized;
                estimatedPos = participantPos;
                estimatedGazeDir = gazeDir;
                estimatedTargetPos = targetPos;
                break;
            }
        }

        this.firstOrigin = estimatedOrigin;
        this.firstDirTo = estimatedDir;
        this.firstPos = estimatedPos;
        this.firstGazeDir = estimatedGazeDir;
        this.firstTargetPos = estimatedTargetPos;
        this.firstSet = true;
        Debug.Log(this.firstOrigin.ToString());
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
    bool idsScanned = false;

    List<ExperimentID> ids;

    void OnEnable() {
        // Remove delegate listener if it has previously
        // been assigned.
        SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
        // Add (or re-add) the delegate.
        SceneView.onSceneGUIDelegate += this.OnSceneGUI;
    }
    void OnDisable() {
        // When the window is destroyed, remove the delegate
        // so that it will no longer do any drawing.
        SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
    }

    public void OnSceneGUI(SceneView sceneView) {
        Handles.BeginGUI();
        if (userFolders.Count > 0) {
            for(int i = 0; i < userFolders.Count; i++) {
                if (userFolders[i].isActive) {
                        foreach(UserTrial userTrial in userFolders[i].folderTrials) {
                            if (userTrial.filesValid) {
                                if (userTrial.firstSet) {
                                    Handles.color = Color.red;
                                    Handles.DrawSolidDisc(userTrial.firstOrigin, Vector3.up, 1f);
                                    //Handles.DrawLine(userTrial.firstPos, userTrial.firstTargetPos, 1f);
                                    /*
                                    Handles.color = Color.blue;
                                    Handles.DrawLine(userTrial.firstOrigin, userTrial.firstTargetPos, 1f);
                                    */
                                }
                            }
                        }
                }
            }
        }
        Handles.EndGUI();
    }

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
        if (GUILayout.Button("Generate IDs Database")) ScanForIDs(out ids);
        if (GUILayout.Button("Scan for User Folders")) userFolders = ScanForFolders(userDataDirectory);
        if (GUILayout.Button("Empty User Folder Cache")) userFolders = new List<UserFolder>();
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
                            userFolders[i].ProcessOffline(ids);
                        }
                        if (GUILayout.Button("Online Process")) {
                            userFolders[i].ProcessOnline(ids);
                        }
                        GUILayout.EndHorizontal();
                        
                        foreach(UserTrial userTrial in userFolders[i].folderTrials) {
                            if (userFolders[i].showFolders) {
                                if (userTrial.filesValid) {
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
                            if (userTrial.firstSet) Debug.Log("GHA:KJD");
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

    private void ScanForIDs(out List<ExperimentID> activeIDs) {
        // We want to scan the hierarchy for `ExperimentID` components that are active
        activeIDs = new List<ExperimentID>(FindObjectsOfType<ExperimentID>()).Where(f=>f.gameObject.activeInHierarchy).ToList();
        Debug.Log("Active ExperimentIDs found: " + activeIDs.Count);
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
            // We only add those whose data are valid
            if (tempUserFolder.allValid) newUserFolders.Add(tempUserFolder);
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
