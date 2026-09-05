using UnityEngine;
public class VisionSensor : MonoBehaviour { public float distance=20,angle=70; public bool CanSee(Transform target){var d=target.position-transform.position;if(d.magnitude>distance||Vector3.Angle(transform.forward,d)>angle*.5f)return false;return !Physics.Linecast(transform.position,target.position,~0,QueryTriggerInteraction.Ignore); } }
