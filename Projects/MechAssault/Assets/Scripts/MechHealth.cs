using UnityEngine; using System;
public class MechHealth : MonoBehaviour { public float maxArmor=100,armor=100; public event Action Destroyed; public void ApplyDamage(float value){armor=Mathf.Max(0,armor-value);if(armor<=0)Destroyed?.Invoke();} }
