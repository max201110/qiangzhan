using UnityEngine;
public class Projectile : MonoBehaviour { public float speed=55,damage=30,life=4; void Update(){transform.position+=transform.forward*speed*Time.deltaTime; life-=Time.deltaTime;if(life<=0)Destroy(gameObject);} void OnTriggerEnter(Collider c){var h=c.GetComponent<MechHealth>();if(h){h.ApplyDamage(damage);Destroy(gameObject);}} }
