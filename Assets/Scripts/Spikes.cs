using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spikes : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D _other) {
        if(_other.CompareTag("Player")) {
            StartCoroutine(RespawnPoint());
        }
    }

    IEnumerator RespawnPoint() {
        playerController.Instance.pState.cutscene = true;
        playerController.Instance.pState.invincible = true;
        playerController.Instance.rb.velocity = Vector2.zero;
        Time.timeScale = 0f;
        StartCoroutine(UIManager.Instance.sceneFader.Fade(SceneFader.FadeDirection.In));
        playerController.Instance.TakeDamage(1);
        yield return new WaitForSecondsRealtime(1f);
        Time.timeScale = 1;
        playerController.Instance.transform.position = GameManager.Instance.platformRespawnPoint;
        StartCoroutine(UIManager.Instance.sceneFader.Fade(SceneFader.FadeDirection.Out));
        yield return new WaitForSecondsRealtime(UIManager.Instance.sceneFader.fadeTime);
        playerController.Instance.pState.cutscene = false;
        playerController.Instance.pState.invincible = false;
    }

}
