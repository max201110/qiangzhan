using UnityEngine; using System;
public class MissionObjective : MonoBehaviour { public string title="Reach extraction"; public bool completed; public event Action Completed; public void Complete(){if(completed)return;completed=true;Completed?.Invoke();} }
