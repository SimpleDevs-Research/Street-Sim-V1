using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinnedMeshRendererHelper : MonoBehaviour
{

    [SerializeField] private SkinnedMeshRenderer meshRenderer = null;
    [SerializeField] private MeshCollider collider = null;
    [SerializeField] private float updateDelay = 0.1f;

    // Start is called before the first frame update
    private void Start() {
        if (meshRenderer == null) meshRenderer = GetComponent<SkinnedMeshRenderer>();
        if (collider == null) collider = GetComponent<MeshCollider>();
        StartCoroutine(UpdateCollider());
    }

    private IEnumerator UpdateCollider() {
        while(true) {
            Mesh colliderMesh = new Mesh();
            meshRenderer.BakeMesh(colliderMesh);
            collider.sharedMesh = null;
            collider.sharedMesh = colliderMesh;
            yield return new WaitForSeconds(updateDelay);
        }
    }

}
