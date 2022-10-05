using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using Helpers;

[System.Serializable]
public class ExperimentIDRef {
    public string id;
    public ExperimentID experimentObject;
    public ExperimentIDRef(string id, ExperimentID experimentObject) {
        this.id = id;
        this.experimentObject = experimentObject;
    }
}

public class ExperimentGlobalController : MonoBehaviour
{
    public static ExperimentGlobalController current;

    [SerializeField] private List<ExperimentIDRef> IDs = new List<ExperimentIDRef>();
    private Dictionary<string,ExperimentID> IDsDict = new Dictionary<string,ExperimentID>();

    private void Awake() {
        current = this;
    }

    public void AddID(ExperimentID newID) {
        string id = newID.id;
        while(IDsDict.ContainsKey(id)) {
            // Keep finding alternatives until we find no match
            Match m = Regex.Match(id, @"\d+$");
            if (m.Success) {
                // There is a number... so we modify that number
                int endInt;
                int.TryParse(m.Value, out endInt);
                id = id.Substring(0,m.Index) + (endInt+1);
            } else {
                // No number - so we add one
                id += "1";
            }
        }
        // Let newID know its new id
        newID.SetID(id);
        // Do our adding thing
        IDs.Add(new ExperimentIDRef(id, newID));
        IDsDict.Add(id, newID);
    }

    public bool FindID<T>(string queryID, out T outComponent) {
        if (IDsDict.ContainsKey(queryID)) {
            T comp = default(T);
            bool found = HelperMethods.HasComponent<T>(IDsDict[queryID].gameObject, out comp);
            outComponent = comp;
            return found;
        } else {
            outComponent = default(T);
            return false;
        }
    }  
}
