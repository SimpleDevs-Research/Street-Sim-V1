using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable] 
public class MyNavAgentDestinationBehavior {
    public enum LookAtBehavior {
        Always,
        WhenStill,
        WhenMoving,
        Never
    }
    public Transform destination;
    public Transform lookAtTarget = null;
    public Transform dontLookAtTarget = null;
    public LookAtBehavior lookAtBehavior = LookAtBehavior.Always;
}

[System.Serializable]
public class MyNavAgentBehavior {
    public string name;
    public NavMeshAgent agent;
    public bool enabled = true;
    public MyNavAgentDestinationBehavior[] movementBehavior;
    public bool repeat;
}

public class MyNavAgentController : MonoBehaviour
{

    [SerializeField] private List<NavMeshAgent> agents = new List<NavMeshAgent>();

    [SerializeField] private List<MyNavAgentBehavior> actions = new List<MyNavAgentBehavior>();
    [SerializeField] private bool _debug = false;

    private void Start() {
        foreach(MyNavAgentBehavior action in actions) {
            StartCoroutine(PerformBehavior(action));
        }
    }

    private IEnumerator PerformBehavior(MyNavAgentBehavior behavior) {
        if (_debug) Debug.Log("Starting Behavior: " + behavior.name);
        NavMeshPath navMeshPath = new NavMeshPath();
        bool pathCalculated = false, arrivedAtTarget = false, isLookingAtTarget = false, shouldAdvance = false;
        int behaviorIndex;
        Transform currentDestination;
        string distancePrint;
        do {
            if (behavior.enabled == false) {
                yield return null;
                continue;
            }
            behaviorIndex = 0;
            while(behaviorIndex < behavior.movementBehavior.Length) {
                // Check if a path is calculated
                shouldAdvance = false;
                if (!pathCalculated) {
                    distancePrint = "-";
                    behavior.agent.CalculatePath(behavior.movementBehavior[behaviorIndex].destination.position, navMeshPath);
                    if (navMeshPath.status == NavMeshPathStatus.PathComplete){
                        // Our path is complete, let's set destination
                        behavior.agent.SetDestination(behavior.movementBehavior[behaviorIndex].destination.position);
                        pathCalculated = true;
                        if (_debug) Debug.Log("Path has been calculated for " + behavior.name);
                        behavior.agent.Resume();
                    } else {
                        // Our path can't be made... We have to stop the agent
                        behavior.agent.Stop();
                        pathCalculated = false;
                        if (_debug) Debug.Log("Can't calculate path for " + behavior.name);
                    }
                }
                // Only advance if we hit the target
                else {
                    distancePrint = behavior.agent.remainingDistance.ToString("F3");
                    if (behavior.agent.remainingDistance <= behavior.agent.stoppingDistance) {
                        if (_debug) Debug.Log("The agent has reached their destination. Prepping for next destination");
                        pathCalculated = false;
                        navMeshPath = new NavMeshPath();
                        behavior.agent.Stop();
                        shouldAdvance = true;
                    }
                }

                // remove target designated under `dontLookAtTarget`
                if (behavior.movementBehavior[behaviorIndex].dontLookAtTarget != null) behavior.agent.gameObject.GetComponent<MyNavAgentHeadTurn>().RemoveTarget(behavior.movementBehavior[behaviorIndex].dontLookAtTarget);
                if (behavior.movementBehavior[behaviorIndex].lookAtTarget == null) {
                    yield return null;
                    continue;
                }
                // Set the look at target
                switch(behavior.movementBehavior[behaviorIndex].lookAtBehavior) {
                    case MyNavAgentDestinationBehavior.LookAtBehavior.Always:
                        //behavior.agent.gameObject.GetComponent<MyNavAgentHeadTurn>().targetTransform = behavior.movementBehavior[behaviorIndex].lookAtTarget;
                        behavior.agent.gameObject.GetComponent<MyNavAgentHeadTurn>().SetTarget(behavior.movementBehavior[behaviorIndex].lookAtTarget);
                        break;
                    case MyNavAgentDestinationBehavior.LookAtBehavior.Never:
                        // behavior.agent.gameObject.GetComponent<MyNavAgentHeadTurn>().targetTransform = null;
                        behavior.agent.gameObject.GetComponent<MyNavAgentHeadTurn>().RemoveTarget(behavior.movementBehavior[behaviorIndex].lookAtTarget);
                        break;
                    case MyNavAgentDestinationBehavior.LookAtBehavior.WhenStill:
                        if ( behavior.agent.gameObject.GetComponent<VelocityTracker>().velocity.magnitude <= 0.05f) {
                             //behavior.agent.gameObject.GetComponent<MyNavAgentHeadTurn>().targetTransform = behavior.movementBehavior[behaviorIndex].lookAtTarget;
                            behavior.agent.gameObject.GetComponent<MyNavAgentHeadTurn>().SetTarget(behavior.movementBehavior[behaviorIndex].lookAtTarget);
                        }
                        else {
                            //behavior.agent.gameObject.GetComponent<MyNavAgentHeadTurn>().targetTransform = null;
                            behavior.agent.gameObject.GetComponent<MyNavAgentHeadTurn>().RemoveTarget(behavior.movementBehavior[behaviorIndex].lookAtTarget);
                        }
                        break;
                    case MyNavAgentDestinationBehavior.LookAtBehavior.WhenMoving:
                        if ( behavior.agent.gameObject.GetComponent<VelocityTracker>().velocity.magnitude > 0.05f) {
                            // behavior.agent.gameObject.GetComponent<MyNavAgentHeadTurn>().targetTransform = behavior.movementBehavior[behaviorIndex].lookAtTarget;
                            behavior.agent.gameObject.GetComponent<MyNavAgentHeadTurn>().SetTarget(behavior.movementBehavior[behaviorIndex].lookAtTarget);
                        }
                        else {
                            //behavior.agent.gameObject.GetComponent<MyNavAgentHeadTurn>().targetTransform = null;
                            behavior.agent.gameObject.GetComponent<MyNavAgentHeadTurn>().RemoveTarget(behavior.movementBehavior[behaviorIndex].lookAtTarget);
                        }
                        break;
                }

                if (shouldAdvance) {
                    behaviorIndex++;
                }
                //Debug.Log("Agent's remanining distance: " + distancePrint + "\nAgent's current velocity: " + behavior.agent.GetComponent<VelocityTracker>().velocity.magnitude.ToString("F3"));
                yield return null;
            }
        } while(behavior.repeat);
        yield return null;
    }

}
