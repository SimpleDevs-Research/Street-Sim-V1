using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistrictController : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject areaPrefab;
    [SerializeField] private GameObject agentPrefab;
    
    [Header("Area Determination")]
    [SerializeField] private AreaController[,] areas;
    [SerializeField] private Vector2Int dimensions = new Vector2Int(10,10);
    [SerializeField, Tooltip("By default. all areas are 0.5m by 0.5m.")] 
    private float m_resolution = 0.5f;
    public float resolution {
        get { return m_resolution; }
        set {}
    }
    [SerializeField] private List<AreaController> areasWithAgents = new List<AreaController>();
    [SerializeField] private bool isCheckingAreasWithAgents = false;

    [Header("Agent Determination")]
    [SerializeField] private List<GridAgent> agents = new List<GridAgent>();

    [Header("Logistics")]
    [SerializeField, Tooltip("We designate how many frames a second we check our areas")]
    private int fps = 15;

    private void Awake() {
        // Upon initialization, the program will look at the intended dimensions and determine how many areas are needed
        areas = new AreaController[dimensions.x,dimensions.y];
        // Initialize an `area` variable that'll be used constantly, to save memory.
        AreaController area;
        GameObject areaObject;
        // Iterating through multidimensional array;
        for(int x = 0; x < dimensions.x; x++) {
            for(int y = 0; y < dimensions.y; y++) {
                // Initializing Area
                areaObject = Instantiate(areaPrefab,transform.position,Quaternion.identity,this.transform);
                area = areaObject.GetComponent<AreaController>();
                area.Initialize(this,x,y);
                areas[x,y] = area;
                // Setting neighbors
                if (x>0) {
                    area.SetNeighbor(areas[x-1,y]);
                    areas[x-1,y].SetNeighbor(area);
                }
                if (y>0)  {
                    area.SetNeighbor(areas[x,y-1]);
                    areas[x,y-1].SetNeighbor(area);
                }
                if (x>0 && y>0) {
                    area.SetNeighbor(areas[x-1,y-1]);
                    areas[x-1,y-1].SetNeighbor(area);
                }
            }
        }
        // Initialize agents
        // For now, we just add the agents manually into the scene through the Unity inspector
        for(int i = 0; i < agents.Count; i++) {
            agents[i].Initialize(this,i);
        }
        
        // Instead of using Update, which runs each frame, we run a Coroutine to control the speed at which our DistrictController tracks objects
        // In the real world, we wouldn't do it like this. However, it's an experimental setup...
        // The frequency at which we run the coroutine is determined by 1/<fps>
        /* StartCoroutine(CustomUpdate()); */

        // We run a coroutine that runs on occasion to reset areas that used to have obstacles but no longer do.
        StartCoroutine(CheckAreas());
    }

    /*
    private IEnumerator CustomUpdate() {
        float waitTime = 1f / (float)fps;
        while(true) {
            forint x = 0; x < dimensions.x; x++) {
                for(int y = 0; y < dimensions.y; y++) {
                    // In this case, we do a raycast downward to check for obstacles.
                    areas[x,y].SearchForObstacle();
                    yield return null;
                }
            }
            yield return new WaitForSeconds(waitTime);
        }
    }
    */

    private IEnumerator CheckAreas() {
        float waitTime = 1f / (float)fps;
        while(true) {
            isCheckingAreasWithAgents = true;
            List<AreaController> stillHas = new List<AreaController>();
            if (areasWithAgents.Count > 0) {
                foreach(AreaController area in areasWithAgents) {
                    // Each area knows which agents are in them. We'll have each area check with each agent where their distances between themselves and the agents are.
                    // Determination if they're containing an agent or not is based on each agent's radius
                    if (area.CheckAgentDistances()) {
                        // This one both checks in the area if agents are in that area and returns if true or false
                        // In this case, if there are, we put them in `stillHas`
                        if (!stillHas.Contains(area)) stillHas.Add(area);
                    }
                    yield return null;
                }
                areasWithAgents = stillHas;
            }
            isCheckingAreasWithAgents = false;
            yield return new WaitForSeconds(waitTime);
        }
    }

    private IEnumerator AddAreaWithAgents(AreaController area) {
        while(isCheckingAreasWithAgents) {
            yield return null;
        }
        if (!areasWithAgents.Contains(area)) areasWithAgents.Add(area);
    }

    public void UpdateAgentPosition(GridAgent agent, Vector3 position, float radius) {
        // At this point, all we know is: 1) the position of the current agent, and 2) the agent's radius.
        // We can pinpoint the specific areas the agent is in.
        // The maximum X indices we need to consider is minX < x < maxX. Same with Y
        int minX = PositionToIndex(position.x - radius);
        int maxX = PositionToIndex(position.x + radius);
        int minY = PositionToIndex(position.z - radius);
        int maxY = PositionToIndex(position.z + radius);
        
        // Iterate through possible areas, confirming their hits and telling our Agent that we hit those areas
        for(int x = minX; x <= maxX; x++) {
            if (x < 0 || x >= areas.GetLength(0)) continue;
            for (int y = minY; y <= maxY; y++) {
                if (y < 0 || y >= areas.GetLength(1)) continue;
                // We notify the area if they fit within the radius of the obstacle
                areas[x,y].AddAgentInside(agent);
                // We add that area to `areasWithAgents`
                if (!areasWithAgents.Contains(areas[x,y])) StartCoroutine(AddAreaWithAgents(areas[x,y]));
            }
        }
    }

    public int PositionToIndex(float pos) {
        return (int)Mathf.Floor(pos/m_resolution);
    }
    public AreaController GetAgentFromIndex(int x, int y) {
        if (
            x < 0 
            || x >= areas.GetLength(0)
            || y < 0
            || y >= areas.GetLength(1)
        ) return null;
        return areas[x,y];
    }
    public AreaController GetAgentFromIndex(Vector2Int indices) {
        return GetAgentFromIndex(indices.x, indices.y);
    }      

}
