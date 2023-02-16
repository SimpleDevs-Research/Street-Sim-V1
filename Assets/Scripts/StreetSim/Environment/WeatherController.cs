using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class WeatherController : MonoBehaviour
{
    public ParticleSystem rainSystem;
    public ParticleSystem snowSystem;   

    public enum WeatherMode {
        Sunny,
        Rainy,
        Snowy
    }
    public WeatherMode weatherMode = WeatherMode.Sunny;

    public bool fogOn = false;
    public Color fogColor = Color.grey;
    public FogMode fogMode = FogMode.Linear;

    [Tooltip("Only works if `Fog Mode` is `Linear`.")]
    public Vector2 fogDistances = new Vector2(5f,300f);
    [Tooltip("Only works if `Fog Mode` is `Exponential` or `Exponential Squared.")]
    public float fogDensity = 0.01f;

    void Update() {
        UpdateWeather();
        UpdateFog();
    }

    private void UpdateWeather() {
        var rainEmission = rainSystem.emission;
        var snowEmission = snowSystem.emission;
        switch(weatherMode) {
            case WeatherMode.Rainy:
                rainEmission.enabled = true;
                snowEmission.enabled = false;
                break;
            case WeatherMode.Snowy:
                rainEmission.enabled = false;
                snowEmission.enabled = true;
                break;
            default:
                rainEmission.enabled = false;
                snowEmission.enabled = false;
                break;
        }
    }

    private void UpdateFog() {
        RenderSettings.fog = fogOn;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;

        if (RenderSettings.fogMode == FogMode.Linear) {
            RenderSettings.fogStartDistance = fogDistances.x;
            RenderSettings.fogEndDistance = fogDistances.y;
        } else {
            RenderSettings.fogDensity = fogDensity;
        }
    }
}
