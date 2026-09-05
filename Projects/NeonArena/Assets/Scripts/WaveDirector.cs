using UnityEngine;
public class WaveDirector : MonoBehaviour { public int baseEnemies=6; public float spawnInterval=.6f; public int CurrentWave=>GameState.I?GameState.I.wave:1; public int EnemiesForWave()=>baseEnemies+(CurrentWave-1)*3; }
