using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinnedMeshRendererHelper : MonoBehaviour
{

    public SkinnedMeshRenderer meshRenderer = null;
    public MeshCollider collider = null;
    public float updateDelay = 0.1f;
    private bool initialized = false;

    // Start is called before the first frame update
    private void Start() {
        if (meshRenderer == null) meshRenderer = GetComponent<SkinnedMeshRenderer>();
        if (collider == null) collider = GetComponent<MeshCollider>();
        Initialize();
    }

    public void Initialize() {
        if (meshRenderer != null && collider != null) {
            if (!initialized) StartCoroutine(UpdateCollider());
        }
    }

    private IEnumerator UpdateCollider() {
        initialized = true;
        while(true) {
            Mesh colliderMesh = new Mesh();
            meshRenderer.BakeMesh(colliderMesh);
            collider.sharedMesh = null;
            collider.sharedMesh = colliderMesh;
            yield return new WaitForSeconds(updateDelay);
        }
    }

}
