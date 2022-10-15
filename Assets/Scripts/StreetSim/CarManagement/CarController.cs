using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

[System.Serializable]
public class CarInstantiation {
    public int carIndex;
    public float instantiationDelay;
}

[System.Serializable]
public class CarRoute {
    public string name;
    public PathCreator pathCreator;
    public CarInstantiation[] cars;
}

public class StreetSimCarController : MonoBehaviour
{
    public static StreetSimCarController C;

    [SerializeField] private GameObject[] carPrefabs;
    // [SerializeField] private List<CarAgent> m_cars = new List<CarAgent>();
    [SerializeField] private List<CarRoute> m_routes = new List<CarRoute>();

    // private Dictionary<string, CarRoute> m_routeDict = new Dictionary<string, CarRoute>();

    private void Awake() {
        C = this;
    }

    private void StartCarController() {
        foreach(CarRoute route in m_routes) {
            StartCoroutine(StartPath(route));
        }
    }

    private IEnumerator StartPath(CarRoute route) {
        for (int i = 0; i < route.cars.Length; i++) {
            yield return new WaitForSeconds(route.cars[i].instantiationDelay);
            GameObject car = Instantiate(carPrefabs[route.cars[i].carIndex],route.pathCreator.path.GetPointAtDistance(0f),Quaternion.identity);
            car.GetComponent<CarPathFollower>().pathCreator = route.pathCreator;
        }
        yield return null;
    }

    /*
    public void AddCar(CarAgent car) {
        if (!m_cars.Contains(car)) m_cars.Add(car);
    }

    public bool GetRoute(string name, out CarRoute route) {
        if (!m_routeDict.ContainsKey(name)) {
            route = null;
            return false;
        }
        route = m_routeDict[name];
        return true;
    }
    */
}
