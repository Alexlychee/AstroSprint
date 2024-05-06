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

    public void RespawnPlayer()
    {
        if(bench.interacted) {
            respawnPoint = bench.transform.position;
        } else {
            respawnPoint = platformRespawnPoint;
        }
        playerController.Instance.transform.position = respawnPoint;
        StartCoroutine(UIManager.Instance.DeactivateDeathScreen());
        playerController.Instance.Respawned();
    }
}
