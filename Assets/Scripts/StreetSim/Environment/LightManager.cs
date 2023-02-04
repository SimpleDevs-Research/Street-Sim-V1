using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LightManager : MonoBehaviour
{
    [SerializeField] private Light directionLight;
    [SerializeField] private LightPreset preset;
    [SerializeField, Range(0, 24)] private float timeOfDay = 12.0f;

    public void Update()
    {
        if(!preset) return;
        if(Application.isPlaying)
        {
            timeOfDay += Time.deltaTime;
            timeOfDay %= 24;
            updateLight(timeOfDay / 24f);
        }
        else
            updateLight(timeOfDay / 24f);
    }

    private void updateLight(float timePercent)
    {
        RenderSettings.ambientLight = preset.ambientColor.Evaluate(timePercent);
        RenderSettings.fogColor = preset.fogColor.Evaluate(timePercent);

        if(directionLight)
        {
            directionLight.color = preset.directionColor.Evaluate(timePercent);
            directionLight.transform.localRotation = Quaternion.Euler(new Vector3(360f * timePercent - 90f, 170f, 0));
        }
    }

    private void OnValidate()
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