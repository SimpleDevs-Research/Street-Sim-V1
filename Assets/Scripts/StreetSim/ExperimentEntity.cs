using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentEntity : MonoBehaviour
{

    [SerializeField] private ExperimentEntity m_parent = null;
    [SerializeField] private List<ExperimentEntity> m_children - new List<ExperimentEntity>();
    [SerializeField] private string m_entityName = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
