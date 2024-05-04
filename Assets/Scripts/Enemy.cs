using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] protected float health;
    [SerializeField] protected float recoilLength;
    [SerializeField] protected float recoilFactor;
    [SerializeField] protected bool isRecoiling = false;

    [SerializeField] protected playerController player;
    [SerializeField] protected float speed;

    [SerializeField] protected float damage;

    protected float recoilTimer;
    protected Rigidbody2D rb;
    // Start is called before the first frame update
    protected virtual void Start()
    {
        
    }

    protected virtual void Awake() {
        rb = GetComponent<Rigidbody2D>();
        player = playerController.Instance;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if(health <= 0) {
            Destroy(gameObject);
        }
        if(isRecoiling) {
            if(recoilTimer < recoilLength) {
                recoilTimer += Time.deltaTime;
            } else {
                isRecoiling = false;
                recoilTimer = 0;
            }
        }
    }

    public virtual void EnemyHit(float _damageDone, Vector2 _hitDirection, float _hitForce) {
        health -= _damageDone;
        if(!isRecoiling) {
            rb.AddForce(-_hitForce * recoilFactor * _hitDirection);
        }
    }

    protected void OnTriggerStay2D(Collider2D _other) {
        if(_other.gameObject.CompareTag("Player") && !playerController.Instance.pState.invincible) {
            Attack();
            playerController.Instance.HitStopTime(0, 5, 0.5f);
        }
    }

    protected virtual void Attack() {
        playerController.Instance.TakeDamage(damage);
    }
}
