using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

[System.Serializable]
public class CarPath {
    public string name;
    public PathCreator pathCreator;
    public Transform startTarget, middleTarget, endTarget;
    public TrafficSignal trafficSignal; 
}

public class StreetSimCarManager : MonoBehaviour
{
    public static StreetSimCarManager CM;
    public enum CarManagerStatus {
        Off,
        NoCongestion,
        MinimalCongestion,
        Congested
    }

    public CarManagerStatus m_status = CarManagerStatus.Off;
    [SerializeField] private List<CarPath> m_carPaths = new List<CarPath>();
    private Dictionary<string, CarPath> m_carPathDict = new Dictionary<string, CarPath>();
    [SerializeField] private List<StreetSimCar> m_cars = new List<StreetSimCar>();

    [SerializeField] private List<StreetSimCar> activeCars = new List<StreetSimCar>();
    [SerializeField] private List<StreetSimCar> inactiveCars = new List<StreetSimCar>();


    private void Awake() {
        CM = this;
        foreach(CarPath path in m_carPaths) {
            if (!m_carPathDict.ContainsKey(path.name)) m_carPathDict.Add(path.name, path);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch(m_status) {
            case CarManagerStatus.Off:
                // Active cars = 0
                // Put off on printing new cars onto the scene
                break;
            case CarManagerStatus.NoCongestion:
                // Active cars = 5
                // 
                break;
            case CarManagerStatus.MinimalCongestion:
                // Active cars = 10
                break;
            case CarManagerStatus.Congested:
                // Active cars = ALL OF THEM (that can reasonabily fit)
                break;
        }
    }
    
    public CarPath GetCarPathFromName(string name) {
        if (m_carPathDict.ContainsKey(name)) return m_carPathDict[name];
        return null;
    }
}
