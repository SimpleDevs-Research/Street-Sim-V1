using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

public class MyNavAgent : MonoBehaviour
{
    private NavMeshAgent agent;
    private ThirdPersonCharacter character;

    private void Awake() {
        agent = GetComponent<NavMeshAgent>();
        character = GetComponent<ThirdPersonCharacter>();
    }

    private void Start() {
        if (agent != null) agent.updateRotation = false;
    }

    private void Update() {
        if (agent == null || character == null) return;
        if (agent.remainingDistance > agent.stoppingDistance) {
            character.Move(agent.desiredVelocity,false,false);
        } else {
            character.Move(Vector3.zero,false,false);
        }
    }
}
