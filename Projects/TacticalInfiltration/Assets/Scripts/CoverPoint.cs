using UnityEngine;
public class CoverPoint : MonoBehaviour { public bool occupied; public bool IsSafe(Vector3 threat){return !Physics.Linecast(transform.position+Vector3.up,threat,~0,QueryTriggerInteraction.Ignore);} }
