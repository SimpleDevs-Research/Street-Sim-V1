using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ModelMeshMapper {
    public string modelID;
    public ExperimentID mesh;
}

public class StreetSimModelMapper : MonoBehaviour
{
    public static StreetSimModelMapper M;
    public List<ModelMeshMapper> maps = new List<ModelMeshMapper>();
    private Dictionary<string, ExperimentID> mapDict = new Dictionary<string, ExperimentID>();

    private ExperimentID m_currentMesh = null;

    private void Awake() {
        M = this;
        foreach(ModelMeshMapper mapper in maps) {
            if (!mapDict.ContainsKey(mapper.modelID)) mapDict.Add(mapper.modelID, mapper.mesh);
        }
    }

    public bool MapMeshToModel(StreetSimAgent model) {
        if (!mapDict.ContainsKey(model.gameObject.GetComponent<ExperimentID>().id)) return false;
        ExperimentID newMesh = Instantiate(mapDict[model.gameObject.GetComponent<ExperimentID>().id],model.transform.position,model.transform.rotation, StreetSim.S.agentMeshParent) as ExperimentID;
        // Need to make the following changes:
        //      [1] Follow Position: Set the followed model to `model`
        newMesh.GetComponent<FollowPosition>().toFollow = model.transform;
        //      [2] Skinned Mesh Renderer Helper: set the skinned mesh renderer reference
        newMesh.GetComponent<SkinnedMeshRendererHelper>().meshRenderer = model.GetRenderer();
        newMesh.GetComponent<SkinnedMeshRendererHelper>().Initialize();
        // And we're set!
        m_currentMesh = newMesh;
        return true;
    }

    public void DestroyMesh() {
        if (m_currentMesh == null) return;
        Destroy(m_currentMesh.gameObject);
        m_currentMesh = null;
    }
}
