using UnityEngine;


[CreateAssetMenu(fileName = "New UWS Colors", menuName = "TidefallEscape/Colors Preset")]
public class ColorsPreset : ScriptableObject
{
    public Gradient _absorptionRamp;
    public Gradient _scatterRamp;
}
