using System;
using UnityEngine;

[ExecuteAlways]
class LightManager : MonoBehaviour
{
    [SerializeField] Transform sunParent;
    [SerializeField] Light directionLight;
    [SerializeField] LightPreset preset;
    [SerializeField, Range(0, 24)] float timeOfDay = 12;
    [SerializeField, Range(-90, 90)] float latitude = 0f;

    void Update()
    {
        if(!preset) return;
        float time = timeOfDay / 24;
        updateLight(time);
    }

    void updateLight(float time)
    {
        RenderSettings.ambientLight = preset.ambientColor.Evaluate(time);
        RenderSettings.fogColor = preset.fogColor.Evaluate(time);

        if(directionLight)
        {
            directionLight.color = preset.directionColor.Evaluate(time);
            // Set the time of day rotation of `sunParent`
            sunParent.localRotation = Quaternion.Euler(new Vector3(0f,0f,360f * time));
            // Set the latitude of the `Sun`.
            // At greater latitudes (i.e. lat > 0), sun will appear to be coming in from the south. 
            // In lower latitudes (i.e. lat < 0), the sun will be appearing to come from the north.
            directionLight.transform.localRotation = Quaternion.Euler(new Vector3(latitude-90f,0f,0f));
        }
    }

    void OnValidate()
    {
        if(directionLight)
            return;
        if(RenderSettings.sun)
            directionLight = RenderSettings.sun;
        else
        {
            Light[] lights = GameObject.FindObjectsOfType<Light>();
            for(int i = 0; i < lights.Length; ++i)
            {
                if(lights[i].type == LightType.Directional)
                {
                    directionLight = lights[i];
                    return;
                }
            }
        }
    }
}