using UnityEngine;
public class WeaponSystem : MonoBehaviour { public int magazine=30,reserve=180; public float fireRate=.12f,damage=24; float nextShot; public bool TryFire(){if(Time.time<nextShot||magazine<=0)return false;nextShot=Time.time+fireRate;magazine--;return true;} public void Reload(){int n=Mathf.Min(30-magazine,reserve);magazine+=n;reserve-=n;} }
