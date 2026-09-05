using UnityEngine;
public class TargetLock : MonoBehaviour { public Transform target; public float range=80; public void Acquire(Transform candidate){if(candidate&&Vector3.Distance(transform.position,candidate.position)<=range)target=candidate;} public void Clear(){target=null;} }
