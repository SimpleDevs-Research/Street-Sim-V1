using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class ParticipantDataMap {
    public string participantName;
    public List<LoadedSimulationDataPerTrial> participantTrials;
    public ParticipantDataMap(string name, List<LoadedSimulationDataPerTrial> data) {
        this.participantName = name;
        this.participantTrials = data;
    }
}

public class StreetSimLoadSim : MonoBehaviour
{
    public static StreetSimLoadSim LS = null;
    private bool m_initialized = false;
    public bool initialized { get=>m_initialized; set{} }

    [Header("REFERENCES")]
    public Transform heightRef;
    public Transform userImitator;
    public Transform gazeCube, gazeRect;
    public GazePoint gazePointPrefab;
    public Transform cam360, xCam, yCam, zCam, xyzCam;
    public LayerMask gazeMask;
    public Transform gazeTrackPlacementPositionRef;
    public Material GazeOnObjectMaterial;

    [Header("PARTICIPANTS")]
    public string sourceDirectory;
    public List<string> participants = new List<string>();
    public TextAsset omitsFile = null;
    public Dictionary<string, Dictionary<string, List<TrialOmit>>> omits = new Dictionary<string, Dictionary<string, List<TrialOmit>>>();
    public Dictionary<string, List<LoadedSimulationDataPerTrial>> participantData = new Dictionary<string, List<LoadedSimulationDataPerTrial>>();
    private bool loadingParticipant = false, 
                    loadingAverageFixation = false, 
                    loadingDiscretizedFixation = false, 
                    loadingDurationsByIndex = false, 
                    loadingDurationsByHit = false,
                    loadingDurationsByAgent = false,
                    loadingAssumedAttempts = false;
    private LoadedSimulationDataPerTrial m_newLoadedTrial, currentLoadedTrial = null;
    public LoadedSimulationDataPerTrial newLoadedTrial { get=>m_newLoadedTrial; set{} }

    private string m_currentParticipant = null;
    public string currentParticipant { get=>m_currentParticipant; set{} }
    public Dictionary<float, bool> discretizations = new Dictionary<float,bool>() {
        {-4f,true},
        {-3f,true},
        {-2f,true},
        {-1f,true},
        {0f,true},
        {1f,true},
        {2f,true},
        {3f,true},
        {4f,true},
    };
    public int NumDiscretizations { get=>new List<float>(discretizations.Keys).Count; set{} }

    [Header("DATA")]
    [SerializeField] private bool m_generateSphereOnStart = false;
    [SerializeField] private bool m_visualizeSphere = false;
    public bool visualizeSphere {
        get => m_visualizeSphere;
        set { 
            m_visualizeSphere = value;
            ToggleSphereGrid();
        }
    }
    public float sphereRadius = 1f;
    //public int sphereAngle = 1; // must be a factor of 180'
    public int numViewDirections = 300;
    public List<Vector3> directions = new List<Vector3>();
    public Dictionary<Vector3, Color> directionColors = new Dictionary<Vector3, Color>();
    public float averageDistanceBetweenPoints = 0f;
    [SerializeField] private float m_visualDistanceBetweenPoints = 0.25f;
    public float visualDistanceBetweenPoints { 
        get=>m_visualDistanceBetweenPoints; 
        set {
            m_visualDistanceBetweenPoints = value;
            RescaleSphereGridPoints();
        }
    }
    private IEnumerator sphereCoroutine = null;
    private IEnumerator GTSCoroutine = null;
    private Dictionary<Vector3, GazePoint> directionRefPoints = new Dictionary<Vector3, GazePoint>();

    [Header("LOADING")]
    public bool loadInitials = false;
    [SerializeField] private bool determineFixationsOnLoad = true;
    private List<GazePoint> activeGazePoints = new List<GazePoint>();

    [Header("GAZE-OBJECT TRACKING")]
    [SerializeField] private List<ExperimentID> m_gazeObjectTracking = new List<ExperimentID>();
    public List<ExperimentID> gazeObjectTracking { get=>m_gazeObjectTracking; set{} }
    private Transform currentGazeObjectTracked = null;
    
    /*
    void OnDrawGizmosSelected() {
        if (directions.Count == 0 || !visualizeSphere) return;
        for(int i = 0; i < directions.Count; i++) {
            Vector3 dir = directions[i];
            Vector3 pos = cam360.position + dir*sphereRadius;
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(
                pos,                                      // position
                //controller.cam360.position - pos,      // normal
                visualDistanceBetweenPoints
                //((2*Mathf.PI*controller.sphereRadius*(float)controller.sphereAngle)/360f)*0.5f*Mathf.Pow(2f,0.5f) // radius
            );
        }
    }
    */

    private void Awake() {
        LS = this;
        m_initialized = true;
    }

    #if UNITY_EDITOR

    public void Start() {
        if (m_generateSphereOnStart) GenerateSphereGrid();
    }

    public void Load() {
        if (directions.Count == 0) GenerateSphereGrid();
        if (omitsFile != null) {
            Debug.Log("[LOAD SIM] Loading omits data");
            List<TrialOmit> loadedOmits = LoadOmits();
            foreach(TrialOmit omit in loadedOmits) {
                Debug.Log("New Omit: start=" + omit.startTimestamp + "\tend=" + omit.endTimestamp);
                if (!omits.ContainsKey(omit.participantName)) omits.Add(omit.participantName, new Dictionary<string, List<TrialOmit>>());
                if (!omits[omit.participantName].ContainsKey(omit.trialName)) omits[omit.participantName].Add(omit.trialName, new List<TrialOmit>());
                omits[omit.participantName][omit.trialName].Add(omit);
            }
            Debug.Log("[LOAD SIM] Loaded Omits: " + loadedOmits.Count.ToString() + "\n# Participants with Omits: " + omits.Count.ToString());
        }
        StartCoroutine(LoadCoroutine());
    }

    public List<TrialOmit> LoadOmits() {
        List<TrialOmit> omits = new List<TrialOmit>();
        int numHeaders = TrialOmit.Headers.Count;
        string[] omitsData = SaveSystemMethods.ReadCSV(omitsFile);
        int tableSize = omitsData.Length/numHeaders - 1;
        for(int i = 0; i < tableSize; i++) {
            int rowKey = numHeaders*(i+1);
            string[] row = omitsData.RangeSubset(rowKey,numHeaders);
            omits.Add(new TrialOmit(row));
        }
        return omits;
    }

    public IEnumerator LoadCoroutine() {
        // We can't do anything if our participants list is empty
        if (participants.Count == 0) {
            Debug.Log("[LOAD SIM] ERROR: Cannot parse participants if there aren't any participants...");
            yield break;
        }

        xCam.gameObject.SetActive(true);
        yCam.gameObject.SetActive(true);
        zCam.gameObject.SetActive(true);
        xyzCam.gameObject.SetActive(true);
        
        // We first get the absolute path to our save directory
        string p = SaveSystemMethods.GetSaveLoadDirectory(sourceDirectory);
        // We then get the path to the save directory from "Assets"
        string ap = "Assets/"+sourceDirectory + "/";
        
        // Confirm that our absolute save directory path exists. If not, we have to exit early
        Debug.Log("[LOAD SIM] Loading data from: \"" + p + "\"");
        if (!SaveSystemMethods.CheckDirectoryExists(p)) {
            Debug.Log("LOAD SIM] ERROR: Designated simulation folder does not exist.");
            yield break;
        }

        cam360.gameObject.SetActive(true);
        gazeCube.gameObject.SetActive(true);
        gazeRect.gameObject.SetActive(true);
        
        gazeCube.position = heightRef.position;
        gazeCube.rotation = Quaternion.identity;
        gazeCube.gameObject.layer = LayerMask.NameToLayer("PostProcessGaze");
        foreach(Transform child in gazeCube) child.gameObject.layer = LayerMask.NameToLayer("PostProcessGaze");

        // For each participant in `participants`, we load each of their data.
        // If our data is successfully loaded, we save that into `participantData`
        participantData = new Dictionary<string, List<LoadedSimulationDataPerTrial>>();
        Queue<string> participantsQueue = new Queue<string>(participants);
        while(participantsQueue.Count > 0) {
            string participant = participantsQueue.Dequeue();
            List<LoadedSimulationDataPerTrial> trials;
            loadingParticipant = true;
            StartCoroutine(LoadParticipantData(p,ap,participant));
            while(loadingParticipant) yield return null;
        }
    }

    private IEnumerator LoadParticipantData(string p, string ap, string participantName) {
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
        List<LoadedSimulationDataPerTrial> trials = new List<LoadedSimulationDataPerTrial>();
        int groupCount = StreetSim.S.trialGroups.Count;
        
        // Loop through number of possible trial groups
        for(int i = 0; i < groupCount; i++) {
            // Create assumed path to simulationMetadata_<i>.json
            string pathToData = path+"simulationMetadata_"+i.ToString()+".json";
            Debug.Log("[LOAD SIM] Attempting to load \""+pathToData+"\"");

            // Exit early to next group if we can't find that file
            if (!SaveSystemMethods.CheckFileExists(pathToData)) {
                Debug.Log("[LOAD SIM] ERROR: metadata #" + i.ToString() + " for \""+participantName+"\" does not appear to exist");
                yield return null;
                continue;
            }

            // Attempt to load that simulation metadata            
            SimulationData simData;
            if (!SaveSystemMethods.LoadJSON<SimulationData>(pathToData, out simData)) {
                Debug.Log("[LOAD SIM] ERROR: Unable to read json data of metadata #"+i.ToString()+" for \""+participantName+"\"");
                yield return null;
                continue;
            }

            Debug.Log("[LOAD SIM] Loaded Simulation Data #"+simData.simulationGroupNumber.ToString()+" for \""+participantName+"\"");
            foreach(string trialName in simData.trials) {
                if (!loadInitials && trialName.Contains("Initial")) {
                    Debug.Log("[LOAD SIM] Skipping \""+trialName+"\"");
                    yield return null;
                    continue;
                }
                Debug.Log("[LOAD SIM] Attempting to load trial \""+trialName+"\"");
                // Cut out early if this trial's folder doesn't exist
                if (!SaveSystemMethods.CheckDirectoryExists(path+trialName+"/")) {
                    Debug.Log("[LOAD SIM] WARNING: Skipping \""+trialName+"\" - unable to find directory");
                    continue;
                }
                List<TrialOmit> trialOmits = new List<TrialOmit>();
                if (omits.ContainsKey(participantName) && omits[participantName].ContainsKey(trialName)) trialOmits = omits[participantName][trialName];
                m_newLoadedTrial = new LoadedSimulationDataPerTrial(trialName, simData.version, assetPath+trialName, trialOmits);
                TrialData trialData;
                if (LoadTrialData(path+trialName+"/trial.json", out trialData)) m_newLoadedTrial.trialData = trialData;
                
                bool positionsLoaded = StreetSimIDController.ID.LoadDataPath(m_newLoadedTrial, out LoadedPositionData newPositionData);
                if (positionsLoaded) m_newLoadedTrial.positionData = newPositionData;
                bool gazesLoaded = StreetSimRaycaster.R.LoadGazePath(m_newLoadedTrial, out LoadedGazeData newGazeData);
                if (gazesLoaded) m_newLoadedTrial.gazeData = newGazeData;
                if (positionsLoaded && gazesLoaded && determineFixationsOnLoad) {
                    // We can now generate an averaged and discretized fixation map for this particular trial
                    // This is the averaged fixation map, regardless of discretization
                    loadingAverageFixation = true;
                    StartCoroutine(GenerateFixationMap());
                    while(loadingAverageFixation) yield return null;
                    loadingDiscretizedFixation = true;
                    StartCoroutine(GenerateDiscretizedFixationMap());
                    while(loadingDiscretizedFixation) yield return null;
                    loadingDurationsByIndex = true;
                    StartCoroutine(GenerateDurationsByTriangleIndex());
                    while(loadingDurationsByIndex) yield return null;
                    loadingDurationsByHit = true;
                    StartCoroutine(GenerateDurationsByHit());
                    while(loadingDurationsByHit) yield return null;
                    loadingDurationsByAgent = true;
                    StartCoroutine(GenerateDurationsByAgent());
                    while(loadingDurationsByAgent) yield return null;
                    loadingAssumedAttempts = true;
                    StartCoroutine(GenerateAssumedAttempts());
                    while(loadingAssumedAttempts) yield return null;
                }
                trials.Add(m_newLoadedTrial);
                yield return null;
            }
            yield return null;
        }

        // Report our results, return false if trial count is 0.
        Debug.Log("[LOAD SIM] \""+participantName+"\": We have " + trials.Count.ToString() + " trials available for parsing");
        if (trials.Count > 0) {
            participantData.Add(participantName,trials);
        }
        loadingParticipant = false;
        yield return null;
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
    public void ToggleAverageFixationMap(LoadedSimulationDataPerTrial trial) {
        ClearGazePoints();
        foreach(SGazePoint point in trial.averageFixations.gazePoints) {
            GazePoint newGazePoint = Instantiate(gazePointPrefab, point.GetWorldPosition(sphereRadius), Quaternion.identity) as GazePoint;
            activeGazePoints.Add(newGazePoint);
        }
        /*
        if (currentLoadedTrial != null) {
            foreach(GazePoint point in currentLoadedTrial.averageFixations.gazePoints) {
                point.gameObject.SetActive(false);
            }
            foreach(LoadedFixationData lfd in currentLoadedTrial.discretizedFixations.Values) {
                foreach(GazePoint point in lfd.gazePoints) {
                    point.gameObject.SetActive(false);
                }
            }
        }
        
        currentLoadedTrial = trial;
        foreach(GazePoint point in currentLoadedTrial.averageFixations.gazePoints) {
            point.gameObject.SetActive(true);
        }
        */
        foreach(KeyValuePair<Vector3,int> kvp in trial.averageFixations.fixations) {
            directionColors[kvp.Key] = (kvp.Value > 0) ? new Color(0.9f,0.9f,0.9f,0.2f) : Color.clear;
            if (kvp.Value == 0) continue;
            Debug.Log("\t"+kvp.Key.ToString() + ": " + kvp.Value);
        }
        RecolorSphereGrid();
    }
    public void ToggleDiscretizedFixationMap(LoadedSimulationDataPerTrial trial) {
        ClearGazePoints();
        foreach(KeyValuePair<Vector2, LoadedFixationData> kvp in trial.discretizedFixations) {
            Debug.Log("At position "+kvp.Key.ToString()+":");
            foreach(SGazePoint point in kvp.Value.gazePoints) {
                GazePoint newGazePoint = Instantiate(gazePointPrefab, point.GetWorldPosition(sphereRadius), Quaternion.identity) as GazePoint;
                activeGazePoints.Add(newGazePoint);
            }
            Debug.Log("\tPoints: " + kvp.Value.gazePoints.Count.ToString());
            foreach(KeyValuePair<Vector3,int> kvp2 in kvp.Value.fixations) {
                if (kvp2.Value == 0) continue;
                Debug.Log("\t\t- "+kvp2.Key.ToString() + ": " + kvp2.Value);
            }
        }

        /*
        if (currentLoadedTrial != null) {
            foreach(GazePoint point in currentLoadedTrial.averageFixations.gazePoints) {
                point.gameObject.SetActive(false);
            }
            foreach(LoadedFixationData lfd in currentLoadedTrial.discretizedFixations.Values) {
                foreach(GazePoint point in lfd.gazePoints) {
                    point.gameObject.SetActive(false);
                }
            }
        }
        currentLoadedTrial = trial;
        foreach(KeyValuePair<float, LoadedFixationData> kvp in currentLoadedTrial.discretizedFixations) {
            Debug.Log("At z="+kvp.Key.ToString()+":");
            foreach(GazePoint point in kvp.Value.gazePoints) {
                point.gameObject.SetActive(discretizations[kvp.Key]);
            }
            Debug.Log("\tPoints: " + kvp.Value.gazePoints.Count.ToString());
            foreach(KeyValuePair<Vector3,int> kvp2 in kvp.Value.fixations) {
                if (kvp2.Value == 0) continue;
                Debug.Log("\t\t- "+kvp2.Key.ToString() + ": " + kvp2.Value);
            }
        }
        */
    }
    public void ClearGazePoints() {
        while(activeGazePoints.Count > 0) {
            GazePoint curGazePoint = activeGazePoints[0];
            Destroy(curGazePoint.gameObject);
            activeGazePoints.RemoveAt(0);
        }
        ResetSphereGridColors();
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

    #endif

    public void GenerateSphereGrid() {
        directions = new List<Vector3>();
        directionColors = new Dictionary<Vector3, Color>();

        float goldenRatio = (1 + Mathf.Sqrt (5)) / 2;
        float angleIncrement = Mathf.PI * 2 * goldenRatio;

        for (int i = 0; i < numViewDirections; i++) {
            float t = (float) i / numViewDirections;
            float inclination = Mathf.Acos (1 - 2 * t);
            float azimuth = angleIncrement * i;

            float x = Mathf.Sin (inclination) * Mathf.Cos (azimuth);
            float y = Mathf.Sin (inclination) * Mathf.Sin (azimuth);
            float z = Mathf.Cos (inclination);

            Vector3 dir = new Vector3(x,y,z);
            directions.Add(dir);
            directionColors.Add(dir, new Color(0f,0f,0f,0f));
        }

        foreach(GazePoint point in directionRefPoints.Values) {
            Destroy(point.gameObject);
        }
        foreach(Vector3 dir in directions) {
            GazePoint newDirPoint = Instantiate(gazePointPrefab) as GazePoint;
            newDirPoint.transform.position = cam360.position + dir*sphereRadius;
            newDirPoint.SetColor(directionColors[dir]);
            newDirPoint.SetScale(m_visualDistanceBetweenPoints);
            directionRefPoints.Add(dir, newDirPoint);
        }

        // Save all directions into csv
        string postProcessDir = SaveSystemMethods.GetSaveLoadDirectory("PostProcessData_Ignore");
        string objectFilename = postProcessDir + "sphereGrid.csv";
        List<SDirection> sDirections = new List<SDirection>();
        for(int i = 0; i < directions.Count; i++) {
            sDirections.Add(new SDirection(i,directions[i]));
        }
         if (!SaveSystemMethods.SaveCSV<SDirection>(objectFilename, SDirection.Headers, sDirections)) {
            Debug.Log("[LOAD SIM] ERROR: Cannot save ROC agreement csv in \""+objectFilename+"\"");
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

    private void ToggleSphereGrid() {
        foreach(GazePoint point in directionRefPoints.Values) {
            point.gameObject.SetActive(m_visualizeSphere);
        }
    }

    public void ResetSphereGridColors() {
        foreach(Vector3 dir in directions) {
            directionColors[dir] = new Color(0f,0f,0f,0f);
        }
        RecolorSphereGrid();
    }
    public void RecolorSphereGrid() {
        foreach(KeyValuePair<Vector3, GazePoint> kvp in directionRefPoints) {
            kvp.Value.SetColor(directionColors[kvp.Key]);
        }
    }
    public void RescaleSphereGridPoints() {
        foreach(GazePoint point in directionRefPoints.Values) {
            point.SetScale(m_visualDistanceBetweenPoints);
        }
    }


    /*
    public void ROC(bool discretized = false) {
        if (GTSCoroutine != null) StopCoroutine(GTSCoroutine);
        GTSCoroutine = GroundTruthSaliency(discretization);
        StartCoroutine(GTSCoroutine)
    }
    */

    #if UNITY_EDITOR
    public void ManuallyGenerateFixationMap(LoadedSimulationDataPerTrial trial) {
        if (loadingAverageFixation || loadingDiscretizedFixation) {
            Debug.Log("[LOAD SIM] ERROR: Currently generating fixations for another trial.");
            return;
        }
        ClearGazePoints();
        m_newLoadedTrial = trial;
        loadingAverageFixation = true;
        StartCoroutine(GenerateFixationMap());
    }
    public IEnumerator GenerateFixationMap() {
        Vector3 origin = heightRef.position;

        // First attempt to load fixation data, if it exists
        List<SGazePoint> spherePoints;
        string assetPath =  m_newLoadedTrial.assetPath+"/averageFixations.csv";
        if (!StreetSimRaycaster.R.LoadFixationsData(m_newLoadedTrial, "averageFixations", out spherePoints)) {
            StreetSimRaycaster.R.loadingAverageFixation = true;
            StartCoroutine(StreetSimRaycaster.R.GetSpherePointsForTrial());
            while(StreetSimRaycaster.R.loadingAverageFixation) yield return null;
            spherePoints = StreetSimRaycaster.R.loadedAverageFixations;

            // We'll actually be saving this into a separate file called `averageFixations,csv`
            StreetSimRaycaster.R.SaveFixationsData(m_newLoadedTrial,"averageFixations",spherePoints);
        }

        Dictionary<Vector3, int> directionFixations = new Dictionary<Vector3, int>();
        foreach(SGazePoint point in spherePoints) {
            Vector3 closestDir = default(Vector3);
            bool closestDirFound = false;
            float closestDistance = Mathf.Infinity;
            foreach(Vector3 dir in directions) {
                if(!directionFixations.ContainsKey(dir)) directionFixations.Add(dir,0);
                //float distance = Vector3.Distance(origin+dir*sphereRadius, point.transform.position);
                float distance = Vector3.Distance(origin+dir*sphereRadius, point.GetWorldPosition(sphereRadius));
                if (distance <= averageDistanceBetweenPoints*2f && distance < closestDistance) {
                    // Found a possible fixation
                    closestDistance = distance;
                    closestDir = dir;
                    closestDirFound = true;
                }
            }
            if (closestDirFound) directionFixations[closestDir] += 1;
            //point.gameObject.SetActive(false);
        }
        m_newLoadedTrial.averageFixations = new LoadedFixationData(spherePoints, directionFixations);

        // Save into new file, if it doesn't exist yet
        string mappedAssetPath = m_newLoadedTrial.assetPath+"/fixationsByDirection.csv";
        if (!SaveSystemMethods.CheckFileExists(mappedAssetPath)) {
            Debug.Log("[LOAD SIM] ERROR: Cannot find asset \""+mappedAssetPath+"\"! Generating new file...");
            List<SDirectionFixation> toSave = new List<SDirectionFixation>();
            foreach(KeyValuePair<Vector3, int> kvp in directionFixations) {
                int i = directions.IndexOf(kvp.Key);
                if (i > -1) {
                    toSave.Add(new SDirectionFixation(i, kvp.Key, kvp.Value));
                }
            }
            if (!SaveSystemMethods.SaveCSV<SDirectionFixation>(mappedAssetPath,SDirectionFixation.Headers,toSave)) {
                Debug.Log("[LOAD SIM] ERROR: Cannot save \"fixationsByDirection.csv\"...");
            }
        }
        loadingAverageFixation = false;
        yield return null;
    }
    public void ManuallyGenerateDiscretizedFixationMap(LoadedSimulationDataPerTrial trial) {
        if (loadingAverageFixation || loadingDiscretizedFixation) {
            Debug.Log("[LOAD SIM] ERROR: Currently generating fixations for another trial.");
            return;
        }
        ClearGazePoints();
        m_newLoadedTrial = trial;
        loadingDiscretizedFixation = true;
        StartCoroutine(GenerateDiscretizedFixationMap());
    }
    public IEnumerator GenerateDiscretizedFixationMap() {

        // First attempt to load fixation data, if it exists
        List<SGazePoint> tempPoints;
        Dictionary<Vector2, List<SGazePoint>> spherePoints;
        if (StreetSimRaycaster.R.LoadFixationsData(m_newLoadedTrial, "discretizedFixations", out tempPoints)) {
            spherePoints = new Dictionary<Vector2, List<SGazePoint>>();
            foreach(SGazePoint point in tempPoints) {
                Vector2 pDiscretization = new Vector2(point.xDiscretization, point.zDiscretization);
                if (!spherePoints.ContainsKey(pDiscretization)) spherePoints.Add(pDiscretization, new List<SGazePoint>());
                spherePoints[pDiscretization].Add(point);
            }
        } else {
            StreetSimRaycaster.R.loadingDiscretizedFixation = true;
            StartCoroutine(StreetSimRaycaster.R.GetDiscretizedSpherePointsForTrial());
            while(StreetSimRaycaster.R.loadingDiscretizedFixation) yield return null;
            spherePoints = StreetSimRaycaster.R.loadedDiscretizedFixations;

            // We'll actually be saving this into a separate file called `discretizedFixations.csv`
            tempPoints = new List<SGazePoint>();
            foreach(KeyValuePair<Vector2, List<SGazePoint>> kvp in spherePoints) {
                foreach(SGazePoint p in kvp.Value) {
                    tempPoints.Add(p);
                }
            }
            StreetSimRaycaster.R.SaveFixationsData(m_newLoadedTrial,"discretizedFixations",tempPoints);
        }
        
        Dictionary<Vector2, LoadedFixationData> discretizedFixations = new Dictionary<Vector2, LoadedFixationData>();
        foreach(KeyValuePair<Vector2, List<SGazePoint>> kvp in spherePoints) {
            Vector2 xzIndex = kvp.Key;
            List<SGazePoint> points = new List<SGazePoint>(kvp.Value);
            Dictionary<Vector3, int> directionFixations = new Dictionary<Vector3, int>();
            Vector3 origin = new Vector3(xzIndex.x,heightRef.position.y,xzIndex.y);
            foreach(SGazePoint point in points) {
                Vector3 closestDir = default(Vector3);
                bool closestDirFound = false;
                float closestDistance = Mathf.Infinity;
                foreach(Vector3 dir in directions) {
                    if(!directionFixations.ContainsKey(dir)) directionFixations.Add(dir,0);
                    // float distance = Vector3.Distance(origin+dir*sphereRadius, point.transform.position);
                    float distance = Vector3.Distance(origin+dir*sphereRadius, point.GetWorldPosition(sphereRadius));
                    if (distance <= averageDistanceBetweenPoints*2f && distance < closestDistance) {
                        // Found a possible fixation
                        closestDistance = distance;
                        closestDir = dir;
                        closestDirFound = true;
                    }
                }
                if (closestDirFound) directionFixations[closestDir] += 1;
                //point.gameObject.SetActive(false);
            }
            discretizedFixations.Add(xzIndex, new LoadedFixationData(points, directionFixations));
            yield return null;
        }
        m_newLoadedTrial.discretizedFixations = discretizedFixations;
        loadingDiscretizedFixation = false;
        yield return null;
    }
    public IEnumerator GenerateDurationsByTriangleIndex() {
        List<RaycastHitDurationRow> durationRows;
        if (StreetSimRaycaster.R.LoadDurationData(m_newLoadedTrial, "gazeDurationsByIndex", out durationRows)) {
            // In this case, the function `StreetSimRaycaster.R.LoadDurationData` DID find a "durations.csv" file inside the trial. The outcome comes in durationRows
            m_newLoadedTrial.gazeDurationsByIndex = durationRows;
        }
        else {
            // In this case, the function `StreetSimRaycaster.R.LoadDurationByIndexData` could not find a "durations.csv" file inside the trial.
            // This means we need to generate one ourselves
            List<RaycastHitRow> gazes = m_newLoadedTrial.gazeData.gazes;
            int prevFrameIndex = -1;
            int curFrameIndex = -1;
            Dictionary<string, Dictionary<int,RaycastHitDurationRow>> cached = new Dictionary<string, Dictionary<int,RaycastHitDurationRow>>();
            Dictionary<string, Dictionary<int, bool>> cachedFound = new Dictionary<string, Dictionary<int,bool>>();
            durationRows = new List<RaycastHitDurationRow>();
            for(int i = 0; i < gazes.Count; i++) {
                RaycastHitRow row = gazes[i];
                curFrameIndex = row.frameIndex;
                if (curFrameIndex != prevFrameIndex) {
                    if(cachedFound.Count > 0) {
                        // delete any entries in `cached` whose results in `cachedFound` are false
                        foreach(KeyValuePair<string,Dictionary<int,bool>> kvp1 in cachedFound) {
                            if (kvp1.Value.Count == 0) continue;
                            foreach(KeyValuePair<int,bool> kvp2 in kvp1.Value) {
                                if (!kvp2.Value) {
                                    // This is false from our last index. This means that this gaze fixation has ended becaus the previous timestep did not have this hitID recorded
                                    durationRows.Add(cached[kvp1.Key][kvp2.Key]);
                                    cached[kvp1.Key].Remove(kvp2.Key);
                                }
                            }
                            if (cached[kvp1.Key].Count == 0) cached.Remove(kvp1.Key);
                        }
                    }
                    cachedFound = new Dictionary<string, Dictionary<int,bool>>();
                    foreach(string key1 in cached.Keys) { 
                        cachedFound.Add(key1,new Dictionary<int,bool>()); 
                        foreach(int key2 in cached[key1].Keys) {
                            cachedFound[key1][key2] = false;
                        }
                    }
                    prevFrameIndex = curFrameIndex;
                }
                if (!cached.ContainsKey(row.hitID)) {
                    cached.Add(row.hitID, new Dictionary<int, RaycastHitDurationRow>());
                    cached[row.hitID].Add(row.triangleIndex, new RaycastHitDurationRow(row.agentID, row.hitID, row.triangleIndex, row.timestamp, row.frameIndex, i));
                } else {
                    if (!cached[row.hitID].ContainsKey(row.triangleIndex)) {
                        cached[row.hitID].Add(row.triangleIndex, new RaycastHitDurationRow(row.agentID, row.hitID, row.triangleIndex, row.timestamp, row.frameIndex, i));
                    } else {
                        cached[row.hitID][row.triangleIndex].endTimestamp = row.timestamp;
                        cached[row.hitID][row.triangleIndex].endFrameIndex = row.frameIndex;
                        cached[row.hitID][row.triangleIndex].AddIndex(i);
                    }
                }
                if(cachedFound.ContainsKey(row.hitID) && cachedFound[row.hitID].ContainsKey(row.triangleIndex)) {
                    cachedFound[row.hitID][row.triangleIndex] = true;
                }
                yield return null;
            }
            // Get the last few cached RaycastHitDurationRows
            foreach(Dictionary<int,RaycastHitDurationRow> rows in cached.Values) {
                foreach(RaycastHitDurationRow row in rows.Values) {
                    durationRows.Add(row);
                }
            }
            // Need to save results into new `gazeDurations.csv`
            m_newLoadedTrial.gazeDurationsByIndex = durationRows;
            StreetSimRaycaster.R.SaveDurationData(m_newLoadedTrial, "gazeDurationsByIndex", durationRows);
        }
        Debug.Log(m_newLoadedTrial.trialName + ":" + durationRows.Count.ToString());
        loadingDurationsByIndex = false;
        yield return null;
    }
    public IEnumerator GenerateDurationsByHit() {
        List<RaycastHitDurationRow> durationRows;
        if (StreetSimRaycaster.R.LoadDurationData(m_newLoadedTrial, "gazeDurationsByHit", out durationRows)) {
            // In this case, the function `StreetSimRaycaster.R.LoadDurationData` DID find a "durations.csv" file inside the trial. The outcome comes in durationRows
            m_newLoadedTrial.gazeDurationsByHit = durationRows;
        }
        else {
            // In this case, the function `StreetSimRaycaster.R.LoadDurationByIndexData` could not find a "durations.csv" file inside the trial.
            // This means we need to generate one ourselves
            List<RaycastHitRow> gazes = m_newLoadedTrial.gazeData.gazes;
            int prevFrameIndex = -1;
            int curFrameIndex = -1;
            Dictionary<string, Dictionary<string,RaycastHitDurationRow>> cached = new Dictionary<string, Dictionary<string,RaycastHitDurationRow>>();
            Dictionary<string, Dictionary<string, bool>> cachedFound = new Dictionary<string, Dictionary<string,bool>>();
            durationRows = new List<RaycastHitDurationRow>();
            for(int i = 0; i < gazes.Count; i++) {
                RaycastHitRow row = gazes[i];
                curFrameIndex = row.frameIndex;
                if (curFrameIndex != prevFrameIndex) {
                    if(cachedFound.Count > 0) {
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
                yield return null;
            }
            // Get the last few cached RaycastHitDurationRows
            foreach(Dictionary<string,RaycastHitDurationRow> rows in cached.Values) {
                foreach(RaycastHitDurationRow row in rows.Values) {
                    durationRows.Add(row);
                }
            }
            // Need to save results into new `gazeDurations.csv`
            m_newLoadedTrial.gazeDurationsByHit = durationRows;
            StreetSimRaycaster.R.SaveDurationData(m_newLoadedTrial, "gazeDurationsByHit", durationRows);
        }
        Debug.Log(m_newLoadedTrial.trialName + ":" + durationRows.Count.ToString());
        loadingDurationsByHit = false;
        yield return null;
    }
    public IEnumerator GenerateDurationsByAgent() {
        List<RaycastHitDurationRow> durationRows;
        if (StreetSimRaycaster.R.LoadDurationData(m_newLoadedTrial, "gazeDurationsByAgent", out durationRows)) {
            // In this case, the function `StreetSimRaycaster.R.LoadDurationData` DID find a "durations.csv" file inside the trial. The outcome comes in durationRows
            m_newLoadedTrial.gazeDurationsByAgent = durationRows;
        }
        else {
            // In this case, the function `StreetSimRaycaster.R.LoadDurationData` could not find a "durations.csv" file inside the trial.
            // This means we need to generate one ourselves
            List<RaycastHitRow> gazes = m_newLoadedTrial.gazeData.gazes;
            int prevFrameIndex = -1;
            int curFrameIndex = -1;
            Dictionary<string, RaycastHitDurationRow> cached = new Dictionary<string, RaycastHitDurationRow>();
            Dictionary<string, bool> cachedFound = new Dictionary<string, bool>();
            durationRows = new List<RaycastHitDurationRow>();
            for(int i = 0; i < gazes.Count; i++) {
                RaycastHitRow row = gazes[i];
                curFrameIndex = row.frameIndex;
                if (curFrameIndex != prevFrameIndex) {
                    if(cachedFound.Count > 0) {
                        // delete any entries in `cached` whose results in `cachedFound` are false
                        foreach(KeyValuePair<string,bool> kvp in cachedFound) {
                            if (!kvp.Value) {
                                // This is false from our last index. This means that this gaze fixation has ended becaus the previous timestep did not have this hitID recorded
                                durationRows.Add(cached[kvp.Key]);
                                cached.Remove(kvp.Key);
                            }
                        }
                    }
                    cachedFound = new Dictionary<string, bool>();
                    foreach(string key in cached.Keys) { 
                        cachedFound[key] = false;
                    }
                    prevFrameIndex = curFrameIndex;
                }
                if (!cached.ContainsKey(row.agentID)) {
                    cached.Add(row.agentID, new RaycastHitDurationRow(row.agentID, row.agentID, -1, row.timestamp, row.frameIndex, i));
                } else {
                    cached[row.agentID].endTimestamp = row.timestamp;
                    cached[row.agentID].endFrameIndex = row.frameIndex;
                    cached[row.agentID].AddIndex(i);
                }
                if(cachedFound.ContainsKey(row.agentID)) {
                    cachedFound[row.agentID] = true;
                }
                yield return null;
            }
            // Get the last few cached RaycastHitDurationRows
            foreach(RaycastHitDurationRow row in cached.Values) {
                durationRows.Add(row);
            }
            // Need to save results into new `gazeDurations.csv`
            m_newLoadedTrial.gazeDurationsByAgent = durationRows;
            StreetSimRaycaster.R.SaveDurationData(m_newLoadedTrial, "gazeDurationsByAgent", durationRows);
        }
        Debug.Log(m_newLoadedTrial.trialName + ":" + durationRows.Count.ToString());
        loadingDurationsByAgent = false;
        yield return null;
    }
    public IEnumerator GenerateAssumedAttempts() {
        string originalAttemptsAssetPath = m_newLoadedTrial.assetPath+"/attempts.csv";
        string newAttemptsAssetPath = m_newLoadedTrial.assetPath+"/assumedAttempts.csv";
        // First, check if we have  `assumedAttempts.csv` first...
        if (SaveSystemMethods.CheckFileExists(newAttemptsAssetPath)) {
            Debug.Log("[LOAD SIM] 'assumedAttempts.csv' found for " + m_newLoadedTrial.trialName);
            loadingAssumedAttempts = false;
            yield break;
        } else {
            // Second, check if we even have a previous `attempts.csv` file
            if (!SaveSystemMethods.CheckFileExists(originalAttemptsAssetPath)) {
                Debug.LogError("[LOAD SIM] ERROR: \"attempts.csv\" file not efound for " + m_newLoadedTrial.trialName);
                loadingAssumedAttempts = false;
                yield break;
            } else {
                // We need to load in our attempts and check if "User" is among them.
                List<TrialAttempt> allAttempts = new List<TrialAttempt>();
                TextAsset ta = (TextAsset)AssetDatabase.LoadAssetAtPath(originalAttemptsAssetPath, typeof(TextAsset));
                string[] pr = SaveSystemMethods.ReadCSV(ta);
                int numHeaders = TrialAttempt.Headers.Count;
                int tableSize = pr.Length/numHeaders - 1;
                bool userFound = false;
                for(int i = 0; i < tableSize; i++) {
                    int rowKey = numHeaders*(i+1);
                    string[] row = pr.RangeSubset(rowKey, numHeaders);
                    TrialAttempt newAttempt = new TrialAttempt(row);
                    // Filter based on trialOmits
                    if (m_newLoadedTrial.trialOmits.Count > 0) {
                        bool isValid = true;
                        foreach(TrialOmit omit in m_newLoadedTrial.trialOmits) {
                            if (newAttempt.startTime >= omit.startTimestamp && newAttempt.startTime < omit.endTimestamp) {
                                isValid = false;
                                break;
                            }
                        }
                        if (isValid) {
                            allAttempts.Add(newAttempt);
                            if (newAttempt.id == "User") userFound = true;
                        }
                    } else {
                        allAttempts.Add(newAttempt);
                        if (newAttempt.id == "User") userFound = true;
                    }
                }
                // If "User" is among the ids in our attempts list, we just need to port this data into a new "assumedAttempts.csv" file
                // However, if "User" is NOT among the ids in our attempts list, we need to interpret them from existing position data
                if (!userFound) {
                    Debug.Log("[LOAD SIM] ERROR: Could not find 'assumedAttempts.csv' for " + m_newLoadedTrial.trialName + ". Deriving from positional data...");
                    Dictionary<string, TrialAttempt> currentAttempts = new Dictionary<string, TrialAttempt>();
                    StreetSimTrackable trackable, prevTrackable;
                    for(int i = 1; i < m_newLoadedTrial.positionData.rawPositionsList.Count; i++) {
                        trackable = m_newLoadedTrial.positionData.rawPositionsList[i];
                        prevTrackable = m_newLoadedTrial.positionData.rawPositionsList[i-1];
                        if (trackable.id != "User") {
                            yield return null;
                            continue;
                        }
                        if (m_newLoadedTrial.trialOmits.Count > 0) {
                            bool isValid = true;
                            foreach(TrialOmit omit in m_newLoadedTrial.trialOmits) {
                                if (prevTrackable.timestamp >= omit.startTimestamp && prevTrackable.timestamp < omit.endTimestamp) {
                                    isValid = false;
                                    break;
                                }
                            }
                            if (!isValid) {
                                yield return null;
                                continue;
                            }
                        }

                        if (Mathf.Abs(trackable.localPosition_z)<2.75f && !currentAttempts.ContainsKey(trackable.id)) {
                            // This means that the agent has moved onto the crosswalk, yet we don't have an attempt linked to it.
                            currentAttempts.Add(trackable.id, new TrialAttempt(trackable.id, m_newLoadedTrial.trialData.direction, prevTrackable.timestamp));
                            yield return null;
                            continue;
                        }
                        if(m_newLoadedTrial.trialData.direction == "NorthToSouth") {
                            // Start is when z > 2.25
                            // End is when z < -2.25f
                            if (trackable.localPosition_z >= 2.75f && currentAttempts.ContainsKey(trackable.id)) {
                                // In this case, the person returned to the start sidewalk. This is a failed attempt
                                TrialAttempt cAttempt = currentAttempts[trackable.id];
                                cAttempt.endTime = prevTrackable.timestamp;
                                cAttempt.successful = false;
                                cAttempt.reason = "[ASSUMED] Returned to start sidewalk";
                                allAttempts.Add(cAttempt);
                                currentAttempts.Remove(trackable.id);
                                yield return null;
                                continue;
                            }
                            if (trackable.localPosition_z <= -2.75f && currentAttempts.ContainsKey(trackable.id)) {
                                // In this case, the person got to the other end of the sidewalk. This is a successful attempt
                                TrialAttempt cAttempt = currentAttempts[trackable.id];
                                cAttempt.endTime = prevTrackable.timestamp;
                                cAttempt.successful = true;
                                cAttempt.reason = "[ASSUMED] Successfully reached the destination sidewalk";
                                allAttempts.Add(cAttempt);
                                currentAttempts.Remove(trackable.id);
                                yield return null;
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
                                allAttempts.Add(cAttempt);
                                currentAttempts.Remove(trackable.id);
                                yield return null;
                                continue;
                            }
                            if (trackable.localPosition_z >= 2.75f && currentAttempts.ContainsKey(trackable.id)) {
                                // In this case, the person got to the other end of the sidewalk. This is a successful attempt
                                TrialAttempt cAttempt = currentAttempts[trackable.id];
                                cAttempt.endTime = prevTrackable.timestamp;
                                cAttempt.successful = true;
                                cAttempt.reason = "[ASSUMED] Successfully reached the destination sidewalk";
                                allAttempts.Add(cAttempt);
                                currentAttempts.Remove(trackable.id);
                                yield return null;
                                continue;
                            }
                        }
                        yield return null;
                    }
                    // We need to clean up any attempts remaining. We automatically label them as successful
                    prevTrackable = m_newLoadedTrial.positionData.rawPositionsList[m_newLoadedTrial.positionData.rawPositionsList.Count-1];
                    foreach(KeyValuePair<string, TrialAttempt> kvp in currentAttempts) {
                        TrialAttempt cAttempt = kvp.Value;
                        cAttempt.endTime = prevTrackable.timestamp;
                        cAttempt.successful = true;
                        cAttempt.reason = "[ASSUMED] End of Trial";
                        allAttempts.Add(cAttempt);
                        yield return null;
                    }
                }

                // Now we save the file
                if (SaveSystemMethods.SaveCSV<TrialAttempt>(newAttemptsAssetPath,TrialAttempt.Headers,allAttempts)) {
                    Debug.Log("[LOAD SIM] " + m_newLoadedTrial.trialName + ": Saved Assumed Trial Attempts");
                } else {
                    Debug.LogError("[LOAD SIM] " + m_newLoadedTrial.trialName + ": Could not save Assumed Trial Attempts");
                }
                loadingAssumedAttempts = false;
            }
        }
    }


    private float Gaussian3D(Vector3 input, Vector3 mean, float sd) {
        return (1f/(Mathf.Pow(sd,3f)*Mathf.Pow(2*Mathf.PI,1.5f))) * Mathf.Exp(-1f*((Mathf.Pow(input.x-mean.x,2f)+Mathf.Pow(input.y-mean.y,2f)+Mathf.Pow(input.z-mean.z,2f))/(2*Mathf.Pow(sd,2f))));
    }
    public void GroundTruthSaliency() {
        Gradient g = new Gradient();
        GradientColorKey[] gck = new GradientColorKey[3];
        GradientAlphaKey[] gak = new GradientAlphaKey[3];
        gck[0].color = Color.red;
        gck[0].time = 1f;
        gck[1].color = Color.yellow;
        gck[1].time = 0.5f;
        gck[2].color = Color.blue;
        gck[2].time = 0f;
        gak[0].alpha = 1f;
        gak[0].time = 1f;
        gak[1].alpha = 1f;
        gak[1].time = 0.5f;
        gak[2].alpha = 0f;
        gak[2].time = 0f;
        g.SetKeys(gck, gak);

        Dictionary<Vector3, int> participantFixations = new Dictionary<Vector3, int>();
        foreach(Vector3 dir in directions) participantFixations.Add(dir, 0);

        foreach(KeyValuePair<string, List<LoadedSimulationDataPerTrial>> participant in participantData) {            
            // Now aggregate fixations for both averaged and discretized setups
            foreach(LoadedSimulationDataPerTrial trial in participant.Value) {
                foreach(KeyValuePair<Vector3, int> fixations in trial.averageFixations.fixations) {
                    participantFixations[fixations.Key] += fixations.Value;
                }
            }
        }

        // We find the means of x,y,z, as well as standard deviation of 1 degree
        List<Vector3> fixationsKeys = new List<Vector3>(participantFixations.Keys);
        float sd = (2f*Mathf.PI*Mathf.Pow(sphereRadius,2f))/360f;   // Global across all scenarios
        Vector3 mean = fixationsKeys.Aggregate(new Vector3(0f,0f,0f), (s,v) => s + v) / (float)fixationsKeys.Count;
        // Sort the fixations by count while also averaging
        List<KeyValuePair<Vector3,float>> sortedFixations = new List<KeyValuePair<Vector3,float>>();
        foreach(KeyValuePair<Vector3,int> af in participantFixations) {
            float avg = (float)af.Value / (float)(participantData.Count-1);
            float s = avg * Gaussian3D(af.Key,mean,sd);
            sortedFixations.Add(new KeyValuePair<Vector3,float>(af.Key, s));
        }
        sortedFixations.Sort((x, y) => y.Value.CompareTo(x.Value));
        // Normalize to between 1 and 0. The first item in `aggregateSortedFixations` has he greatest number of fixations
        float MAX_FIX_NUMBER = sortedFixations[0].Value;
        for (int i = 0; i < sortedFixations.Count; i++) {
            sortedFixations[i] = new KeyValuePair<Vector3, float>(sortedFixations[i].Key, sortedFixations[i].Value / MAX_FIX_NUMBER);
            directionColors[sortedFixations[i].Key] = g.Evaluate(sortedFixations[i].Value);
        }
        RecolorSphereGrid();
    }
    public void TruthSaliencyAtZ(float z) {
        Gradient g = new Gradient();
        GradientColorKey[] gck = new GradientColorKey[3];
        GradientAlphaKey[] gak = new GradientAlphaKey[3];
        gck[0].color = Color.red;
        gck[0].time = 1f;
        gck[1].color = Color.yellow;
        gck[1].time = 0.5f;
        gck[2].color = Color.blue;
        gck[2].time = 0f;
        gak[0].alpha = 1f;
        gak[0].time = 1f;
        gak[1].alpha = 1f;
        gak[1].time = 0.5f;
        gak[2].alpha = 0f;
        gak[2].time = 0f;
        g.SetKeys(gck, gak);

        Dictionary<Vector3, int> participantFixations = new Dictionary<Vector3, int>();
        foreach(Vector3 dir in directions) participantFixations.Add(dir, 0);

        foreach(KeyValuePair<string, List<LoadedSimulationDataPerTrial>> participant in participantData) {            
            // Now aggregate fixations for discretized setups
            foreach(LoadedSimulationDataPerTrial trial in participant.Value) {
                foreach(KeyValuePair<Vector3, int> fixations in trial.discretizedFixations[new Vector2(0f,z)].fixations) {
                    participantFixations[fixations.Key] += fixations.Value;
                }
            }
        }

        // We find the means of x,y,z, as well as standard deviation of 1 degree
        List<Vector3> fixationsKeys = new List<Vector3>(participantFixations.Keys);
        float sd = (2f*Mathf.PI*Mathf.Pow(sphereRadius,2f))/360f;   // Global across all scenarios
        Vector3 mean = fixationsKeys.Aggregate(new Vector3(0f,0f,0f), (s,v) => s + v) / (float)fixationsKeys.Count;
        // Sort the fixations by count while also averaging
        List<KeyValuePair<Vector3,float>> sortedFixations = new List<KeyValuePair<Vector3,float>>();
        foreach(KeyValuePair<Vector3,int> af in participantFixations) {
            float avg = (float)af.Value / (float)(participantData.Count-1);
            float s = avg * Gaussian3D(af.Key,mean,sd);
            sortedFixations.Add(new KeyValuePair<Vector3,float>(af.Key, s));
        }
        sortedFixations.Sort((x, y) => y.Value.CompareTo(x.Value));
        // Normalize to between 1 and 0. The first item in `aggregateSortedFixations` has he greatest number of fixations
        float MAX_FIX_NUMBER = sortedFixations[0].Value;
        for (int i = 0; i < sortedFixations.Count; i++) {
            sortedFixations[i] = new KeyValuePair<Vector3, float>(sortedFixations[i].Key, sortedFixations[i].Value / MAX_FIX_NUMBER);
            directionColors[sortedFixations[i].Key] = g.Evaluate(sortedFixations[i].Value);
        }
        RecolorSphereGrid();
    }

    public void GroundTruthROC(bool discretized = false) {

        // Ground Truth Saliency is effectively all gaze data, from all participants.
        // However, what we're looking for here is not ground truth saliency, but the ROC generated from ground truth saliency.
        // To achieve this, we need to do the following:
        //  1. For every i^th user, we aggregate fixation from all participants other than the i^th user, aggregating all scenes.
        //  2. We then collect the fixation from the i^th user
        //  3. We essentially count the % of fixations that the i^th user matched with the aggregated fixation
        //      So for example, let's say we have i^th user have a total of 7 fixations. 5 of those fixations match fixations from other participants. This means the succuss hit rate for the i^th user is 5/7
        //  4. We do this for all participants. We then average the success hit rates by the total number of participants.
        //  5. We do this for every % of saliency. For example, the top 5% of saliency, then the top 10% of saliency, and so on.

        // To do this, we already have access to all the average and discretized fixation data for each participant for each trial.
        //  For ground truth, we:
        //  1. loop through each participant `i`:
        //      a. Count all aggregates across all trials, not including `i`th participant. Order each direction by their total fixation number
        //      b. Count all aggregates across all trials, but only for `i`th participant. Order each direction by their total fixation number
        //      c. For each % of salience [5% - 100%, in 5% increments]:
        //          > Decide which directions to consider for aggregated total - for example, the top 5% salient directions are the the first 5% of the order of the aggregated directions
        //          > Decide which directions to cosnider for `i`th participant's total.
        //          > For every direction for the `i`th participant:
        //              - If that direction is not present inside the aggregated directions, then it's a miss
        //              - If that direction IS present inside the aggreagted directions, then it's a hit
        //          > Calculate ratio: #$hits / #hits+misses
        //          > Store this ratio inside a dictionary that maps saliencies to ratios

        Gradient g = new Gradient();
        GradientColorKey[] gck = new GradientColorKey[3];
        GradientAlphaKey[] gak = new GradientAlphaKey[3];
        gck[0].color = Color.red;
        gck[0].time = 1f;
        gck[1].color = Color.yellow;
        gck[1].time = 0.5f;
        gck[2].color = Color.blue;
        gck[2].time = 0f;
        gak[0].alpha = 1f;
        gak[0].time = 1f;
        gak[1].alpha = 1f;
        gak[1].time = 0.5f;
        gak[2].alpha = 0f;
        gak[2].time = 0f;
        g.SetKeys(gck, gak);
        
        // key: saliency level
        // value: List of ratios
        Dictionary<string, Dictionary<string, List<float>>> ratiosAcrossSaliencies = new Dictionary<string, Dictionary<string, List<float>>>();

        // Get list of active names
        List<string> trialNames = StreetSim.S.GetActiveTrialsByName();

        // Save trial names to `ratiosAcrossSaliencies`
        foreach(string trialName in trialNames) {
            ratiosAcrossSaliencies.Add(trialName, new Dictionary<string,List<float>>());
        }
        ratiosAcrossSaliencies.Add("Average", new Dictionary<string,List<float>>());

        // Global across all scenarios
        float sd = (2f*Mathf.PI*Mathf.Pow(sphereRadius,2f))/360f;
        Vector3 mean = directions.Aggregate(new Vector3(0f,0f,0f), (s,v) => s + v) / (float)directions.Count;

        Dictionary<string,float> GenerateSaliencyMaps(Dictionary<Vector3, int> trialFixations, Dictionary<Vector3, int> participantFixations) {
            //List<Vector3> aggregateFixationsKeys = new List<Vector3>(aggregateFixations.Keys);
            List<Vector3> aggregateFixationsKeys = directions;
            // float sd = (2f*Mathf.PI*Mathf.Pow(sphereRadius,2f))/360f;
            //Vector3 mean = aggregateFixationsKeys.Aggregate(new Vector3(0f,0f,0f), (s,v) => s + v) / (float)aggregateFixationsKeys.Count;
            
            // Sort the fixations by count while also averaging
            Debug.Log(trialFixations.Count);
            List<KeyValuePair<Vector3,float>> aggregateSaliencies = new List<KeyValuePair<Vector3,float>>();
            foreach(KeyValuePair<Vector3,int> af in trialFixations) {
                float avg = (float)af.Value / (float)(participantData.Count-1);
                float s = avg * Gaussian3D(af.Key,mean,sd);
                aggregateSaliencies.Add(new KeyValuePair<Vector3,float>(af.Key, s));
            }
            aggregateSaliencies.Sort((x, y) => y.Value.CompareTo(x.Value));
            
            // Normalize to between 1 and 0. The first item in `aggregateSortedFixations` hast he greatest number of fixations
            float MAX_FIX_NUMBER = aggregateSaliencies[0].Value;
            for (int i = 0; i < aggregateSaliencies.Count; i++) {
                aggregateSaliencies[i] = new KeyValuePair<Vector3, float>(aggregateSaliencies[i].Key, aggregateSaliencies[i].Value / MAX_FIX_NUMBER);
            }

            // Now need to iterate through each saliency
            int maxIndex, hits, total;
            float saliencyLevelFloat;
            string saliencyLevelString;
            KeyValuePair<Vector3,int> aggregateSaliency;
            Dictionary<string, float> saliencyToRatio = new Dictionary<string, float>();
            for(int saliencyLevel = 5; saliencyLevel <= 100; saliencyLevel += 5) {
                // Remember: starting from index 0, these are fixations sorted in descending order
                saliencyLevelFloat = (float)System.Math.Round((float)saliencyLevel/100f,2);
                saliencyLevelString = saliencyLevel.ToString();
                maxIndex = (int)Mathf.Round((float)directions.Count * saliencyLevelFloat);
                
                List<KeyValuePair<Vector3,float>> aggregateSubset = aggregateSaliencies.GetRange(0, maxIndex);
                Dictionary<Vector3,float> aggregateSubsetDictionary = aggregateSubset.ToDictionary(x=>x.Key,x=>x.Value);
                hits = 0;
                total = 0;
                foreach(KeyValuePair<Vector3,int> ithKVP in participantFixations) {
                    if (ithKVP.Value == 0) continue;
                    if (ithKVP.Value > 0 && aggregateSubsetDictionary.ContainsKey(ithKVP.Key) && aggregateSubsetDictionary[ithKVP.Key] > 0f) hits += 1;
                    total += 1;
                }
                float r = (hits == 0) ? 0f : (float)hits/(float)total;
                saliencyToRatio.Add(saliencyLevelString, r);
            }

            return saliencyToRatio;
        }

        // Iterate through each participant.
        // kvp.Key = participantName
        // kvp.Value = list of trials under participant.
        foreach(KeyValuePair<string, List<LoadedSimulationDataPerTrial>> kvp in participantData) {
            // This tracks ground truth fixations
            // Key = direction
            // Value = total number of fixations in that direction
            Debug.Log("[LOAD SIM] ROC Generation: Observing " + kvp.Key);
            Dictionary<string, Dictionary<Vector3, int>> ithParticipantFixations = new Dictionary<string, Dictionary<Vector3, int>>();
            Dictionary<string, Dictionary<Vector3, int>> aggregateFixations = new Dictionary<string, Dictionary<Vector3, int>>();
            foreach(string trialName in trialNames) {
                ithParticipantFixations.Add(trialName, new Dictionary<Vector3, int>());
                aggregateFixations.Add(trialName, new Dictionary<Vector3, int>());
                foreach(Vector3 dir in directions) {
                    ithParticipantFixations[trialName].Add(dir, 0);
                    aggregateFixations[trialName].Add(dir, 0);
                }
            }
            ithParticipantFixations.Add("Average",new Dictionary<Vector3, int>());
            aggregateFixations.Add("Average",new Dictionary<Vector3, int>());
            foreach(Vector3 dir in directions) {
                ithParticipantFixations["Average"].Add(dir, 0);
                aggregateFixations["Average"].Add(dir, 0);
            }
            
            // Now aggregate fixations for the ith partitipant
            Debug.Log("[LOAD SIM] Generating fixations for " + kvp.Key);
            foreach(LoadedSimulationDataPerTrial trial in kvp.Value) {
                Debug.Log("[LOAD SIM] Trial: \"" + trial.trialName + "\" - " + trial.averageFixations.fixations.Count.ToString());
                foreach(KeyValuePair<Vector3, int> fixations in trial.averageFixations.fixations) {
                    ithParticipantFixations[trial.trialName][fixations.Key] += fixations.Value;
                    ithParticipantFixations["Average"][fixations.Key] += fixations.Value;
                    Debug.Log("[LOAD SIM] Trial \""+trial.trialName+"\": " + fixations.Key + ": " + ithParticipantFixations[trial.trialName][fixations.Key].ToString() + " fixations");
                }
            }
            foreach(KeyValuePair<string, List<LoadedSimulationDataPerTrial>> kvpInner in participantData) {
                if (kvpInner.Key == kvp.Key) continue;
                foreach(LoadedSimulationDataPerTrial trial in kvpInner.Value) {
                    foreach(KeyValuePair<Vector3, int> fixations in trial.averageFixations.fixations) {
                        aggregateFixations[trial.trialName][fixations.Key] += fixations.Value;
                        aggregateFixations["Average"][fixations.Key] += fixations.Value;
                    }
                }
            }

            foreach(string trialName in trialNames) {
                Dictionary<string, float> smaps = GenerateSaliencyMaps(aggregateFixations[trialName], ithParticipantFixations[trialName]);
                foreach(KeyValuePair<string,float> smap in smaps) {
                    if (!ratiosAcrossSaliencies[trialName].ContainsKey(smap.Key)) ratiosAcrossSaliencies[trialName].Add(smap.Key, new List<float>());
                    ratiosAcrossSaliencies[trialName][smap.Key].Add(smap.Value);
                }
            }
            Dictionary<string, float> averagemaps = GenerateSaliencyMaps(aggregateFixations["Average"], ithParticipantFixations["Average"]);
            foreach(KeyValuePair<string,float> amap in averagemaps) {
                if (!ratiosAcrossSaliencies["Average"].ContainsKey(amap.Key)) ratiosAcrossSaliencies["Average"].Add(amap.Key, new List<float>());
                ratiosAcrossSaliencies["Average"][amap.Key].Add(amap.Value);
            }
            /*
            Dictionary<string, float> averagemaps = GenerateSaliencyMaps(aggregateFixations["Average"], ithParticipantFixations["Average"]);
            foreach(KeyValuePair<string,float> averageMap in averagemaps) {
                ratiosAcrossSaliencies["Average"].Add(averageMap.Key,averageMap.Value);
            }
            */

            /*
            // We find the means of x,y,z, as well as standard deviation of 1 degree
            List<Vector3> aggregateFixationsKeys = new List<Vector3>(aggregateFixations.Keys);
            float sd = (2f*Mathf.PI*Mathf.Pow(sphereRadius,2f))/360f;   // Global across all scenarios
            Vector3 mean = aggregateFixationsKeys.Aggregate(new Vector3(0f,0f,0f), (s,v) => s + v) / (float)aggregateFixationsKeys.Count;

            // Sort the fixations by count while also averaging
            List<KeyValuePair<Vector3,float>> aggregateSortedFixations = new List<KeyValuePair<Vector3,float>>();
            foreach(KeyValuePair<Vector3,int> af in aggregateFixations) {
                float avg = (float)af.Value / (float)(participantData.Count-1);
                float s = avg * Gaussian3D(af.Key,mean,sd);
                aggregateSortedFixations.Add(new KeyValuePair<Vector3,float>(af.Key, s));
            }
            aggregateSortedFixations.Sort((x, y) => y.Value.CompareTo(x.Value));
            // Normalize to between 1 and 0. The first item in `aggregateSortedFixations` hast he greatest number of fixations
            float MAX_FIX_NUMBER = aggregateSortedFixations[0].Value;
            for (int i = 0; i < aggregateSortedFixations.Count; i++) {
                aggregateSortedFixations[i] = new KeyValuePair<Vector3, float>(aggregateSortedFixations[i].Key, aggregateSortedFixations[i].Value / MAX_FIX_NUMBER);
                directionColors[aggregateSortedFixations[i].Key] = g.Evaluate(aggregateSortedFixations[i].Value);
            }
            RecolorSphereGrid();

            // Now need to iterate through each saliency
            int maxIndex, hits, total;
            KeyValuePair<Vector3,int> aggregateFixation;
            for(float saliencyLevel = 0.05f; saliencyLevel <= 1f; saliencyLevel += 0.05f) {
                // Remember: starting from index 0, these are fixations sorted in descending order
                saliencyLevel = (float)System.Math.Round(saliencyLevel,2);
                if (!ratiosAcrossSaliencies.ContainsKey(saliencyLevel)) ratiosAcrossSaliencies.Add(saliencyLevel, new List<float>());
                maxIndex = (int)Mathf.Round((float)directions.Count * saliencyLevel);
                
                List<KeyValuePair<Vector3,float>> aggregateSubset = aggregateSortedFixations.GetRange(0, maxIndex);
                Dictionary<Vector3,float> aggregateSubsetDictionary = aggregateSubset.ToDictionary(x=>x.Key,x=>x.Value);
                hits = 0;
                total = 0;
                foreach(KeyValuePair<Vector3,int> ithKVP in ithParticipantFixations) {
                    if (ithKVP.Value == 0) continue;
                    if (ithKVP.Value > 0 && aggregateSubsetDictionary.ContainsKey(ithKVP.Key) && aggregateSubsetDictionary[ithKVP.Key] > 0f) hits += 1;
                    total += 1;
                }
                ratiosAcrossSaliencies[saliencyLevel].Add((float)hits/(float)total);
            }
            */
        }

        // We now have, across each saliency, a list of ratios. We can condense this further so that each saliency level has an averaged ratio
        Debug.Log("[LOAD SIM] PRESENTING GROUND TRUTH SALIENCIES:");
        List<ROCRow> rocRows = new List<ROCRow>();
        Dictionary<string, Dictionary<string, float>> saliencyAverageRatio = new Dictionary<string, Dictionary<string, float>>();
        foreach(KeyValuePair<string, Dictionary<string,List<float>>> kvp in ratiosAcrossSaliencies) {
            Debug.Log("CALCULATING ROC FOR TRIAL \""+kvp.Key+"\"");
            ROCRow row = new ROCRow(kvp.Key);
            Dictionary<string, float> rowValues = new Dictionary<string, float>();
            foreach(KeyValuePair<string, List<float>> kvp2 in kvp.Value) {
                Debug.Log("SALIENCY LEVEL: " + kvp2.Key);
                float total = 0f;
                foreach(float ratio in kvp2.Value) total += ratio;
                Debug.Log("TOTAL: " + total);
                float average = total/(float)kvp2.Value.Count;
                //saliencyAverageRatio.Add(kvp2.Key, average);
                rowValues.Add(kvp2.Key, average);
                Debug.Log("\t"+kvp2.Key.ToString()+": " + average + " | " + (average*100f).ToString() + "%");
            }
            row.SetValues(rowValues);
            rocRows.Add(row);
        }

        // Save results in CSV
        string postProcessDir = SaveSystemMethods.GetSaveLoadDirectory("PostProcessData_Ignore");
        string objectFilename = postProcessDir + "rocs.csv";
         if (!SaveSystemMethods.SaveCSV<ROCRow>(objectFilename, ROCRow.Headers, rocRows)) {
            Debug.Log("[LOAD SIM] ERROR: Cannot save ROC agreement csv in \""+objectFilename+"\"");
        }

    }
    
    public void ROCAtZ(float z) {
        Gradient g = new Gradient();
        GradientColorKey[] gck = new GradientColorKey[3];
        GradientAlphaKey[] gak = new GradientAlphaKey[3];
        gck[0].color = Color.red;
        gck[0].time = 1f;
        gck[1].color = Color.yellow;
        gck[1].time = 0.5f;
        gck[2].color = Color.blue;
        gck[2].time = 0f;
        gak[0].alpha = 1f;
        gak[0].time = 1f;
        gak[1].alpha = 1f;
        gak[1].time = 0.5f;
        gak[2].alpha = 0f;
        gak[2].time = 0f;
        g.SetKeys(gck, gak);
        
        Dictionary<float, List<float>> ratiosAcrossSaliencies = new Dictionary<float, List<float>>();

        foreach(KeyValuePair<string, List<LoadedSimulationDataPerTrial>> kvp in participantData) {

            Debug.Log("[LOAD SIM] ROC Generation: Observing " + kvp.Key);
            // This tracks ground truth fixations
            Dictionary<Vector3, int> ithParticipantFixations = new Dictionary<Vector3, int>();
            // This tracks Discretized fixations
            // Add directions to each dictionary
            foreach(Vector3 dir in directions) {
                ithParticipantFixations.Add(dir, 0);
            }
            // Now aggregate fixations for both averaged and discretized setups
            foreach(LoadedSimulationDataPerTrial trial in kvp.Value) {
                foreach(KeyValuePair<Vector3, int> fixations in trial.discretizedFixations[new Vector2(0f,z)].fixations) {
                    ithParticipantFixations[fixations.Key] += fixations.Value;
                }
            }

            // Generate fixations from all observers outside of current observer
            Dictionary<Vector3, int> aggregateFixations = new Dictionary<Vector3, int>();

            foreach(Vector3 dir in directions) {
                aggregateFixations.Add(dir, 0);
            }
            foreach(KeyValuePair<string, List<LoadedSimulationDataPerTrial>> kvpInner in participantData) {
                if (kvpInner.Key == kvp.Key) continue;
                foreach(LoadedSimulationDataPerTrial trial in kvpInner.Value) {
                    foreach(KeyValuePair<Vector3, int> fixations in trial.discretizedFixations[new Vector2(0f,z)].fixations) {
                        aggregateFixations[fixations.Key] += fixations.Value;
                    }
                }
            }

            // We find the means of x,y,z, as well as standard deviation of 1 degree
            List<Vector3> aggregateFixationsKeys = new List<Vector3>(aggregateFixations.Keys);
            float sd = (2f*Mathf.PI*Mathf.Pow(sphereRadius,2f))/360f;   // Global across all scenarios
            Vector3 mean = aggregateFixationsKeys.Aggregate(new Vector3(0f,0f,0f), (s,v) => s + v) / (float)aggregateFixationsKeys.Count;

            // Sort the fixations by count while also averaging
            List<KeyValuePair<Vector3,float>> aggregateSortedFixations = new List<KeyValuePair<Vector3,float>>();
            foreach(KeyValuePair<Vector3,int> af in aggregateFixations) {
                float avg = (float)af.Value / (float)(participantData.Count-1);
                float s = avg * Gaussian3D(af.Key,mean,sd);
                aggregateSortedFixations.Add(new KeyValuePair<Vector3,float>(af.Key, s));
            }
            aggregateSortedFixations.Sort((x, y) => y.Value.CompareTo(x.Value));
            // Normalize to between 1 and 0. The first item in `aggregateSortedFixations` hast he greatest number of fixations
            float MAX_FIX_NUMBER = aggregateSortedFixations[0].Value;
            for (int i = 0; i < aggregateSortedFixations.Count; i++) {
                aggregateSortedFixations[i] = new KeyValuePair<Vector3, float>(aggregateSortedFixations[i].Key, aggregateSortedFixations[i].Value / MAX_FIX_NUMBER);
                directionColors[aggregateSortedFixations[i].Key] = g.Evaluate(aggregateSortedFixations[i].Value);
            }
            RecolorSphereGrid();

            // Now need to iterate through each saliency
            int maxIndex, hits, total;
            KeyValuePair<Vector3,int> aggregateFixation;
            for(float saliencyLevel = 0.05f; saliencyLevel <= 1f; saliencyLevel += 0.05f) {
                // Remember: starting from index 0, these are fixations sorted in descending order
                saliencyLevel = (float)System.Math.Round(saliencyLevel,2);
                if (!ratiosAcrossSaliencies.ContainsKey(saliencyLevel)) ratiosAcrossSaliencies.Add(saliencyLevel, new List<float>());
                maxIndex = (int)Mathf.Round((float)directions.Count * saliencyLevel);
                
                List<KeyValuePair<Vector3,float>> aggregateSubset = aggregateSortedFixations.GetRange(0, maxIndex);
                Dictionary<Vector3,float> aggregateSubsetDictionary = aggregateSubset.ToDictionary(x=>x.Key,x=>x.Value);
                hits = 0;
                total = 0;
                foreach(KeyValuePair<Vector3,int> ithKVP in ithParticipantFixations) {
                    if (ithKVP.Value == 0) continue;
                    if (ithKVP.Value > 0 && aggregateSubsetDictionary.ContainsKey(ithKVP.Key) && aggregateSubsetDictionary[ithKVP.Key] > 0f) hits += 1;
                    total += 1;
                }
                ratiosAcrossSaliencies[saliencyLevel].Add((float)hits/(float)total);
            }
        }

        // We now have, across each saliency, a list of ratios. We can condense this further so that each saliency level has an averaged ratio
        Debug.Log("[LOAD SIM] PRESENTING GROUND TRUTH SALIENCIES:");
        Dictionary<float, float> saliencyAverageRatio = new Dictionary<float, float>();
        foreach(KeyValuePair<float, List<float>> kvp in ratiosAcrossSaliencies) {
            float total = 0f;
            foreach(float ratio in kvp.Value) total += ratio;
            float average = total/(float)kvp.Value.Count;
            saliencyAverageRatio.Add(kvp.Key, average);
            Debug.Log("\t"+kvp.Key.ToString()+": " + average + " | " + (average*100f).ToString() + "%");
        }
    }

    public float GetDiscretizationFromIndex(int i) {
        return new List<float>(discretizations.Keys)[i];
    }
    public void ToggleDiscretization(float z) {
        discretizations[z] = !discretizations[z];
        StreetSimRaycaster.R.ToggleDiscretization(z);
    }

    public void PlaceCam(float z) {
        cam360.position = new Vector3(0f,heightRef.position.y,z);
    }

    public void TrackGazeOnObjectFromFile(ExperimentID target) {

        // We first check if there's an associated file with this target's name inside of PostProcessData_Ignore
        string postProcessDir = SaveSystemMethods.GetSaveLoadDirectory("PostProcessData_Ignore");
        string objectFilename = postProcessDir + target.id + ".csv";
        if (!SaveSystemMethods.CheckFileExists(objectFilename)) {
            Debug.Log("[LOAD SIM] ERROR: Cannot find .csv file associated with the target  \""+target.id+"\" inside \"PostProcessData_Ignore\"");
            return;
        }

        Dictionary<string, int> numAgentsNormalizationDict = new Dictionary<string,int>() {
            {"LowFem",14},
            {"LowFemale2",9},
            {"LowFemale3",3},
            {"LowFemale4",3},
            {"LowMale",14},
            {"LowMale2",9},
            {"LowMale3",3},
            {"LowMale4",3},
            {"HighFemale",14},
            {"HighFemale2",9},
            {"HighFemale3",3},
            {"HighFemale4",3},
            {"HighMale",14},
            {"HighMale2",9},
            {"HigMale3",3},
            {"HighMale4",3}
        };
        Debug.Log(numAgentsNormalizationDict[target.id]);

        TextAsset ta = (TextAsset)AssetDatabase.LoadAssetAtPath("Assets/PostProcessData_Ignore/"+target.id+".csv", typeof(TextAsset));
        string[] s = SaveSystemMethods.ReadCSV(ta);
        List<RaycastHitRow> rows = new List<RaycastHitRow>();
        int numHeaders = RaycastHitRow.Headers.Count;
        int tableSize = s.Length/numHeaders - 1;
        for(int i = 0; i < tableSize; i++) {
            int rowKey = numHeaders*(i+1);
            string[] row = s.RangeSubset(rowKey,numHeaders);
            rows.Add(new RaycastHitRow(row));
        }
        // End early if we don't have any rows...
        if (rows.Count == 0) {
            Debug.Log("[LOAD SIM] ERROR: Cannot find any gaze rows associated iwth this object...");
            return;
        }

        // With the data collected, we can safely start our process
        if (currentGazeObjectTracked != null) Destroy(currentGazeObjectTracked.gameObject);
        ClearGazePoints();

        // We instantiate a copy of the current target and place it at `GazeTrakcPlacementPositionRef` position
        currentGazeObjectTracked = Instantiate(target.transform, gazeTrackPlacementPositionRef.position, gazeTrackPlacementPositionRef.rotation, this.transform) as Transform;
        // Create a temp gameObject child, set scale to 1.5x the original scale
        GameObject temp = new GameObject("mesher");
        temp.transform.parent = currentGazeObjectTracked;
        temp.transform.localPosition = Vector3.zero;
        temp.transform.localScale = new Vector3(1f, 1f, 1f);
        MeshFilter filter = temp.AddComponent<MeshFilter>();
        MeshRenderer renderer = temp.AddComponent<MeshRenderer>();
        Material[] matsToSet = new Material[1];
        matsToSet[0] = GazeOnObjectMaterial;
        renderer.materials = matsToSet;
        Mesh mesh = Instantiate(currentGazeObjectTracked.GetComponent<StreetSimAgent>().GetRenderer().sharedMesh);
        mesh.SetTriangles(mesh.triangles, 0);
        mesh.subMeshCount = 1;
        currentGazeObjectTracked.GetComponent<StreetSimAgent>().GetRenderer().enabled = false;

        // We get all `ExperimentID`s associated with this object
        ExperimentID[] ids = currentGazeObjectTracked.gameObject.GetComponentsInChildren<ExperimentID>();
        string[] idNames = new string[ids.Length];
        for (int i = 0; i < ids.Length; i++) {
            idNames[i] = ids[i].id;
        }

        // Color scale
        Gradient g = new Gradient();
        GradientColorKey[] gck = new GradientColorKey[3];
        GradientAlphaKey[] gak = new GradientAlphaKey[3];
        gck[0].color = Color.red;
        gck[0].time = 1f;
        gck[1].color = Color.yellow;
        gck[1].time = 0.5f;
        gck[2].color = Color.blue;
        gck[2].time = 0f;
        gak[0].alpha = 1f;
        gak[0].time = 1f;
        gak[1].alpha = 1f;
        gak[1].time = 0.5f;
        gak[2].alpha = 0f;
        gak[2].time = 0f;
        g.SetKeys(gck, gak);

        // Color the mesh!
        Vector3[] allVertices = mesh.vertices;
        Color[] colors = new Color[allVertices.Length];
        int[] vertexCounts = new int[allVertices.Length];
        int[] indices = mesh.GetIndices(0); 
        // Now, iterate through each row in `rows`
        foreach(RaycastHitRow row in rows) {
            // need to get vertices from triangleIndex
            int firstIndexLocation = row.triangleIndex * 3;
            int firstIndex = indices[firstIndexLocation];
            int secondIndex = indices[firstIndexLocation + 1];
            int thirdIndex = indices[firstIndexLocation + 2]; 
            vertexCounts[firstIndex] += 1;
            vertexCounts[secondIndex] += 1;
            vertexCounts[thirdIndex] += 1;
        }
        // Now, iterate through all vertex counts, get the max
        int maxVertexCount = vertexCounts.Max();
        int minVertexCount = vertexCounts.Min();
        //Debug.Log("Max Count: {"+maxVertexCount.ToString()+"}");
        for(int i = 0; i < vertexCounts.Length; i++) {
            //Debug.Log("Vertex Count: {"+vertexCounts[i].ToString()+"}");
            float normalizedCount = (vertexCounts[i] > 0) 
                ? (float)vertexCounts[i] / (float)maxVertexCount
                : 0f;
            //Debug.Log("Normalized Count: {"+normalizedCount.ToString()+"}");
            colors[i] = g.Evaluate(normalizedCount);
        }

        // Now, color the mesh
        mesh.colors = colors;
        filter.sharedMesh = mesh;
    }

    public void TrackGazeOnObject(ExperimentID target) {
        // this target MAY or MAY NOT have any gaze targets on it.
        // What we need to do is to iterate through all trials.
        // Specifically, each `LoadedSimulationDataPerTrial` has a `LoadedGazeData` object called `gazeData`.
        //      This `LoadedGazeData` comes with an `objectsTracked` list that lets us know which objects were tracked in that trial

        // Delete our existing `currentGazeObjectTracked`
        if (currentGazeObjectTracked != null) Destroy(currentGazeObjectTracked.gameObject);
        ClearGazePoints();

        // We instantiate a copy of the current target and place it at `GazeTrakcPlacementPositionRef` position
        currentGazeObjectTracked = Instantiate(target.transform, gazeTrackPlacementPositionRef.position, gazeTrackPlacementPositionRef.rotation, this.transform) as Transform;
        // Create a temp gameObject child, set scale to 1.5x the original scale
        GameObject temp = new GameObject("mesher");
        temp.transform.parent = currentGazeObjectTracked;
        temp.transform.localPosition = Vector3.zero;
        temp.transform.localScale = new Vector3(1f, 1f, 1f);
        MeshFilter filter = temp.AddComponent<MeshFilter>();
        MeshRenderer renderer = temp.AddComponent<MeshRenderer>();
        Material[] matsToSet = new Material[1];
        matsToSet[0] = GazeOnObjectMaterial;
        renderer.materials = matsToSet;
        Mesh mesh = Instantiate(currentGazeObjectTracked.GetComponent<StreetSimAgent>().GetRenderer().sharedMesh);
        mesh.SetTriangles(mesh.triangles, 0);
        mesh.subMeshCount = 1;
        //Mesh[] submeshes = mesh.GetComponentsInChildren<Mesh>();
        currentGazeObjectTracked.GetComponent<StreetSimAgent>().GetRenderer().enabled = false;

        // We get all `ExperimentID`s associated with this object
        ExperimentID[] ids = currentGazeObjectTracked.gameObject.GetComponentsInChildren<ExperimentID>();
        string[] idNames = new string[ids.Length];
        for (int i = 0; i < ids.Length; i++) {
            idNames[i] = ids[i].id;
        }

        // We get all rows associated with this target
        List<RaycastHitRow> rows = new List<RaycastHitRow>();
        foreach(List<LoadedSimulationDataPerTrial> trials in participantData.Values) {
            foreach(LoadedSimulationDataPerTrial trial in trials) {
                if (trial.gazeData.objectsTracked.Contains(target)) {
                    List<RaycastHitRow> trialRows = trial.gazeData.gazeDataByTimestamp.Flatten2D<float,RaycastHitRow>();
                    foreach(RaycastHitRow row in trialRows) {
                        if (idNames.Contains(row.hitID)) {
                            rows.Add(row);
                        }
                    }
                }
            }
        }

        // Color scale
        Gradient g = new Gradient();
        GradientColorKey[] gck = new GradientColorKey[3];
        GradientAlphaKey[] gak = new GradientAlphaKey[3];
        gck[0].color = Color.red;
        gck[0].time = 1f;
        gck[1].color = Color.yellow;
        gck[1].time = 0.5f;
        gck[2].color = Color.blue;
        gck[2].time = 0f;
        gak[0].alpha = 1f;
        gak[0].time = 1f;
        gak[1].alpha = 1f;
        gak[1].time = 0.5f;
        gak[2].alpha = 0f;
        gak[2].time = 0f;
        g.SetKeys(gck, gak);

        // With our list of relevant rows, we now process them
        
        // The thing about raycasthitrow is that it stores the triangle index... but this triangle index doesn't take into account submeshes
        // Here's how we're going ot do this
        /*
        Dictionary<int, int[]> submeshCounts = new Dictionary<int, int[]>();
        Dictionary<int, Vector3[]> submeshVertices = new Dictionary<int, Vector3[]>();
        for(int i = 0; i < mesh.subMeshCount; i++) {
            submeshVertices.Add(i, submeshes[i].vertices);
            submeshCounts.Add(i, new int[submeshes[i].vertices.Length]);
        }

        foreach(RaycastHitRow row in rows) {
            int[] triangleIndexLocations = new int[] {
                row.triangleIndex * 3,
                row.triangleIndex * 3 + 1,
                row.triangleIndex * 3 + 2
            };
            for (int i = 0; i < mesh.subMeshCount; i++) {
                int[] subMeshTriangles = mesh.GetTriangles(i);
                for (int j = 0; j < subMeshTriangles.Length; j += 3) {
                    if (
                        subMeshTriangles[j] == triangleIndexLocations[0] &&
                        subMeshTriangles[j + 1] == triangleIndexLocations[1] &&
                        subMeshTriangles[j + 2] == triangleIndexLocations[2]
                    ) { 
                        // triangle index = row.triangleIndex
                        // submesh index = i
                        // submesh triangle index = j/3
                        submeshCounts[i][j] += 1;
                        submeshCounts[i][j+1] += 1;
                        submeshCounts[i][j+2] += 1;
                    }
                }
            }
        }

        int maxCount = 0;
        foreach(int[] counts in submeshCounts.Values) {
            int curMax = counts.Max();
            if (curMax > maxCount) maxCount = curMax;
        }

        Dictionary<int, Color[]> submeshColors = new Dictionary<int, Color[]>();
        for(int i = 0; i < subMeshes.Length; i++) {
            submeshColors.Add(i, new Color[submeshVertices[i].Length]);
            for(int j = 0; j < submeshCounts[i].Length; j++) {
                float normalizedVal = (float)submeshCounts[i][j] / (float)maxCount;
                Color c = g.Evaluate(normalizedVal);
                submeshColors[i][j] = c;
            }
            submeshes[i].colors = submeshColors[i];

        }
        */

        Vector3[] allVertices = mesh.vertices;
        Color[] colors = new Color[allVertices.Length];
        int[] vertexCounts = new int[allVertices.Length];
        int[] indices = mesh.GetIndices(0); 
        // Now, iterate through each row in `rows`
        foreach(RaycastHitRow row in rows) {
            // need to get vertices from triangleIndex
            int firstIndexLocation = row.triangleIndex * 3;
            int firstIndex = indices[firstIndexLocation];
            int secondIndex = indices[firstIndexLocation + 1];
            int thirdIndex = indices[firstIndexLocation + 2]; 
            vertexCounts[firstIndex] += 1;
            vertexCounts[secondIndex] += 1;
            vertexCounts[thirdIndex] += 1;
        }
        // Now, iterate through all vertex counts, get the max
        int maxVertexCount = vertexCounts.Max();
        //Debug.Log("Max Count: {"+maxVertexCount.ToString()+"}");
        for(int i = 0; i < vertexCounts.Length; i++) {
            //Debug.Log("Vertex Count: {"+vertexCounts[i].ToString()+"}");
            float normalizedCount = (vertexCounts[i] > 0) 
                ? (float)vertexCounts[i] / (float)maxVertexCount
                : 0f;
            //Debug.Log("Normalized Count: {"+normalizedCount.ToString()+"}");
            colors[i] = g.Evaluate(normalizedCount);
        }

        // Now, color the mesh
        mesh.colors = colors;
        filter.sharedMesh = mesh;


        /*
        // We generate a list of `GazeOnObjectTrackable` objects
        List<GazeOnObjectTrackable> trackables = new List<GazeOnObjectTrackable>();

        string postProcessDir = SaveSystemMethods.GetSaveLoadDirectory("PostProcessData_Ignore");
        SaveSystemMethods.CheckOrCreateDirectory(postProcessDir);
        string objectFilename = postProcessDir + target.id + ".csv";
        Debug.Log("[LOAD SIM] Attempting to load object-gaze map file \""+objectFilename+"\" from memory...");

        if (SaveSystemMethods.CheckFileExists(objectFilename)) {
            // We know it exists. We now need to convert it into assetPath form
            string ap = "Assets/PostProcessData_Ignore/"+target.id+".csv";
            TextAsset ta = (TextAsset)AssetDatabase.LoadAssetAtPath(ap, typeof(TextAsset));
            string[] data = SaveSystemMethods.ReadCSV(ta);
            trackables = new List<GazeOnObjectTrackable>();
            int numHeaders = GazeOnObjectTrackable.Headers.Count;
            int tableSize = data.Length/numHeaders - 1;
      
            for(int i = 0; i < tableSize; i++) {
                int rowKey = numHeaders*(i+1);
                string[] row = data.RangeSubset(rowKey,numHeaders);
                trackables.Add(new GazeOnObjectTrackable(row));
            }
            
            foreach(GazeOnObjectTrackable t in trackables) {
                foreach(ExperimentID id in ids) {
                    if (t.hitID == id.id) {
                        GazePoint newPoint = Instantiate(gazePointPrefab, id.transform) as GazePoint;
                        newPoint.parent = id;
                newPoint.SetScale(0.025f);
                newPoint.SetColor(Color.yellow);
                        newPoint.transform.localPosition = new Vector3(t.localPosition_x, t.localPosition_y, t.localPosition_z);
                activeGazePoints.Add(newPoint);
                    }
                }
            }

            Debug.Log("[LOAD SIM] Found " + trackables.Count.ToString() + " gaze points associated with this object");

            return;
        } 
        
        List<RaycastHitRow> rows = new List<RaycastHitRow>();
        foreach(List<LoadedSimulationDataPerTrial> trials in participantData.Values) {
            foreach(LoadedSimulationDataPerTrial trial in trials) {
                if (trial.gazeData.objectsTracked.Contains(target)) {
                    List<RaycastHitRow> trialRows = trial.gazeData.gazeDataByTimestamp.Flatten2D<float,RaycastHitRow>();
                    rows.AddRange(trialRows);
                }
            }
        }
        // If there's more than 0 rows associated with this object, then we need to set up the display
        if (rows.Count == 0) {
            Debug.Log("[LOAD SIM] No gaze points found on this object \""+target.id+"\". Unable to bring up render view");
            Destroy(currentGazeObjectTracked.gameObject);
            currentGazeObjectTracked = null;
            return;
        }

        // For each `RaycastHitRow` in `rows`, attach a GazePoint onto it.
        foreach(RaycastHitRow row in rows) {
            foreach(ExperimentID id in ids) {
                if (row.hitID == id.id) {
                    // If the hitID matches the id, then we attach a new GazePoint to this object
                    Vector3 localPosition = new Vector3(row.localPositionOfHitPosition[0], row.localPositionOfHitPosition[1], row.localPositionOfHitPosition[2]);
                    GazePoint newPoint = Instantiate(gazePointPrefab, id.transform) as GazePoint;
                    newPoint.parent = id;
                    newPoint.SetScale(0.025f);
                    newPoint.SetColor(Color.yellow);
                    newPoint.transform.localPosition = localPosition;
                    activeGazePoints.Add(newPoint);
                    //trackables.Add(new GazeOnObjectTrackable(target.id,currentGazeObjectTracked.InverseTransformPoint(newPoint.transform.position)));
                    trackables.Add(new GazeOnObjectTrackable(target.id, id.id, newPoint.transform.localPosition, currentGazeObjectTracked.InverseTransformPoint(newPoint.transform.position)));
                }
            }
        }

        // We need to save the data into a new file
        if (!SaveSystemMethods.SaveCSV<GazeOnObjectTrackable>(objectFilename, GazeOnObjectTrackable.Headers, trackables)) {
            Debug.Log("[LOAD SIM] ERROR: Cannot save object gaze map tracking file \""+objectFilename+"\"");
        }
        Debug.Log("[LOAD SIM] Found " + trackables.Count.ToString() + " gaze points associated with this object");
        */
    }
    public void TrackGazeGroupsOnObject(ExperimentID target) {
        // this target MAY or MAY NOT have any gaze targets on it.
        // What we need to do is to iterate through all trials.
        // Specifically, each `LoadedSimulationDataPerTrial` has a `LoadedGazeData` object called `gazeData`.
        //      This `LoadedGazeData` comes with an `objectsTracked` list that lets us know which objects were tracked in that trial

        // Delete our existing `currentGazeObjectTracked`
        if (currentGazeObjectTracked != null) Destroy(currentGazeObjectTracked.gameObject);
        ClearGazePoints();

        // We instantiate a copy of the current target and place it at `GazeTrakcPlacementPositionRef` position
        currentGazeObjectTracked = Instantiate(target.transform, gazeTrackPlacementPositionRef.position, gazeTrackPlacementPositionRef.rotation, this.transform) as Transform;
        // We get all `ExperimentID`s associated with this object
        ExperimentID[] ids = currentGazeObjectTracked.gameObject.GetComponentsInChildren<ExperimentID>(); 
        // We generate a list of `GazeOnObjectTrackable` objects
        List<GazeOnObjectTrackable> trackables = new List<GazeOnObjectTrackable>();

        string postProcessDir = SaveSystemMethods.GetSaveLoadDirectory("PostProcessData_Ignore");
        SaveSystemMethods.CheckOrCreateDirectory(postProcessDir);
        string objectFilename = postProcessDir + target.id + ".csv";
        Debug.Log("[LOAD SIM] Attempting to load object-gaze map file \""+objectFilename+"\" from memory...");

        if (!SaveSystemMethods.CheckFileExists(objectFilename)) {
            Debug.Log("[LOAD SIM] ERROR: Cannot get gaze groups on target without data file");
            return;
        }
        
        // We know it exists. We now need to convert it into assetPath form
        string ap = "Assets/PostProcessData_Ignore/"+target.id+".csv";
        TextAsset ta = (TextAsset)AssetDatabase.LoadAssetAtPath(ap, typeof(TextAsset));
        string[] data = SaveSystemMethods.ReadCSV(ta);
        trackables = new List<GazeOnObjectTrackable>();
        int numHeaders = GazeOnObjectTrackable.Headers.Count;
        int tableSize = data.Length/numHeaders - 1;
      
        Dictionary<int, List<Vector3>> groups = new Dictionary<int,List<Vector3>>();
        for(int i = 0; i < tableSize; i++) {
            int rowKey = numHeaders*(i+1);
            string[] row = data.RangeSubset(rowKey,numHeaders);
            GazeOnObjectTrackable newTrackable = new GazeOnObjectTrackable(row);
            if (!groups.ContainsKey(newTrackable.dbscanID)) groups.Add(newTrackable.dbscanID,new List<Vector3>());
            groups[newTrackable.dbscanID].Add(new Vector3(newTrackable.position_x, newTrackable.position_y, newTrackable.position_z));
            trackables.Add(newTrackable);
        }
        
        foreach(KeyValuePair<int,List<Vector3>> kvp in groups) {
            Vector3 avg = Vector3.zero;
            foreach(Vector3 v in kvp.Value) {
                avg += v;
            }
            Vector3 pos = avg/kvp.Value.Count;
            Color groupColor = new Color(
                UnityEngine.Random.Range(0f, 1f), 
                UnityEngine.Random.Range(0f, 1f), 
                UnityEngine.Random.Range(0f, 1f),
                0.5f
            );
            float size = (float)kvp.Value.Count;
            GazePoint newPoint = Instantiate(gazePointPrefab) as GazePoint;
            newPoint.SetScale(0.02518f * size * 0.1f);
            newPoint.SetColor(groupColor);
            newPoint.transform.parent = currentGazeObjectTracked;
            newPoint.transform.localPosition = pos;
            activeGazePoints.Add(newPoint);
        }

        /*
        // Color scale
        Gradient g = new Gradient();
        GradientColorKey[] gck = new GradientColorKey[3];
        GradientAlphaKey[] gak = new GradientAlphaKey[3];
        gck[0].color = Color.red;
        gck[0].time = 1f;
        gck[1].color = Color.yellow;
        gck[1].time = 0.5f;
        gck[2].color = Color.blue;
        gck[2].time = 0f;
        gak[0].alpha = 1f;
        gak[0].time = 1f;
        gak[1].alpha = 1f;
        gak[1].time = 0.5f;
        gak[2].alpha = 0f;
        gak[2].time = 0f;
        g.SetKeys(gck, gak);

        foreach(GazeOnObjectTrackable t in trackables) {
            GazePoint newPoint = Instantiate(gazePointPrefab) as GazePoint;
            newPoint.SetScale(0.025f);
            float groupColorVal = (float)groupCount[t.dbscanID] / biggestGroupCount;
            newPoint.SetColor(g.Evaluate(groupColorVal));
            newPoint.transform.parent = currentGazeObjectTracked;
            newPoint.transform.localPosition = new Vector3(t.position_x, t.position_y, t.position_z);
            activeGazePoints.Add(newPoint);
        }
        */
    }
    #endif
}

[System.Serializable]
public class LoadedSimulationDataPerTrial {
    public string trialName;
    public string simVersion;
    public string assetPath;
    public TrialData trialData;
    public List<TrialOmit> trialOmits;
    public Dictionary<int, float> indexTimeMap;
    [SerializeField] private LoadedPositionData m_positionData;
    [SerializeField] private LoadedGazeData m_gazeData;
    public LoadedFixationData averageFixations;
    public Dictionary<Vector2, LoadedFixationData> discretizedFixations;
    public List<RaycastHitDurationRow> gazeDurationsByIndex;
    public List<RaycastHitDurationRow> gazeDurationsByHit;
    public List<RaycastHitDurationRow> gazeDurationsByAgent;
    public LoadedPositionData positionData { get=>m_positionData; set {
        m_positionData = value;
        CompareIndexTimeMap(value.indexTimeMap);
    }}
    public LoadedGazeData gazeData { get=>m_gazeData; set {
        m_gazeData = value;
        CompareIndexTimeMap(value.indexTimeMap);
    }}
    public LoadedSimulationDataPerTrial(string trialName, string simVersion, string assetPath, List<TrialOmit> trialOmits) {
        this.trialName = trialName;
        this.simVersion = simVersion;
        this.assetPath = assetPath;
        this.trialOmits = trialOmits;
        indexTimeMap = new Dictionary<int, float>();
        m_positionData = null;
        this.discretizedFixations = new Dictionary<Vector2, LoadedFixationData>();
    }
    public void CompareIndexTimeMap(Dictionary<int, float> newMap) {
        if (this.indexTimeMap.Count == 0) {
            this.indexTimeMap = newMap;
            return;
        }
        foreach(KeyValuePair<int, float> mapItem in newMap) {
            if (!this.indexTimeMap.ContainsKey(mapItem.Key)) this.indexTimeMap.Add(mapItem.Key, mapItem.Value);
        }
        Debug.Log("[STREET SIM] Comparing mapkey for \""+this.trialName+"\" shows we have "+this.indexTimeMap.Count+" timeframes to consider");
        return;
    }
}

[SerializeField]
public class LoadedFixationData {
    public List<SGazePoint> gazePoints;
    public Dictionary<Vector3, int> fixations;
    public LoadedFixationData(List<SGazePoint> gazePoints) {
        this.gazePoints = gazePoints;
        this.fixations = new Dictionary<Vector3, int>();
    }
    public LoadedFixationData(List<SGazePoint> gazePoints, Dictionary<Vector3, int> fixations) {
        this.gazePoints = gazePoints;
        this.fixations = fixations;
    }
}
[System.Serializable]
public class SDirection {
    public int i;
    public float x, y, z;
    public SDirection(int i, Vector3 dir) {
        this.i = i;
        this.x = dir.x;
        this.y = dir.y;
        this.z = dir.z;
    }
    public static List<string> Headers => new List<string> {
        "i","x","y","z"
    };
}
[System.Serializable]
public class SDirectionFixation {
    public int i;
    public float x, y, z;
    public int fixes;
    public SDirectionFixation(int i, Vector3 dir, int fixes) {
        this.i = i;
        this.x = dir.x;
        this.y = dir.y;
        this.z = dir.z;
        this.fixes = fixes;
    }
    public static List<string> Headers => new List<string> {
        "i","x","y","z","fixes"
    };
}
[System.Serializable]
public class DirectionFixationMap {
    public Vector3 direction;
    public int fixationCount;
    public DirectionFixationMap(Vector3 direction, int fixationCount) {
        this.direction = direction;
        this.fixationCount = fixationCount;
    }
}

[SerializeField]
public class GazeOnObjectTrackable {
    public string agentID;
    public float position_x;
    public float position_y;
    public float position_z;
    public int dbscanID;
    public GazeOnObjectTrackable(string agentID, Vector3 position) {
        this.agentID = agentID;
        this.position_x = position.x;
        this.position_y = position.y;
        this.position_z = position.z;
        this.dbscanID = -1;
    }
    public GazeOnObjectTrackable(string[] data) {
        this.agentID = data[0];
        this.position_x = float.Parse(data[1]);
        this.position_y = float.Parse(data[2]);
        this.position_z = float.Parse(data[3]);
        this.dbscanID = int.Parse(data[4]);
    }
    public static List<string> Headers => new List<string> {
        "agentID",
        "position_x",
        "position_y",
        "position_z",
        "dbscanID"
    };
}

[System.Serializable]
public class ROCRow {
    public string trialName;
    public float agree_5, agree_10, agree_15, agree_20, agree_25, agree_30, agree_35, agree_40, agree_45, agree_50, agree_55, agree_60, agree_65, agree_70, agree_75, agree_80, agree_85, agree_90, agree_95, agree_100;
    public ROCRow(string trialName) {
        this.trialName = trialName;
    }
    public ROCRow(string trialName, Dictionary<string,float> values) {
        this.trialName = trialName;
        this.agree_5 = values["5"];
        this.agree_10 = values["10"];
        this.agree_15 = values["15"];
        this.agree_20 = values["20"];
        this.agree_25 = values["25"];
        this.agree_30 = values["30"];
        this.agree_35 = values["35"];
        this.agree_40 = values["40"];
        this.agree_45 = values["45"];
        this.agree_50 = values["50"];
        this.agree_55 = values["55"];
        this.agree_60 = values["60"];
        this.agree_65 = values["65"];
        this.agree_70 = values["70"];
        this.agree_75 = values["75"];
        this.agree_80 = values["80"];
        this.agree_85 = values["85"];
        this.agree_90 = values["90"];
        this.agree_95 = values["95"];
        this.agree_100 = values["100"];
    }
    public void SetValues(Dictionary<string, float> values) {
        this.agree_5 = values["5"];
        this.agree_10 = values["10"];
        this.agree_15 = values["15"];
        this.agree_20 = values["20"];
        this.agree_25 = values["25"];
        this.agree_30 = values["30"];
        this.agree_35 = values["35"];
        this.agree_40 = values["40"];
        this.agree_45 = values["45"];
        this.agree_50 = values["50"];
        this.agree_55 = values["55"];
        this.agree_60 = values["60"];
        this.agree_65 = values["65"];
        this.agree_70 = values["70"];
        this.agree_75 = values["75"];
        this.agree_80 = values["80"];
        this.agree_85 = values["85"];
        this.agree_90 = values["90"];
        this.agree_95 = values["95"];
        this.agree_100 = values["100"];
    }
    public static List<string> Headers = new List<string> {
        "trialName",
        "agree_5", 
        "agree_10", 
        "agree_15", 
        "agree_20", 
        "agree_25", 
        "agree_30", 
        "agree_35", 
        "agree_40", 
        "agree_45", 
        "agree_50", 
        "agree_55", 
        "agree_60", 
        "agree_65", 
        "agree_70", 
        "agree_75", 
        "agree_80", 
        "agree_85", 
        "agree_90", 
        "agree_95", 
        "agree_100"
    };
}

[System.Serializable]
public class TrialOmit {
    public string participantName, trialName;
    public int startTimeIndex;
    public float startTimestamp, endTimestamp;
    public TrialOmit(string[] data) {
        this.participantName = data[0];
        this.trialName = data[1];
        this.startTimeIndex = int.Parse(data[2]);
        this.startTimestamp = float.Parse(data[3]);
        this.endTimestamp = float.Parse(data[4]);
    }
    public static List<string> Headers = new List<string> {
        "participantName", 
        "trialName",
        "startTimeIndex",
        "startTimestamp", 
        "endTimestamp"
    };
}