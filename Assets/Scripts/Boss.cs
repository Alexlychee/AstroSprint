using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Pool;

public class Boss : Enemy
{
    public static Boss Instance;

    [SerializeField] GameObject slashEffect;
    public Transform SideAttackTransform; //the middle of the side attack area
    public Vector2 SideAttackArea; //how large the area of side attack is

    public Transform UpAttackTransform; //the middle of the up attack area
    public Vector2 UpAttackArea; //how large the area of side attack is

    public Transform DownAttackTransform; //the middle of the down attack area
    public Vector2 DownAttackArea; //how large the area of down attack is

    public float attackRange;
    public float attackTimer;

    [HideInInspector] public bool facingRight;

    [Header("Ground Check Settings:")]
    [SerializeField] public Transform groundCheckPoint; //point at which ground check happens
    [SerializeField] private float groundCheckY = 0.2f; //how far down from ground chekc point is Grounded() checked
    [SerializeField] private float groundCheckX = 0.5f; //how far horizontally from ground chekc point to the edge of the player is
    [SerializeField] private LayerMask whatIsGround; //sets the ground layer

    int hitCounter;
    bool stunned, canStun;
    bool alive;

    [HideInInspector] public float runSpeed;

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
    }
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        ChangeState(EnemyStates.Boss_Stage1);
        alive = true;
    }
    public bool Grounded()
    {
        if (Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(SideAttackTransform.position, SideAttackArea);
        Gizmos.DrawWireCube(UpAttackTransform.position, UpAttackArea);
        Gizmos.DrawWireCube(DownAttackTransform.position, DownAttackArea);
    }

    float bloodCountdown;
    float bloodTimer;
    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        if (health <= 0 && alive)
        {
            Death(0);
        }
        if (!attacking)
        {
            attackCountdown -= Time.deltaTime;
        }
    }
    public void Flip()
    {
        if (playerController.Instance.transform.position.x < transform.position.x && transform.localScale.x > 0)
        {
            transform.eulerAngles = new Vector2(transform.eulerAngles.x, 180);
            facingRight = false;
        }
        else
        {
            transform.eulerAngles = new Vector2(transform.eulerAngles.x, 0);
            facingRight = true;
        }
    }
    protected override void UpdateEnemyStates()
    {
        if (playerController.Instance != null)
        {
            switch (GetCurrentEnemyState)
            {
                case EnemyStates.Boss_Stage1:
                    attackTimer = 6;
                    runSpeed = speed;
                    break;

                case EnemyStates.Boss_Stage2:
                    attackTimer = 5;
                    break;

                case EnemyStates.Boss_Stage3:
                    attackTimer = 8;
                    break;

                case EnemyStates.Boss_Stage4:
                    attackTimer = 10;
                    runSpeed = speed / 2;
                    break;
            }
        }
    }
    protected override void OnCollisionStay2D(Collision2D _other)
    {

    }
    #region attacking
    #region variables
    [HideInInspector] public bool attacking;
    [HideInInspector] public float attackCountdown;
    [HideInInspector] public bool damagedPlayer = false;

    [HideInInspector] public bool parrying;
    [HideInInspector] public Vector2 moveToPosition;
    [HideInInspector] public bool diveAttack;
    public GameObject divingCollider;
    public GameObject pillar;

    #endregion

    #region Control
    public void AttackHandler()
    {
        if (currentEnemyState == EnemyStates.Boss_Stage1)
        {
            if (Vector2.Distance(playerController.Instance.transform.position, rb.position) <= attackRange)
            {
                StartCoroutine(TripleSlash());
            }
            else
            {
                StartCoroutine(Lunge());
            }
        }
        if (currentEnemyState == EnemyStates.Boss_Stage2)
        {
            if (Vector2.Distance(playerController.Instance.transform.position, rb.position) <= attackRange)
            {
                StartCoroutine(TripleSlash());
            }
            else
            {
                DiveAttackJump();
            }
        }
    }

    public void ResetAllAttacks()
    {
        attacking = false;
        StopCoroutine(TripleSlash());
        StopCoroutine(Lunge());
        StopCoroutine(Parry());
        StopCoroutine(Slash());

        diveAttack = false;
    }
    #endregion
    #region Stage 1

    #region Stage 2
    void DiveAttackJump()
    {
        attacking = true;
        moveToPosition = new Vector2(playerController.Instance.transform.position.x, rb.position.y + 10);
        diveAttack = true;
        anim.SetBool("Jump", true);
    }

    public void Dive()
    {
        anim.SetBool("Dive", true);
        anim.SetBool("Jump", false);
    }
    private void OnTriggerEnter2D(Collider2D _other)
    {
        if (_other.GetComponent<playerController>() != null && diveAttack)
        {
            _other.GetComponent<playerController>().TakeDamage(damage * 2);
            playerController.Instance.pState.recoilingX = true;
        }
    }

    public void DivingPillars()
    {
        Vector2 _impactPoint = groundCheckPoint.position;
        float _spawnDistance = 5;

        for (int i = 0; i < 10; i++)
        {
            Vector2 _pillarSpawnPointRight = _impactPoint + new Vector2(_spawnDistance, 0);
            Vector2 _pillarSpawnPointLeft = _impactPoint - new Vector2(_spawnDistance, 0);
            Instantiate(pillar, _pillarSpawnPointRight, Quaternion.Euler(0, 0, -90));
            Instantiate(pillar, _pillarSpawnPointLeft, Quaternion.Euler(0, 0, -90));

            _spawnDistance += 5;
        }
        ResetAllAttacks();
    }

    #endregion
    IEnumerator TripleSlash()
    {
        attacking = true;
        rb.velocity = Vector2.zero;

        anim.SetTrigger("Slash");
        SlashAngle();
        yield return new WaitForSeconds(0.3f);
        anim.ResetTrigger("Slash");

        anim.SetTrigger("Slash");
        SlashAngle();
        yield return new WaitForSeconds(0.5f);
        anim.ResetTrigger("Slash");

        anim.SetTrigger("Slash");
        SlashAngle();
        yield return new WaitForSeconds(0.2f);
        anim.ResetTrigger("Slash");

        ResetAllAttacks();
    }
    void SlashAngle()
    {
        if (playerController.Instance.transform.position.x - transform.position.x != 0)
        {
            Instantiate(slashEffect, SideAttackTransform);
        }
        else if (playerController.Instance.transform.position.y > transform.position.y)
        {
            SlashEffectAtAngle(slashEffect, 80, UpAttackTransform);
        }
        else if (playerController.Instance.transform.position.y < transform.position.y)
        {
            SlashEffectAtAngle(slashEffect, -90, UpAttackTransform);
        }
    }

    void SlashEffectAtAngle(GameObject _slashEffect, int _effectAngle, Transform _attackTransform)
    {
        _slashEffect = Instantiate(_slashEffect, _attackTransform);
        _slashEffect.transform.eulerAngles = new Vector3(0, 0, _effectAngle);
        _slashEffect.transform.localScale = new Vector2(transform.localScale.x, transform.localScale.y);
    }
    IEnumerator Lunge()
    {
        Flip();
        attacking = true;
        anim.SetBool("Lunge", true);
        yield return new WaitForSeconds(1f);
        anim.SetBool("Lunge", false);
        damagedPlayer = false;
        ResetAllAttacks();

    }

    IEnumerator Parry()
    {
        attacking = true;
        rb.velocity = Vector2.zero;
        anim.SetBool("Parry", true);
        yield return new WaitForSeconds(0.8f);
        anim.SetBool("Parry", false);
        parrying = false;
        ResetAllAttacks();
    }

    IEnumerator Slash()
    {
        attacking = true;
        rb.velocity = Vector2.zero;

        anim.SetTrigger("Slash");
        SlashAngle();
        yield return new WaitForSeconds(0.2f);
        anim.ResetTrigger("Slash");
        ResetAllAttacks();
    }
    #endregion
    #endregion

    public override void EnemyHit(float _damageDone, Vector2 _hitDirection, float _hitForce)
    {
        if (!parrying)
        {
            base.EnemyHit(_damageDone, _hitDirection, _hitForce);
            if (currentEnemyState != EnemyStates.Boss_Stage4)
            {
                ResetAllAttacks();
                StartCoroutine(Parry());
            }
        }
        else
        {
            StopCoroutine(Parry());
            ResetAllAttacks();
            StartCoroutine(Slash());
        }
        if (health > 20)
        {
            ChangeState(EnemyStates.Boss_Stage1);
        }
        if (health <= 15)
        {
            ChangeState(EnemyStates.Boss_Stage2);
        }
        if (health <= 0)
        {
            Death(0);
        }
    }

    protected override void Death(float _destroyTime)
    {
        ResetAllAttacks();
        alive = false;
        rb.velocity = new Vector2(rb.velocity.x, -25);
        anim.SetTrigger("Die");
        bloodTimer = 0.8f;
    }
    public void DestroyAfterDeath()
    {
        Destroy(gameObject);
    }
}