using UnityEngine; using System;
public class AlarmSystem : MonoBehaviour { public static AlarmSystem I; public bool active; public event Action Activated; void Awake(){I=this;} public void Raise(){if(active)return;active=true;Activated?.Invoke();} }
