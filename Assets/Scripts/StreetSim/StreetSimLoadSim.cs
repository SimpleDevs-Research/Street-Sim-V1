using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using System.Linq;

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
    public Dictionary<float, bool> discretizations = new Dictionary<float,bool>() {
        {-10f,true},
        {-9f,true},
        {-8f,true},
        {-7f,true},
        {-6f,true},
        {-5f,true},
        {-4f,true},
        {-3f,true},
        {-2f,true},
        {-1f,true},
        {0f,true},
        {1f,true},
        {2f,true},
        {3f,true},
        {4f,true},
        {5f,true},
        {6f,true},
        {7f,true},
        {8f,true},
        {9f,true},
        {10f,true}
    };
    public int NumDiscretizations { get=>new List<float>(discretizations.Keys).Count; set{} }

    [Header("DATA")]
    public bool visualizeSphere = true;
    public float sphereRadius = 1f;
    //public int sphereAngle = 1; // must be a factor of 180'
    public int numViewDirections = 300;
    public List<Vector3> directions = new List<Vector3>();
    public float averageDistanceBetweenPoints = 0f;
    private IEnumerator sphereCoroutine = null;
    private IEnumerator GTSCoroutine = null;

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

        cam360.gameObject.SetActive(true);
        gazeCube.gameObject.SetActive(true);
        gazeRect.gameObject.SetActive(true);
        
        gazeCube.position = new Vector3(0f,1.5f,0f);
        gazeCube.rotation = Quaternion.identity;
        gazeCube.gameObject.layer = LayerMask.NameToLayer("PostProcessGaze");
        foreach(Transform child in gazeCube) child.gameObject.layer = LayerMask.NameToLayer("PostProcessGaze");
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

    public void GenerateSphereGrid() {
        directions = new List<Vector3>();

        float goldenRatio = (1 + Mathf.Sqrt (5)) / 2;
        float angleIncrement = Mathf.PI * 2 * goldenRatio;

        for (int i = 0; i < numViewDirections; i++) {
            float t = (float) i / numViewDirections;
            float inclination = Mathf.Acos (1 - 2 * t);
            float azimuth = angleIncrement * i;

            float x = Mathf.Sin (inclination) * Mathf.Cos (azimuth);
            float y = Mathf.Sin (inclination) * Mathf.Sin (azimuth);
            float z = Mathf.Cos (inclination);
            directions.Add(new Vector3(x,y,z));
        }

        /*
        points = new List<Vector3>();
        Vector3 destination;
        GameObject rotator = new GameObject("rotator");
        rotator.transform.position = Vector3.zero;
        rotator.transform.rotation = Quaternion.identity;
        rotator.transform.Rotate(Vector3.right,-90f);
        for(int x = 0; x < 180/sphereAngle; x++) {
            for (int y = 0; y < 360/sphereAngle; y++) {
                destination = rotator.transform.forward * sphereRadius;
                if (!points.Contains(destination)) points.Add(destination);
                rotator.transform.Rotate(Vector3.up,(float)sphereAngle);
                yield return null;
            }
            rotator.transform.Rotate(Vector3.right,(float)sphereAngle);
        }
        rotator.transform.rotation = Quaternion.identity;
        rotator.transform.Rotate(Vector3.forward,-90);
        for(int z = 0; z < 180/sphereAngle; z++) {
            for(int x = 0; x < 360/sphereAngle; x++) {
                destination = rotator.transform.forward * sphereRadius;
                if (!points.Contains(destination)) points.Add(destination);
                rotator.transform.Rotate(Vector3.right,(float)sphereAngle);
                yield return null;
            }
            rotator.transform.Rotate(Vector3.forward,(float)sphereAngle);
        }
        rotator.transform.rotation = Quaternion.identity;
        for(int y = 0; y < 180/sphereAngle; y++) {
            for(int z = 0; z < 360/sphereAngle; z++) {
                destination = rotator.transform.forward * sphereRadius;
                if (!points.Contains(destination)) points.Add(destination);
                rotator.transform.Rotate(Vector3.forward,(float)sphereAngle);
                yield return null;
            }
            rotator.transform.Rotate(Vector3.up,(float)sphereAngle);
        }
        Destroy(rotator);
        */
    }
    /*
    public void ROC(bool discretized = false) {
        if (GTSCoroutine != null) StopCoroutine(GTSCoroutine);
        GTSCoroutine = GroundTruthSaliency(discretization);
        StartCoroutine(GTSCoroutine)
    }
    */
    public void GroundTruthSaliency(bool discretized = false) {

        // Ground Truth Saliency is effectively all gaze data, from all participants.
        // However, what we're looking for here is not ground truth saliency, but the ROC generated from ground truth saliency.
        // To achieve this, we need to do the following:
        //  1. For every i^th user, we aggregate fixation from all participants other than the i^th user
        //  2. We then collect the fixation from the i^th user
        //  3. We essentially count the % of fixations that the i^th user matched with the aggregated fixation
        //      So for example, let's say we have i^th user have a total of 7 fixations. 5 of those fixations match fixations from other participants. This means the succuss hit rate for the i^th user is 5/7
        //  4. We do this for all participants. We then average the success hit rates by the total number of participants.
        //  5. We do this for every % of saliency. For example, the top 5% of saliency, then the top 10% of saliency, and so on.

        // How we're gonna do this is that we're going to generate fixations for each participant.
        // then for each individual participant, we subtract their fixations from the aggregated fixations.
        // The success rate therefore can then be calculated.
        GenerateSphereGrid();
        Vector3 origin = new Vector3(0f,1.5f,0f);
        List<string> participantList = new List<string>(participantData.Keys);
        Dictionary<string, Dictionary<Vector3, int>> aggregateFixations = new Dictionary<string, Dictionary<Vector3, int>>();
        for (int i = 0; i < participantList.Count; i++) {
            string name = participantList[i];
            Debug.Log("[LOAD SIM] GTS: PARSING " + name + "'s Data");
            Dictionary<Vector3, int> directionFixations = new Dictionary<Vector3, int>();
            foreach(LoadedSimulationDataPerTrial trial in participantData[name]) {
                Debug.Log(trial.trialName);
                // Grab sphere data
                List<GazePoint> spherePoints = StreetSimRaycaster.R.GetSpherePointsForTrial(trial);
                Debug.Log(name + ": " + trial.trialName + ": " + spherePoints.Count.ToString());
                // For each spherepoint, derive which directions that the GazePoint is closest to
                foreach(GazePoint point in spherePoints) {
                    foreach(Vector3 dir in directions) {
                        if(!directionFixations.ContainsKey(dir)) directionFixations.Add(dir,0);
                        if (Vector3.Distance(origin+dir*sphereRadius, point.transform.position) <= averageDistanceBetweenPoints) {
                            // Found a fixation
                            directionFixations[dir] += 1;
                        }
                    }
                }
            }
            // Aggregate all fixations for this participant
            aggregateFixations.Add(name, directionFixations);
        }

        for(int i = 0; i < participantList.Count; i++) {
            string name = participantList[i];
            Debug.Log("[LOAD SIM] Participant \""+name+"\" has these fixations:");
            foreach(KeyValuePair<Vector3, int> kvp in aggregateFixations[name]) {
                if (kvp.Value > 0) Debug.Log(kvp.Key.ToString() + ": " + kvp.Value.ToString());
            }
        }

        /*
        // Firstly, generate a list of all points
        Dictionary<Vector3, float> gazeFixationsAcrossParticipants = new Dictionary<Vector3, float>();
        Dictionary<Vector3, float> gazeFixationsForIthParticipant = new Dictionary<Vector3, float>();
        foreach(Vector3 dir in directions) {
            gazeFixationsAcrossParticipants.Add(dir,0f);
        }
        Vector3 origin = new Vector3(0f,1.5f,0f);
        // Secondly, generate a list of directions
        GenerateSphereGrid();
        // Thidly, get list of participant data in List form
        List<List<LoadedSimulationDataPerTrial>> participants = participantData.Values.ToList();
        // Fourthly, start to generate a percentage value that'll be used ubiquitously
        float percentage;
        // Fifthly, start to iterate through participants
        for(int i = 0; i < participants.Count; i++) {
            // The ith user must be left out of this iteration
            for(int j = 0; j < participants.Count; j++) {
                if (i == j) continue;
                // For this user, we need to generate the gaze fixations
                // Whether the gaze fixations are discretized or not is provided as a parameter to this method
                // For each user, for each trial, we can get the gaze points for sphereGaze by calling StreetSimRaycaster.R.ReplayRecord()
                // ReplayRecord accepts a LoadedSimulationDataPerTrial and a bool
                foreach(LoadedSimulationDataPerTrial trial in participants[j]) {
                    if (StreetSimRaycaster.R.ReplayRecord(trial,false)) {
                        // Grab sphere data
                        List<GazePoint> spherePoints = StreetSimRaycaster.R.GetAllSphereGazePoints();
                        // For each spherepoint, derive which directions that the GazePoint is closest to
                        foreach(GazePoint point in spherePoints) {
                            foreach(Vector3 dir in directions) {
                                if (Vector3.Distance(origin+dir*sphereRadius, point.transform.position) <= averageDistanceBetweenPoints) {
                                    // Found a fixation
                                    gazeFixationsAcrossParticipants[dir] += 1f;
                                }
                            }
                        }
                    }
                }
            }
            // Now, we do the replay record for each trial for ith participant
            foreach(LoadedSimulationDataPerTrial trial in participants[i]) {
                if (StreetSimRaycaster.R.ReplayRecord(trial,false)) {
                    // Grab sphere data
                    List<GazePoint> spherePoints =StreetSimRaycaster.R.GetAllSphereGazePoints();
                    // For each spherepoint, derive which directions that the GazePoint is closest to
                    foreach(GazePoint point in spherePoints) {
                        foreach(Vector3 dir in directions) {
                            if (Vector3.Distance(origin+dir*sphereRadius, point.transform.position) <= averageDistanceBetweenPoints) {
                                // Found a fixation
                                gazeFixationsForIthParticipant[dir] += 1f;
                            }
                        }
                    }
                }
            }
            // Now for each direction, we have to judge accuracy.
            int accurate = 0, inaccurate = 0;
            foreach(Vector3 dir in directions) {
                if (gazeFixationsForIthParticipant[dir] > 0 && gazeFixationsAcrossParticipants[dir] > 0) {
                    // this is a case of accuracy
                    accurate += 1;
                } else if (gazeFixationsForIthParticipant[dir] > 0 && gazeFixationsAcrossParticipants[dir] == 0) {
                    // This is an inaccurate count
                    inaccurate += 1;
                }
            }
            // Accuracy is measured by 
        }
        */
    }

    public float GetDiscretizationFromIndex(int i) {
        return new List<float>(discretizations.Keys)[i];
    }
    public void ToggleDiscretization(float z) {
        discretizations[z] = !discretizations[z];
        StreetSimRaycaster.R.ToggleDiscretization(z);
    }

    public void PlaceCam(float z) {
        cam360.position = new Vector3(0f,1.5f,z);
    }
}
