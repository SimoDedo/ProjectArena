using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// The game manager manages the game, it passes itself to the player.

public abstract class GameManager : CoreComponent {

    [Header("Managers")] [SerializeField] protected GameObject mapManager;
    [SerializeField] protected GameObject spawnPointManager;

    [Header("Game")] [SerializeField] protected bool generateOnly;
    [SerializeField] protected int gameDuration = 600;
    [SerializeField] protected int readyDuration = 3;
    [SerializeField] protected int scoreDuration = 10;
    [SerializeField] protected float respawnDuration = 3;

    protected MapManager mapManagerScript;
    protected SpawnPointManager spawnPointManagerScript;

    // Time at which the game started.
    protected float startTime;
    // Current phase of the game: 0 = ready, 1 = figth, 2 = score.
    protected int gamePhase = -1;
    // Is the game paused?
    protected bool isPaused = false;

    // Moves a gameobject to a free spawn point.
    public void Spawn(GameObject g) {
        g.transform.position = spawnPointManagerScript.GetSpawnPosition() + Vector3.up * 3f;
    }

    // Menages the death of an entity.
    public abstract void MenageEntityDeath(GameObject g, Entity e);

    protected abstract void UpdateGamePhase();

    protected abstract void ManageGame();

    public abstract void AddScore(int i, int j);

    public abstract void SetUIColor(Color c);

    public abstract void Pause();

    public IEnumerator FreezeTime(float wait, bool mustPause) {
        if (mustPause)
            yield return new WaitForSeconds(wait);
        Time.timeScale = isPaused ? 0f : 1f;            
    }

    public void Quit() {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene("Menu");
    }

}