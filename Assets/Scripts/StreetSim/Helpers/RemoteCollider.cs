using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoteCollider : MonoBehaviour
{
    public Dictionary<Collider, float> colliders = new Dictionary<Collider,float>();
    public Transform parent;
    public int numColliders = 0;
    public LayerMask layerMask;

    private void Update() {
        Dictionary<Collider,float> updatedList = new Dictionary<Collider,float>();
        foreach(KeyValuePair<Collider,float> kvp in colliders) {
            if (kvp.Key.gameObject.activeInHierarchy) {
                updatedList.Add(kvp.Key, kvp.Value);
            }
        }
        colliders = updatedList;
        numColliders = colliders.Count;
    }

    private void OnTriggerEnter(Collider col) {
        if ((layerMask.value & (1 << col.transform.gameObject.layer)) > 0) {
            if (!colliders.ContainsKey(col)) {
                colliders.Add(col, GetDistanceBetweenColliderAndParent(col.transform));
            }
        }
    }
    private void OnTriggerStay(Collider col) {
        if ((layerMask.value & (1 << col.transform.gameObject.layer)) > 0) {
            if (!colliders.ContainsKey(col)) {
                colliders.Add(col, GetDistanceBetweenColliderAndParent(col.transform));
            } else {
                colliders[col] = GetDistanceBetweenColliderAndParent(col.transform);
            }
        }
    }
    private void OnTriggerExit(Collider col) {
        if (colliders.ContainsKey(col)) colliders.Remove(col);
    }

    private float GetDistanceBetweenColliderAndParent(Transform colTransform) {
        return (parent != null) ? Vector3.Distance(colTransform.position, parent.position) : 0f;
    }

    public Collider GetClosestCollider() {
        Collider closest = null;
        float closestDistance = Mathf.Infinity;
        foreach(KeyValuePair<Collider,float> kvp in colliders) {
            if (closest == null || kvp.Value < closestDistance) {
                closest = kvp.Key;
                closestDistance = kvp.Value;
            }
        }
        return closest;
    }
}
