using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] private string transitionTo;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Vector2 exitDirection;
    [SerializeField] private float exitTime;

    private void OnTriggerEnter2D(Collider2D _other) {
        if(_other.CompareTag("Player")) {
            SceneManager.LoadScene(transitionTo);
        }
    }
} 
