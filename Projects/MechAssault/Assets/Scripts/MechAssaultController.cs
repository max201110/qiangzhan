using UnityEngine;
public class MechAssaultController : MonoBehaviour { public float armor=100,heat,cannonCooldown=.8f; float nextShot; void Update(){heat=Mathf.Max(0,heat-Time.deltaTime*12);if(Input.GetKeyDown(KeyCode.Mouse0)&&Time.time>=nextShot&&heat<90){nextShot=Time.time+cannonCooldown;heat+=18;Debug.Log("Remote cannon fired");}} }
