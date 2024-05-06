using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bench : MonoBehaviour
{
    public bool interacted;

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerStay2D(UnityEngine.Collider2D _collision)
    {
        if(_collision.CompareTag("Player") && Input.GetButtonDown("Interact")) {
            interacted = true;
        }
    }
}
