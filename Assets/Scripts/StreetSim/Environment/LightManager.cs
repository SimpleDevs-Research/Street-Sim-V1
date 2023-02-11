using System;
using UnityEngine;

[ExecuteAlways]
class LightManager : MonoBehaviour
{
    [SerializeField] Light directionLight;
    [SerializeField] LightPreset preset;
    [SerializeField, Range(0, 24)] float timeOfDay = 12;
    [SerializeField, Range(0, 1)] float latitude = 0.5f;

    void Update()
    {
        if(!preset) return;
        float time = timeOfDay / 24;
        if(Application.isPlaying)
        {
            timeOfDay += Time.deltaTime;
            timeOfDay %= 24;
            updateLight(time);
        }
        else
            updateLight(time);
    }

    void updateLight(float time)
    {
        RenderSettings.ambientLight = preset.ambientColor.Evaluate(time);
        RenderSettings.fogColor = preset.fogColor.Evaluate(time);

        if(directionLight)
        {
            directionLight.color = preset.directionColor.Evaluate(time);
            directionLight.transform.localRotation = Quaternion.Euler(new Vector3(360f * (time + latitude) - 270f, latitude * 360f, 0));
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