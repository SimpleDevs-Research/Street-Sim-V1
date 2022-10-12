using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoteCollider : MonoBehaviour
{
    public List<Collider> colliders = new List<Collider>();

    private void Update() {
        List<Collider> updatedList = new List<Collider>();
        foreach(Collider col in colliders) {
            if (col.gameObject.activeInHierarchy) updatedList.Add(col);
        }
        colliders = updatedList;
    }

    private void OnTriggerEnter(Collider col) {
        if (!colliders.Contains(col)) colliders.Add(col);
        Debug.Log("COLLIDER FOUND");
    }
    private void OnTriggerStay(Collider col) {
        if (!colliders.Contains(col)) colliders.Add(col);
        Debug.Log("COLLIDER STAY");
    }
    private void OnTriggerExit(Collider col) {
        if (colliders.Contains(col)) colliders.Remove(col);
        Debug.Log("COLLIDER LEAVING");
    }
}
