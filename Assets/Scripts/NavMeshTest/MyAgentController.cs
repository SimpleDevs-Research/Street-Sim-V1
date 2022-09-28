using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable] 
public class MyAgentDestinationBehavior {
    public Transform destination;
    public Transform lookAtTarget = null;
}

[System.Serializable]
public class MyAgentBehavior {
    public NavMeshAgent agent;
    public Transform startPosition;
    public MyAgentDestinationBehavior[] movementBehavior;
    public bool repeat;
}

public class MyAgentController : MonoBehaviour
{

    [SerializeField] private List<MyAgentBehavior> actions = new List<MyAgentBehavior>();

    private void Start() {
        foreach(MyAgentBehavior action in actions) {
            StartCoroutine(PerformBehavior(action));
        }
    }

    private IEnumerator PerformBehavior(MyAgentBehavior behavior) {
        do {
            behavior.agent.transform.position = behavior.startPosition.position;
            for(int i = 0; i < behavior.movementBehavior.Length; i++) {
                behavior.agent.SetDestination(behavior.movementBehavior[i].destination.position);
                if(behavior.agent.GetComponent<IKManager>() != null) behavior.agent.GetComponent<IKManager>().targetTransform = behavior.movementBehavior[i].lookAtTarget;
                while(behavior.agent.remainingDistance > behavior.agent.stoppingDistance) {
                    yield return null;
                }
                yield return null;
            }
        } while(behavior.repeat);
        yield return null;
    }

}
