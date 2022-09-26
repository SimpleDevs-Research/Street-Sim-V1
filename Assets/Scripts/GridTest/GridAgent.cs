using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridAgent : MonoBehaviour
{
    [SerializeField] private int index;
    [SerializeField] private DistrictController parent;
    [SerializeField] 
    private float m_radius = 0.25f;
    public float radius {
        get { return m_radius; }
        set {}
    }
    [SerializeField] private List<Vector2Int> areaIndices = new List<Vector2Int>();

    public void Initialize(DistrictController parent, int index) {
        this.parent = parent;
        this.index = index;
    }

    private void Update() {
        if (parent == null) return;
        parent.UpdateAgentPosition(this, transform.position, radius);
    }
    
    public void SetAreaIndices(List<Vector2Int> indices) {
        areaIndices = indices;
    }

    public bool CheckIfCloseToArea(Vector2Int areaIndex) {
        return areaIndices.Contains(areaIndex);
    }

    /*
    public void CheckAreas() {
        Debug.Log("Checking areas");
        // Iterate through areas we last known to be in or close to, check if they're in the radius
        List<Vector2Int> stillIn = new List<Vector2Int>();
        Vector2 positionOnPlane = new Vector2(transform.position.x, transform.position.z);
        foreach(Vector2Int areaIndex in areaIndices) {
            AreaController area = parent.GetAgentFromIndex(areaIndex);
            if (area == null) continue;
            float dist = Vector2.Distance(positionOnPlane, new Vector2(area.transform.position.x, area.transform.position.z));
            Debug.Log(areaIndex.ToString() + ": " + dist.ToString());
            if (dist <= m_radius) {
                stillIn.Add(areaIndex);
            } else {

            }
        }
        areaIndices = stillIn;
    }
    */
}
