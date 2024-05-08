using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public string transitionedFromScene;
    public Vector2 platformRespawnPoint;
    public Vector2 respawnPoint;
    [SerializeField] Bench bench;
    
    [SerializeField] private FadeUI pauseMenu;
    [SerializeField] private float fadeTime;
    public bool gameIsPaused;

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        DontDestroyOnLoad(gameObject);
        bench = FindAnyObjectByType<Bench>();
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Escape) && !gameIsPaused) {
            pauseMenu.FadeUIIn(fadeTime);
            Time.timeScale = 0;
            gameIsPaused = true;
        }
    }

    public void UnpauseGame() {
        Time.timeScale = 1;
        gameIsPaused = false;
    }

    public void RespawnPlayer()
    {
        if(bench) {
            respawnPoint = bench.transform.position;
        } else {
            respawnPoint = platformRespawnPoint;
        }
        playerController.Instance.transform.position = respawnPoint;
        StartCoroutine(UIManager.Instance.DeactivateDeathScreen());
        playerController.Instance.Respawned();
    }
}
