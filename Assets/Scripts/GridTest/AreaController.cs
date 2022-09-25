using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AreaController : MonoBehaviour {

    [Header("References")]
    [SerializeField] private LineRenderer lr;
    [SerializeField] private Canvas canvas;
    [SerializeField] TextMeshProUGUI textbox;

    private DistrictController parent;
    public Vector2Int index = Vector2Int.zero;
    [SerializeField] 
    private float m_hitting = 0f;
    public float hitting {
        get { return m_hitting; }
        set {
            m_hitting = value;
            textbox.text = m_hitting.ToString();
        }
    }

    public List<AreaController> neighbors = new List<AreaController>();
    public List<GridAgent> agentsInside = new List<GridAgent>();

    private void Awake() {
        if (lr == null) lr = GetComponent<LineRenderer>();
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
        canvas.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2 (this.parent.resolution, this.parent.resolution);
        textbox.rectTransform.sizeDelta = new Vector2(this.parent.resolution, this.parent.resolution);
        // Set the textbox's text
        textbox.text = hitting.ToString();
    }

    public void SetNeighbor(AreaController neighbor) {
        if (!neighbors.Contains(neighbor)) neighbors.Add(neighbor);
    }

    public void GetCurrentPosition() {
        
    }

    public void AddAgentInside(GridAgent agent) {
        if (!agentsInside.Contains(agent)) agentsInside.Add(agent);
    }

    public bool CheckAgentDistances() {
        List<GridAgent> tempIn = new List<GridAgent>();
        Vector2 curPos = new Vector2(transform.position.x, transform.position.z);
        bool hasAgents = false;
        foreach(GridAgent agent in agentsInside) {
            // We can grab position of agent from `agent.transfrom.position`
            // We can grab position from this transform
            if (Vector2.Distance(curPos, new Vector2(agent.transform.position.x, agent.transform.position.z)) <= agent.radius) {
                if (!tempIn.Contains(agent)) tempIn.Add(agent);
            }
        }
        hasAgents = (tempIn.Count == 0);
        hitting = (hasAgents) ? 1f : 0f;
        agentsInside = tempIn;
        return hasAgents;
    }

    /*
    public void SearchForObstacle() {
        // The raycast is performed relative to the origin of this position in the world.
        RaycastHit hit;
        Vector3[] positions = new Vector3[2];
        positions[0] = transform.position;

        hitting = Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity);
        textbox.text = hitting.ToString();
        if (hitting) {
            positions[1] = hit.point;
        } else {
            positions[1] = transform.position;
        }
        lr.SetPositions(positions);
    }
    */
}
