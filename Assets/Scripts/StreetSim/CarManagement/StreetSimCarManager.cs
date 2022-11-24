using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using SerializableTypes;
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

// UNKNOWN IF USED, CHEKC BACK LATER
[System.Serializable]
public class CarGroup {
    public List<StreetSimCar> cars;
    public CarGroup() {
        cars = new List<StreetSimCar>();
    }
    public CarGroup(StreetSimCar firstCar) {
        cars = new List<StreetSimCar>();
        cars.Add(firstCar);
    }
    public CarGroup(List<StreetSimCar> cars) {
        this.cars = cars;
    }
}


[System.Serializable]
public class CarRow {
    public string name;
    public float crosswalkTime;
    public CarRow(string name, float time) {
        this.name = name;
        this.crosswalkTime = time;
    }
    public static List<string> Headers => new List<string> {
        "name",
        "crosswalkTime"
    };
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
    private Dictionary<CarManagerStatus, Vector3> waitValues = new Dictionary<CarManagerStatus, Vector3> {
        { CarManagerStatus.Off, new Vector3(0f,0f,0f) },
        { CarManagerStatus.NoCongestion, new Vector3(7.5f, 8f, 3f) },
        //{ CarManagerStatus.MinimalCongestion, new Vector2(4f,10f) },
        { CarManagerStatus.MinimalCongestion, new Vector3(5f,5f,6f) },
        { CarManagerStatus.SomeCongestion, new Vector3(2f,15f,7f) },
        { CarManagerStatus.Congested, new Vector3(1f,25f,10f) }
    };
    // x = pause time between spawning car groups
    // y = total number of active cars allowed on the road.
    // z = counter for how many cars need to be spawned before the manager takes a break from spawning

    [SerializeField] private Transform InactiveCarTargetRef;
    [SerializeField] private List<CarRow> m_carHistory = new List<CarRow>();
    public List<CarRow> carHistory { get=>m_carHistory; set{} }

    [SerializeField] private int carSpawnCounter = 0;
    [SerializeField] private float carPathDecider = 0f;

    public LayerMask carDetectionLayerMask;

    //[SerializeField] private List<CarGroup> m_carGroups = new List<CarGroup>();

    // How this works:
    // The cars will move in pelotons, or groups
    // A single group can hold between 1 to many cars, depending on the carmanager status defined. (specificaly, that `y` value)
    
    // We have some variables:
    //  - `m_cars` : List<StreetSimCar>         = merely stores the cars we're using. Set in inspector. Only used in `Awake()` in runtime
    //  - `activeCars` : List<StreetSimCar>     = lets us know how many cars are currently on the road. Doesn't care about groups
    //  - `waitingCars` : Queue<StreetSimCar>   = queue that constantly gets updated based on how many active cars are on the road. Gets refilled every time a car gets reset
    //  - `waitValues` : Dictionary             = controls the traffic behavior based on congestion
    //  - `InactiveCarTargetRef` : Transform    = Tells us where to throw the cars at when the car officially finishes their path on the road
    //  - `m_carHistory` : List<CarRow>         = Saves which cars cross the crosswalk at what time. Purely a post data-processing data scheme. Not relevant here
    //  - `m_carGroups` : List<CarGroup>        = Stores active car groups. Is deleted after 

    // In `Awake()`. we form a queue of waiting cars. These cars are waiting to be assigned to a group
    // In `Update()`, we constantly check if we 

    private void Awake() {
        CM = this;
        for(int i = 0; i < m_carPaths.Count; i++) {
            if (!m_carPathDict.ContainsKey(m_carPaths[i].name)) m_carPathDict.Add(m_carPaths[i].name, i);
        }
        waitingCars = new Queue<StreetSimCar>(m_cars.Shuffle());
        //carPathDecider = Random.value;
        StartCoroutine(PrintCars());
    }

    // Update is called once per frame
    void Update()
    {
        //QueueNextCar();
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
                        if (carSpawnCounter < (int)waitValues[status].z) {
                            timeToNextCarSpawn = UnityEngine.Random.Range(1f,2f);
                            carSpawnCounter += 1;
                        } else {
                            timeToNextCarSpawn = waitValues[status].x;
                            carSpawnCounter = 0;
                            //carPathDecider = Random.value;
                        }
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

    public void AddCarMidToHistory(StreetSimCar car, float time) {
        m_carHistory.Add(new CarRow(car.id.id, time));
    }
    public void ClearData() {
        m_carHistory = new List<CarRow>();
    }

}
