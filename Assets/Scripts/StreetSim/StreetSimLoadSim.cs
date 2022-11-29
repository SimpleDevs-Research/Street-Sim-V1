using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;

public class StreetSimLoadSim : MonoBehaviour
{
    public static StreetSimLoadSim LS;
    private bool m_initialized = false;
    public bool initialized { get=>m_initialized; set{} }

    [Header("REFERENCES")]
    public Transform userImitator;
    public Transform gazeCube, gazeRect;
    public GazePoint gazePointPrefab;
    public Transform cam360;
    public LayerMask gazeMask;

    [Header("PARTICIPANTS")]
    public string sourceDirectory;
    public List<string> participants = new List<string>();
    public Dictionary<string, List<LoadedSimulationDataPerTrial>> participantData = new Dictionary<string, List<LoadedSimulationDataPerTrial>>(); 
    private string m_currentParticipant = null;
    public string currentParticipant { get=>m_currentParticipant; set{} }
    public bool loadInitials = false;

    private void Awake() {
        LS = this;
        m_initialized = true;
    }

    public void Load() {
        // We can't do anything if our participants list is empty
        if (participants.Count == 0) {
            Debug.Log("[LOAD SIM] ERROR: Cannot parse participants if there aren't any participants...");
            return;
        }
        
        // We first get the absolute path to our save directory
        string p = SaveSystemMethods.GetSaveLoadDirectory(sourceDirectory);
        // We then get the path to the save directory from "Assets"
        string ap = "Assets/"+sourceDirectory + "/";
        
        // Confirm that our absolute save directory path exists. If not, we have to exit early
        Debug.Log("[LOAD SIM] Loading data from: \"" + p + "\"");
        if (!SaveSystemMethods.CheckDirectoryExists(p)) {
            Debug.Log("LOAD SIM] ERROR: Designated simulation folder does not exist.");
            return;
        }

        // For each participant in `participants`, we load each of their data.
        // If our data is successfully loaded, we save that into `participantData`
        participantData = new Dictionary<string, List<LoadedSimulationDataPerTrial>>();
        foreach(string participant in participants) {
            List<LoadedSimulationDataPerTrial> trials;
            if (LoadParticipantData(p,ap,participant,out trials)) {
                participantData.Add(participant,trials);
            }
        }
    }

    private bool LoadParticipantData(string p, string ap, string participantName, out List<LoadedSimulationDataPerTrial> trials) {
        string path = p;
        string assetPath = ap;
        if (SaveSystemMethods.CheckDirectoryExists(path+participantName+"/")) {
            assetPath = assetPath+participantName+"/";
            path = path+participantName+"/";
        } 
        // Depending on the way data is saved, sometimes we may have cases where the 
        //      participant's data is nested inside an additional layer. 
        // In such a case, we have to check if this is the case. This additional folder is typically the participant's name
        if (SaveSystemMethods.CheckDirectoryExists(path+participantName+"/")) {
            assetPath = assetPath+participantName+"/";
            path = path+participantName+"/";
        }

        // Prepare some things
        trials = new List<LoadedSimulationDataPerTrial>();
        int groupCount = StreetSim.S.trialGroups.Count;
        
        // Loop through number of possible trial groups
        for(int i = 0; i < groupCount; i++) {
            // Create assumed path to simulationMetadata_<i>.json
            string pathToData = path+"simulationMetadata_"+i.ToString()+".json";
            Debug.Log("[LOAD SIM] Attempting to load \""+pathToData+"\"");

            // Exit early to next group if we can't find that file
            if (!SaveSystemMethods.CheckFileExists(pathToData)) {
                Debug.Log("[LOAD SIM] ERROR: metadata #" + i.ToString() + " for \""+participantName+"\" does not appear to exist");
                continue;
            }

            // Attempt to load that simulation metadata            
            SimulationData simData;
            if (!SaveSystemMethods.LoadJSON<SimulationData>(pathToData, out simData)) {
                Debug.Log("[LOAD SIM] ERROR: Unable to read json data of metadata #"+i.ToString()+" for \""+participantName+"\"");
                continue;
            }

            Debug.Log("[LOAD SIM] Loaded Simulation Data #"+simData.simulationGroupNumber.ToString()+" for \""+participantName+"\"");
            foreach(string trialName in simData.trials) {
                if (!loadInitials && trialName.Contains("Initial")) {
                    Debug.Log("[LOAD SIM] Skipping \""+trialName+"\"");
                    continue;
                }
                Debug.Log("[LOAD SIM] Attempting to load trial \""+trialName+"\"");
                LoadedSimulationDataPerTrial newLoadedTrial = new LoadedSimulationDataPerTrial(trialName, assetPath+trialName);
                TrialData trialData;
                if (LoadTrialData(path+trialName+"/trial.json", out trialData)) {
                    newLoadedTrial.trialData = trialData;
                }
                if (StreetSimIDController.ID.LoadDataPath(newLoadedTrial, out LoadedPositionData newPositionData)) {
                    newLoadedTrial.positionData = newPositionData;
                }
                if (StreetSimRaycaster.R.LoadGazePath(newLoadedTrial, out LoadedGazeData newGazeData)) {
                    newLoadedTrial.gazeData = newGazeData;
                }
                trials.Add(newLoadedTrial);
            }
        }

        // Report our results, return false if trial count is 0.
        Debug.Log("[LOAD SIM] \""+participantName+"\": We have " + trials.Count.ToString() + " trials available for parsing");
        if (trials.Count == 0) {
            // We failed! We'll get them next time
            return false;
        }
        return true;
    }

    private bool LoadTrialData(string path, out TrialData trial) {
        if (!SaveSystemMethods.CheckFileExists(path)) {
            Debug.Log("[LOAD SIM] ERROR: Unable to find trial file \""+path+"\"");
            trial = default(TrialData);
            return false;
        }
        if (!SaveSystemMethods.LoadJSON<TrialData>(path, out trial)) {
            Debug.Log("[STREET SIM] ERROR: Unable to load json file \""+path+"\"...");
            return false;
        }
        return true;
    }

    public void StageParticipant(string participantName = null) {
        if (participantData.Count == 0 || participantName == null) {
            m_currentParticipant = null;
            return;
        }
        m_currentParticipant = (participantData.ContainsKey(participantName))
            ? participantName
            : null;
    }

    public void GetPixels() {
        /*
        Cubemap cubemap = cam360.GetComponent<BodhiDonselaar.EquiCam>().cubemap as Cubemap;
        var tex = new Texture2D(cubemap.width, cubemap.height, TextureFormat.RGB24, false);
        // Read screen contents into the texture        
		tex.SetPixels(cubemap.GetPixels(CubemapFace.PositiveX));        
		// Encode texture into PNG
		var bytes = tex.EncodeToPNG();      
		File.WriteAllBytes(Application.dataPath + "/"  + cubemap.name +"_PositiveX.png", bytes);       

		tex.SetPixels(cubemap.GetPixels(CubemapFace.NegativeX));
		bytes = tex.EncodeToPNG();     
		File.WriteAllBytes(Application.dataPath + "/"  + cubemap.name +"_NegativeX.png", bytes);       

		tex.SetPixels(cubemap.GetPixels(CubemapFace.PositiveY));
		bytes = tex.EncodeToPNG();     
		File.WriteAllBytes(Application.dataPath + "/"  + cubemap.name +"_PositiveY.png", bytes);       

		tex.SetPixels(cubemap.GetPixels(CubemapFace.NegativeY));
		bytes = tex.EncodeToPNG();     
		File.WriteAllBytes(Application.dataPath + "/"  + cubemap.name +"_NegativeY.png", bytes);       

		tex.SetPixels(cubemap.GetPixels(CubemapFace.PositiveZ));
		bytes = tex.EncodeToPNG();     
		File.WriteAllBytes(Application.dataPath + "/"  + cubemap.name +"_PositiveZ.png", bytes);       

		tex.SetPixels(cubemap.GetPixels(CubemapFace.NegativeZ));
		bytes = tex.EncodeToPNG();     
		File.WriteAllBytes(Application.dataPath + "/"  + cubemap.name   +"_NegativeZ.png", bytes);       
		DestroyImmediate(tex);
        */
    }
    private Texture2D ToTexture2D(RenderTexture rTex) {
		Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.ARGB32, false);
		RenderTexture old_rt = RenderTexture.active;
		RenderTexture.active = rTex;

		tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
		tex.Apply();

		RenderTexture.active = old_rt;
		return tex;
	}
}
