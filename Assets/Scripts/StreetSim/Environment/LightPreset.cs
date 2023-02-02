using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "LightPreset", menuName = "Scriptables/LightPreset", order = 1)]
public class LightPreset : ScriptableObject
{
    public Gradient ambientColor, directionColor, fogColor;
}
