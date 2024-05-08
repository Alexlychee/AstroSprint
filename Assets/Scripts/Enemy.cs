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
    [SerializeField] protected GameObject enemyBlood;

    [SerializeField] AudioClip hurtSound;

    protected float recoilTimer;
    protected Rigidbody2D rb;
    protected SpriteRenderer sr;
    protected Animator anim;
    protected AudioSource audioSource;

    protected enum EnemyStates {
        // Crawler
        Crawler_Idle,
        Crawler_Flip,

        // Bat
        Bat_Idle,
        Bat_Chase,
        Bat_Stunned,
        Bat_Death,

        // Charger
        Charger_Idle,
        Charger_Surprised,
        Charger_Charge
    }

    protected EnemyStates currentEnemyState;

    protected virtual EnemyStates GetCurrentEnemyState {
        get { return currentEnemyState; }
        set {
            if(currentEnemyState != value) {
                currentEnemyState = value;
                ChangeCurrentAnimation();
            }
        }
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    protected virtual void Awake() {
        rb = GetComponent<Rigidbody2D>();
        player = playerController.Instance;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if(GameManager.Instance.gameIsPaused) return;

        if(isRecoiling) {
            if(recoilTimer < recoilLength) {
                recoilTimer += Time.deltaTime;
            } else {
                isRecoiling = false;
                recoilTimer = 0;
            }
        } else {
            UpdateEnemyStates();
        }
    }

    public virtual void EnemyHit(float _damageDone, Vector2 _hitDirection, float _hitForce) {
        health -= _damageDone;
        if(!isRecoiling) {
            audioSource.PlayOneShot(hurtSound);
            GameObject _enemyBlood = Instantiate(enemyBlood, transform.position, Quaternion.identity);
            Destroy(_enemyBlood, 5.5f);
            rb.velocity = -_hitForce * recoilFactor * _hitDirection;
        }
    }

    protected void OnCollisionStay2D(Collision2D _other) {
        if(_other.gameObject.CompareTag("Player") && !playerController.Instance.pState.invincible && health > 0) {
            Attack();
            if (playerController.Instance.pState.alive) {
                playerController.Instance.HitStopTime(0, 5, 0.5f);
            }
        }
    }

    protected virtual void Death(float _destroyTime) {
        Destroy(gameObject, _destroyTime);
    }

    protected virtual void UpdateEnemyStates() { }

    protected virtual void ChangeCurrentAnimation() { }

    protected void ChangeState(EnemyStates _newState) {
        GetCurrentEnemyState = _newState;
    }

    protected virtual void Attack() {
        playerController.Instance.TakeDamage(damage);
    }
}
