using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using TMPro;

public class AreaController : MonoBehaviour {

    [Header("References")]
    [SerializeField] private LineRenderer lr;
    //      [SerializeField] private Canvas canvas;
    //      [SerializeField] TextMeshProUGUI textbox;
    [SerializeField] private Renderer r;

    private DistrictController parent;
    public Vector2Int index = Vector2Int.zero;
    [SerializeField] 
    private float m_hitting = 0f;
    public float hitting {
        get { return m_hitting; }
        set {
            m_hitting = value;
            UpdateHitAppearance();
        }
    }
    Color hitColor = Color.white;

    public List<AreaController> neighbors = new List<AreaController>();
    public List<GridAgent> agentsInside = new List<GridAgent>();

    private void Awake() {
        if (lr == null) lr = GetComponent<LineRenderer>();
        if (r == null) r = GetComponent<Renderer>();
    }

    public void Initialize(DistrictController parent, int x, int y) {
        // Set references
        this.parent = parent;
        index = new Vector2Int(x,y);
        // Set Pos
        this.transform.position = 
            this.parent.transform.position + 
            new Vector3(
                (index.x*this.parent.resolution) + (this.parent.resolution*0.5f), 
                0f, 
                (index.y*this.parent.resolution) + (this.parent.resolution*0.5f)
            );
        // Scale the textbox
        //      canvas.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2 (this.parent.resolution, this.parent.resolution);
        //      textbox.rectTransform.sizeDelta = new Vector2(this.parent.resolution, this.parent.resolution);
        // Set the textbox's text
        //      textbox.text = hitting.ToString();
        
        // For good measure, update appearance before we start
        UpdateHitAppearance();
    }

    public void SetNeighbor(AreaController neighbor) {
        if (!neighbors.Contains(neighbor)) neighbors.Add(neighbor);
    }

    public void AddAgentInside(GridAgent agent) {
        if (!agentsInside.Contains(agent)) agentsInside.Add(agent);
    }

    private void Update() {
        // We end early if there aren't any agents to consider
        if (agentsInside.Count == 0) {
            hitting = 0f;
            return;
        }
        // We need to check with each agent if they're still in range.
        // The Agent has a track record of which indices it's closest to.
        // We just need to check if our index is among those indices
        List<GridAgent> tempIn = new List<GridAgent>();
        foreach(GridAgent agent in agentsInside) {
            if (agent.CheckIfCloseToArea(index)) tempIn.Add(agent);
        }
        bool hasAgents = tempIn.Count > 0;
        agentsInside = tempIn;
        hitting = hasAgents ? 1f : 0f;
        return;
    }

    private void UpdateHitAppearance() {
        // textbox.text = m_hitting.ToString();
        hitColor = Color.Lerp(Color.white, Color.red, hitting);
        r.material.SetColor("_Color", hitColor);
    }
}
