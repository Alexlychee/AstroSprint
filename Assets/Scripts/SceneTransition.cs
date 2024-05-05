using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] private string transitionTo;
    [SerializeField] Transform startPoint;
    [SerializeField] private Vector2 exitDirection;
    [SerializeField] private float exitTime;

    private void Start() {
        if(transitionTo == GameManager.Instance.transitionedFromScene) {
            playerController.Instance.transform.position = startPoint.position;
            StartCoroutine(playerController.Instance.WalkIntoNewScene(exitDirection, exitTime));
        }
    }

    private void OnTriggerEnter2D(Collider2D _other) {
        if(_other.CompareTag("Player")) {
            GameManager.Instance.transitionedFromScene = SceneManager.GetActiveScene().name;
            playerController.Instance.pState.cutscene = true;
            SceneManager.LoadScene(transitionTo);
        }
    }

}
