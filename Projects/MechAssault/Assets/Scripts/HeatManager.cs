using UnityEngine;
public class HeatManager : MonoBehaviour { public float heat,maxHeat=100,coolRate=14; public bool Overheated=>heat>=maxHeat; void Update(){heat=Mathf.Max(0,heat-coolRate*Time.deltaTime);} public bool TryAdd(float value){if(Overheated)return false;heat=Mathf.Min(maxHeat,heat+value);return true;} }
