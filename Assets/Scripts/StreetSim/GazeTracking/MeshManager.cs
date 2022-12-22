using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TriangleToIdMap {
    public int triangleIndex;
    public Vector3 position;
    public ExperimentID id;
    public TriangleToIdMap(int triangleIndex, Vector3 position, ExperimentID id) {
        this.triangleIndex = triangleIndex;
        this.position = position;
        this.id = id;
    }
}

public class MeshManager : MonoBehaviour
{
    public Transform scaler, hips;
    public ExperimentID startingPoint;
    public Material GazeOnObjectMaterial;
    public Dictionary<int, TriangleToIdMap> triangleToID = new Dictionary<int, TriangleToIdMap>();
    /*
    public Transform TestRaycaster;
    public LayerMask TestRaycasterLayerMask;
    public TriangleToIdMap closestMap = null;
    */

    void OnDrawGizmosSelected() {
        if (triangleToID.Count == 0) return;
        foreach(TriangleToIdMap m in triangleToID.Values) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(m.position, 0.01f);
        }
        /*
        if (closestMap != null) {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(closestMap.position,0.02f);
        }
        */
    }

    private void Awake() {
        MapTrianglesToIDs();
    }

    public void MapTrianglesToIDs() {
        GameObject temp = new GameObject("mesher");
        temp.transform.parent = this.transform;
        temp.transform.localPosition = Vector3.zero;
        temp.transform.localRotation = Quaternion.identity;
        temp.transform.localScale = Vector3.Scale(scaler.localScale,hips.localScale);
        MeshFilter filter = temp.AddComponent<MeshFilter>();
        MeshRenderer renderer = temp.AddComponent<MeshRenderer>();
        Material[] matsToSet = new Material[1];
        matsToSet[0] = GazeOnObjectMaterial;
        renderer.materials = matsToSet;
        Mesh mesh = Instantiate(gameObject.GetComponent<SkinnedMeshRendererHelper>().meshRenderer.sharedMesh);
        mesh.SetTriangles(mesh.triangles, 0);
        mesh.subMeshCount = 1;
        filter.sharedMesh = mesh;

        ExperimentID[] ids = startingPoint.gameObject.GetComponentsInChildren<ExperimentID>();
        triangleToID = new Dictionary<int, TriangleToIdMap>();

        Vector3[] vertices = mesh.vertices;
        int[] indices = mesh.GetIndices(0); 
        int[] triangles = mesh.triangles;

        for(int i = 0; i < triangles.Length; i+=3) {
            // Get vertices
            int firstIndexLocation = i;
            int firstIndice = indices[firstIndexLocation];
            Vector3 firstVertex = vertices[firstIndice];
            Vector3 secondVertex = vertices[indices[firstIndexLocation + 1]];
            Vector3 thirdVertex = vertices[indices[firstIndexLocation + 2]];
            // Calculate centroid
            Vector3 centroid = (firstVertex + secondVertex + thirdVertex) / 3f;
            // Calculate world position of centroid
            Vector3 centroidWorld = temp.transform.TransformPoint(centroid);
            // Get closest ExperimentID to centroid
            float closestDistance = Vector3.Distance(centroidWorld,ids[0].transform.position);
            float curDistance;
            ExperimentID closest = ids[0];
            for(int j = 1; j < ids.Length; j++) {
                curDistance = Vector3.Distance(centroidWorld, ids[j].transform.position);
                if(curDistance < closestDistance) {
                    closest = ids[j];
                    closestDistance = curDistance;
                }
            }
            //triangleToID.Add(i,closest); 
            triangleToID.Add(i, new TriangleToIdMap(i,centroidWorld,closest));
        }

        DestroyImmediate(mesh);
        DestroyImmediate(temp);

        Debug.Log(triangleToID.Count);
    }

    public TriangleToIdMap GetClosestFromTriangleIndex(int triangleIndex) {
        int i = triangleIndex * 3;
        return triangleToID[i];
    }
    
    /*
    private void Update() {
        if (TestRaycaster == null) return;
        RaycastHit hit;
        if(Physics.Raycast(TestRaycaster.position, TestRaycaster.forward, out hit, 10f, TestRaycasterLayerMask)) {
            closestMap = GetClosestFromTriangleIndex(hit.triangleIndex);
        } else {
            closestMap = null;
        }
    }
    */
}
