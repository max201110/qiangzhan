using UnityEngine;
public class LootPickup : MonoBehaviour { public enum Kind{Health,Ammo,Score}; public Kind kind; public int amount=25; void OnTriggerEnter(Collider other){if(!other.CompareTag("Player"))return; if(kind==Kind.Score&&GameState.I)GameState.I.AddScore(amount); Destroy(gameObject);} }
