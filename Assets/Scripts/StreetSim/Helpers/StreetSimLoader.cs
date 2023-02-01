using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using SerializableTypes;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class StreetSimLoader : MonoBehaviour
{

    public static StreetSimLoader loader;

    private void Awake() {
        loader = this;
    }

    #if UNITY_EDITOR
    
    public bool LoadDurationData(LoadedSimulationDataPerTrial trial, string filename, out List<RaycastHitDurationRow> rows) {
        rows = new List<RaycastHitDurationRow>();
        string assetPath = trial.assetPath+"/"+filename+".csv";
        if(!SaveSystemMethods.CheckFileExists(assetPath)) {
            Debug.Log("[RAYCASTER] ERROR: Cannot load textasset \""+assetPath+"\"!");
            return false;
        }
        TextAsset ta = (TextAsset)AssetDatabase.LoadAssetAtPath(assetPath, typeof(TextAsset));
        string[] pr = SaveSystemMethods.ReadCSV(ta);
        rows = ParseDurationData(trial, pr);
        return true;
    }
    public List<RaycastHitDurationRow> ParseDurationData(LoadedSimulationDataPerTrial trial, string[] data) {
        List<RaycastHitDurationRow> dataFormatted = new List<RaycastHitDurationRow>();
        int numHeaders = RaycastHitDurationRow.Headers.Count;
        int tableSize = data.Length/numHeaders - 1;
        for(int i = 0; i < tableSize; i++) {
            int rowKey = numHeaders*(i+1);
            string[] row = data.RangeSubset(rowKey, numHeaders);
            dataFormatted.Add(new RaycastHitDurationRow(row));
        }
        return dataFormatted;
    }

    public bool LoadFixationsData(LoadedSimulationDataPerTrial trial, string filename, out List<SGazePoint> points) {
        points = new List<SGazePoint>();
        string assetPath =  trial.assetPath+"/"+filename+".csv";
        if (!SaveSystemMethods.CheckFileExists(assetPath)) {
            Debug.Log("[RAYCASTER] ERROR: Cannot load textasset \""+assetPath+"\"!");
            return false;
        }
        TextAsset ta = (TextAsset)AssetDatabase.LoadAssetAtPath(assetPath, typeof(TextAsset));
        string[] pr = SaveSystemMethods.ReadCSV(ta);
        points = ParseFixationData(trial, pr);
        return true;
    }
    public List<SGazePoint> ParseFixationData(LoadedSimulationDataPerTrial trial, string[] data) {
        List<SGazePoint> dataFormatted = new List<SGazePoint>();
        int numHeaders = SGazePoint.Headers.Count;
        int tableSize = data.Length/numHeaders - 1;
        for(int i = 0; i < tableSize; i++) {
            int rowKey = numHeaders*(i+1);
            string[] row = data.RangeSubset(rowKey,numHeaders);
            dataFormatted.Add(new SGazePoint(row));
        }
        return dataFormatted;
    }


    public bool LoadGazePath(LoadedSimulationDataPerTrial trial, out LoadedGazeData newData) {
        string assetPath = trial.assetPath+"/gaze.csv";
        if (!SaveSystemMethods.CheckFileExists(assetPath)) {
            Debug.Log("[RAYCASTER] ERROR: Cannot load textasset \""+assetPath+"\"!");
            newData = default(LoadedGazeData);
            return false;
        }
        TextAsset ta = (TextAsset)AssetDatabase.LoadAssetAtPath(assetPath, typeof(TextAsset));
        string[] pr = SaveSystemMethods.ReadCSV(ta);
        List<RaycastHitRow> p = ParseGazeData(trial, pr);
        Debug.Log("[RAYCASTER] \""+trial.trialName+"\": Loaded " + p.Count.ToString() + " raw gazes");
        if (trial.trialOmits.Count == 0) {
            Debug.Log("[RAYCASTER] \""+trial.trialName+"\": No omits detected, current positions count to " + p.Count.ToString() + " gazes");
            newData = new LoadedGazeData(trial.trialName, ta, p);
        } else {
            List<RaycastHitRow> p2 = new List<RaycastHitRow>();
            foreach(RaycastHitRow rhr in p) {
                bool validRHR = true;
                foreach(TrialOmit omit in trial.trialOmits) {
                    if (rhr.timestamp >= omit.startTimestamp && rhr.timestamp < omit.endTimestamp) {
                        validRHR = false;
                        break;
                    }
                }
                if (validRHR) p2.Add(rhr);
            }
            Debug.Log("[RAYCASTER] \""+trial.trialName+"\": After omits, current positions count to " + p2.Count.ToString() + " gazes");
            newData = new LoadedGazeData(trial.trialName, ta, p2);
        }
        return true;
    }
    private List<RaycastHitRow> ParseGazeData(LoadedSimulationDataPerTrial trial, string[] data){
        List<RaycastHitRow> dataFormatted = new List<RaycastHitRow>();
        int numHeaders = (trial.simVersion != "Version3")
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

    #endif
}
