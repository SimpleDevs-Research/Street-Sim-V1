using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using PathCreation;

[System.Serializable]
public class CarPath {
    public string name;
    public PathCreator pathCreator;
    public Transform startTarget, middleTarget, endTarget;
    public TrafficSignal trafficSignal;
    public RemoteCollider startCollisionDetector;
    public Queue<StreetSimCar> waitingCars = new Queue<StreetSimCar>();
}

public class StreetSimCarManager : MonoBehaviour
{
    public static StreetSimCarManager CM;
    public enum CarManagerStatus {
        Off,
        NoCongestion,
        MinimalCongestion,
        SomeCongestion,
        Congested
    }

    public CarManagerStatus status = CarManagerStatus.Off;
    [SerializeField] private List<CarPath> m_carPaths = new List<CarPath>();
    private Dictionary<string, int> m_carPathDict = new Dictionary<string, int>();
    [SerializeField] private List<StreetSimCar> m_cars = new List<StreetSimCar>();

    [SerializeField] private List<StreetSimCar> activeCars = new List<StreetSimCar>();
    [SerializeField] private Queue<StreetSimCar> waitingCars = new Queue<StreetSimCar>();
    private Dictionary<CarManagerStatus, Vector2> waitValues = new Dictionary<CarManagerStatus, Vector2> {
        { CarManagerStatus.Off, new Vector2(0f,0f) },
        //{ CarManagerStatus.NoCongestion, new Vector2(8f, 5f) },
        //{ CarManagerStatus.MinimalCongestion, new Vector2(4f,10f) },
        { CarManagerStatus.NoCongestion, new Vector2(10f, 5f) },
        { CarManagerStatus.MinimalCongestion, new Vector2(10f,5f) },
        { CarManagerStatus.SomeCongestion, new Vector2(2f,15f) },
        { CarManagerStatus.Congested, new Vector2(1f,25f) }
    };
    // x = wait time between actually spawning cars
    // y = total number of active cars allowed on the road.

    [SerializeField] private Transform InactiveCarTargetRef;

    private void Awake() {
        CM = this;
        for(int i = 0; i < m_carPaths.Count; i++) {
            if (!m_carPathDict.ContainsKey(m_carPaths[i].name)) m_carPathDict.Add(m_carPaths[i].name, i);
        }
        waitingCars = new Queue<StreetSimCar>(m_cars.Shuffle());
        StartCoroutine(PrintCars());
    }

    // Update is called once per frame
    void Update()
    {
        if (activeCars.Count + GetWaitingCarsInQueue() < waitValues[status].y) QueueNextCar();
    }

    private IEnumerator PrintCars() {
        float timeToNextCarSpawn;
        while(true) {
            if (status == CarManagerStatus.Off) {
                yield return null;
                continue;
            }
            if (activeCars.Count >= waitValues[status].y) {
                yield return null;
                continue;
            }
            foreach(CarPath path in m_carPaths) {
                if (path.waitingCars.Count > 0) {
                    if (path.startCollisionDetector.numColliders == 0) {
                        StreetSimCar nextCar = path.waitingCars.Dequeue();
                        nextCar.startTarget = path.startTarget;
                        nextCar.middleTarget = path.middleTarget;
                        nextCar.endTarget = path.endTarget;
                        nextCar.trafficSignal = path.trafficSignal;
                        nextCar.Initialize();
                        activeCars.Add(nextCar);
                        timeToNextCarSpawn = UnityEngine.Random.Range(0f,10f);
                        yield return new WaitForSeconds(timeToNextCarSpawn);
                    }
                }
                yield return null;
            }
        }
    }

    private int GetWaitingCarsInQueue() {
        int num = 0;
        foreach(CarPath path in m_carPaths) {
            num += path.waitingCars.Count;
        }
        return num;
    }

    public void SetCarToIdle(StreetSimCar car) {
        if (activeCars.Contains(car)) activeCars.Remove(car);
        car.status = StreetSimCar.StreetSimCarStatus.Idle;
        car.startTarget = null;
        car.middleTarget = null;
        car.endTarget = null;
        car.trafficSignal = null;
        car.transform.position = InactiveCarTargetRef.position;
        waitingCars.Enqueue(car);
    }

    public void QueueNextCar() {
        if (waitingCars.Count == 0) return;
        StreetSimCar nextCar = waitingCars.Dequeue();
        // pick a random place to instantiate to
        if (Random.value<0.5f) {
            m_carPaths[0].waitingCars.Enqueue(nextCar);
        } else {
            m_carPaths[1].waitingCars.Enqueue(nextCar);
        }
    }
    
    public CarPath GetCarPathFromName(string name) {
        if (m_carPathDict.ContainsKey(name)) return m_carPaths[m_carPathDict[name]];
        return null;
    }

    public void SetCongestionStatus(CarManagerStatus newStatus, bool shouldReset = false) {
        status = newStatus;
        if (shouldReset) {
            if (activeCars.Count > 0) {
                Queue<StreetSimCar> deleteActiveQueue = new Queue<StreetSimCar>(activeCars);
                while(deleteActiveQueue.Count > 0) {
                    SetCarToIdle(deleteActiveQueue.Dequeue());
                }
            }
            foreach(CarPath path in m_carPaths) {
                path.waitingCars.Clear();
            }
        }
    }

}
