using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerController : MonoBehaviour
{
    [Header("Horizontal Movement Settings")]
    [SerializeField] private float walkSpeed = 1;
    [Space(5)]

    [Header("Vertical Movement Settings")]
    [SerializeField] private float jumpForce = 45;
    private int jumpBufferCounter = 0;
    [SerializeField] private int jumpBufferFrames;
    private float coyoteTimeCounter = 0;
    [SerializeField] private float coyoteTime;
    private int airJumpCounter = 0;
    [SerializeField] private int maxAirJumps;
    private float gravity;
    [Space(5)]

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask whatIsGround;
    [Space(5)]

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;
    [SerializeField] GameObject dashEffect;
    private bool canDash = true;
    private bool dashed;
    [Space(5)]

    [Header("Attack Settings")]
    bool attack = false;
    float timeBetweenAttack, timeSinceAttack;
    [SerializeField] Transform SideAttackTransform, UpAttackTransform, DownAttackTransform;
    [SerializeField] Vector2 SideAttackArea, UpAttackArea, DownAttackArea;
    [SerializeField] LayerMask attackableLayer;
    [SerializeField] float damage;
    [SerializeField] GameObject slashEffect;

    bool restoreTime;
    float restoreTimeSpeed;
    [Space(5)]

    [Header("Recoil Settings")]
    [SerializeField] int recoilXSteps = 5;
    [SerializeField] int recoilYSteps = 5;
    [SerializeField] float recoilXSpeed = 100;
    [SerializeField] float recoilYSpeed = 100;
    private int stepsXRecoiled, stepsYRecoiled;
    [Space(5)]

    [Header("Health Settings")]
    public int health;
    public int maxHealth;
    [SerializeField] GameObject bloodSpurt;
    [SerializeField] float hitFlashSpeed;
    public delegate void OnHealthChangedDelegate();
    [HideInInspector] public OnHealthChangedDelegate onHealthChangedCallback;
    float healTimer;
    [SerializeField] float timeToHeal;
    [Space(5)]

    [HideInInspector]public PlayerStateList pState; 
    private Rigidbody2D rb;
    Animator anim;
    private SpriteRenderer sr;

    private float xAxis, yAxis;

    public static playerController Instance;

    private void Awake() {
        if(Instance != null && Instance != this) {
            Destroy(gameObject);
        }
        else {
            Instance = this;
        }
        Health = maxHealth;
    }

    // Start is called before the first frame update
    void Start()
    {
        pState = GetComponent<PlayerStateList>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        gravity = rb.gravityScale;
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(SideAttackTransform.position, SideAttackArea);
        Gizmos.DrawWireCube(UpAttackTransform.position, UpAttackArea);
        Gizmos.DrawWireCube(DownAttackTransform.position, DownAttackArea);

    }

    // Update is called once per frame
    void Update()
    {
        GetInputs();
        UpdateJumpVariables();
        RestoreTimeScale();
        if (pState.dashing) return;
        FlashWhileInvincible();
        Move();
        Heal();
        if (pState.healing) return;
        Flip();
        Jump();
        StartDash();
        Attack();
    }

    private void FixedUpdate() {
        if (pState.dashing) return;
        Recoil();
    }

    void GetInputs() {
        xAxis = Input.GetAxisRaw("Horizontal");
        yAxis = Input.GetAxisRaw("Vertical");
        attack = Input.GetButtonDown("Attack");
    }

    void Flip() {
        if(xAxis < 0) {
            transform.localScale = new Vector2(-1, transform.localScale.y);
            pState.lookingRight = false;
        }
        else if(xAxis > 0) {
            transform.localScale = new Vector2(1, transform.localScale.y);
            pState.lookingRight = true;
        }
    }

    private void Move() {
        rb.velocity = new Vector2(walkSpeed * xAxis, rb.velocity.y);
        anim.SetBool("Walking", rb.velocity.x != 0 && Grounded());
    }

    void StartDash() {
        if(Input.GetButtonDown("Dash") && canDash && !dashed) {
            StartCoroutine(Dash());
            dashed = true;
        }

        if (Grounded()) {
            dashed = false;
        }
    }

    IEnumerator Dash() {
        canDash = false;
        pState.dashing = true;
        anim.SetTrigger("Dashing");
        rb.gravityScale = 0;
        rb.velocity = new Vector2(transform.localScale.x * dashSpeed, 0);
        if (Grounded()) Instantiate(dashEffect, transform);
        yield return new WaitForSeconds(dashTime);
        rb.gravityScale = gravity;
        pState.dashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    void Attack() {
        timeSinceAttack += Time.deltaTime;
        if (attack && timeSinceAttack >= timeBetweenAttack) {
            timeSinceAttack = 0;
            anim.SetTrigger("Attacking");

            if (yAxis == 0 || yAxis < 0 && Grounded()) {
                Hit(SideAttackTransform, SideAttackArea, ref pState.recoilingX, recoilXSpeed);
                Instantiate(slashEffect, SideAttackTransform);
            } else if (yAxis > 0) {
                Hit(UpAttackTransform, UpAttackArea, ref pState.recoilingY, recoilYSpeed);
                SlashEffectAtAngle(slashEffect, 80, UpAttackTransform);
            } else if (yAxis < 0 && !Grounded()) {
                Hit(DownAttackTransform, DownAttackArea, ref pState.recoilingY, recoilYSpeed);
                SlashEffectAtAngle(slashEffect, -90, DownAttackTransform);
            }
        }
    }

    void Hit(Transform _attackTransform, Vector2 _attackArea, ref bool _recoilDir, float _recoilStrength) {
        Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0, attackableLayer);

        if (objectsToHit.Length > 0) {
            _recoilDir = true;
        }
        for (int i = 0; i < objectsToHit.Length; i++) {
            if (objectsToHit[i].GetComponent<Enemy>() != null) {
                objectsToHit[i].GetComponent<Enemy>().EnemyHit
                    (damage, (transform.position - objectsToHit[i].transform.position).normalized, _recoilStrength);
            }
        }
    }

    void SlashEffectAtAngle(GameObject _slashEffect, int _effectAngle, Transform _attackTransform) {
        _slashEffect = Instantiate(_slashEffect, _attackTransform);
        _slashEffect.transform.eulerAngles = new Vector3(0, 0, _effectAngle);
        _slashEffect.transform.localScale = new Vector2(transform.localScale.x, transform.localScale.y);
    }

    void Recoil() {
        if (pState.recoilingX) {
            if (pState.lookingRight) {
                rb.velocity = new Vector2(-recoilXSpeed, 0);
            }
            else {
                rb.velocity = new Vector2(recoilXSpeed, 0);
            }
        }

        if (pState.recoilingY) {
            rb.gravityScale = 0;
            if (yAxis < 0) {
                rb.velocity = new Vector2(rb.velocity.x, recoilYSpeed);
            }
            else {
                rb.velocity = new Vector2(rb.velocity.x, -recoilYSpeed);
            }
            airJumpCounter = 0;
        }
        else {
            rb.gravityScale = gravity;
        }

        // Stop Recoil
        if (pState.recoilingX && stepsXRecoiled < recoilXSteps) {
            stepsXRecoiled++;
        }
        else {
            StopRecoilX();
        }
        if (pState.recoilingY && stepsYRecoiled < recoilYSteps) {
            stepsYRecoiled++;
        }
        else {
            StopRecoilY();
        }

        if (Grounded()) {
            StopRecoilY();
        }
    }
    void StopRecoilX() {
        stepsXRecoiled = 0;
        pState.recoilingX = false;
    }
    void StopRecoilY() {
        stepsYRecoiled = 0;
        pState.recoilingY = false;
    }

    public void TakeDamage(float _damage) {
        Health -= Mathf.RoundToInt(_damage);
        StartCoroutine(StopTakingDamage());
    }

    IEnumerator StopTakingDamage() {
        pState.invincible = true;
        GameObject _bloodSpurtParticles = Instantiate(bloodSpurt, transform.position, Quaternion.identity);
        Destroy(_bloodSpurtParticles, 1.5f);
        anim.SetTrigger("TakeDamage");
        yield return new WaitForSeconds(1f);
        pState.invincible = false;
    }

    void FlashWhileInvincible() {
        sr.material.color = pState.invincible ?
            Color.Lerp(Color.white, Color.black, Mathf.PingPong(Time.time * hitFlashSpeed, 1.0f)) :
            Color.white;
    }

    void RestoreTimeScale() {
        if(restoreTime) {
            if(Time.timeScale < 1) {
                Time.timeScale += Time.deltaTime * restoreTimeSpeed;
            }
            else {
                Time.timeScale = 1;
                restoreTime = false;
            }
        }
    }

    public void HitStopTime(float _newTimeScale, int _restoreSpeed, float _delay) {
        restoreTimeSpeed = _restoreSpeed;
        Time.timeScale = _newTimeScale;
        if (_delay > 0) {
            StopCoroutine(StartTimeAgain(_delay));
            StartCoroutine(StartTimeAgain(_delay));
        }
        else {
            restoreTime = true;
        }
    }

    IEnumerator StartTimeAgain(float _delay) {
        restoreTime = true;
        yield return new WaitForSeconds(_delay);
    }

    public int Health {
        get { return health; }
        set {
            if(health != value) {
                health = Mathf.Clamp(value, 0, maxHealth);
                if(onHealthChangedCallback != null) {
                    onHealthChangedCallback.Invoke();
                }
            }
        }

    }

    void Heal() {
        if (Input.GetButton("Healing") && Health < maxHealth && !pState.jumping && !pState.dashing) {
            pState.healing = true;
            anim.SetBool("Healing", true);

            // Healing
            healTimer += Time.deltaTime;
            if (healTimer >= timeToHeal) {
                Health++;
                healTimer = 0;
            }
        } else {
            pState.healing = false;
            anim.SetBool("Healing", false);
            healTimer = 0;
        }
    }

    public bool Grounded() {
        if(Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0 ,0), Vector2.down, groundCheckY, whatIsGround)) {
            return true;
        } else {
            return false;
        }
    }

    void Jump() {
        if(Input.GetButtonUp("Jump") && rb.velocity.y > 0) {
            rb.velocity = new Vector2(rb.velocity.x, 0);
            pState.jumping = false;
        }

        if(!pState.jumping) {
            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0) {
                rb.velocity = new Vector3(rb.velocity.x, jumpForce);
                pState.jumping = true;
            } else if(!Grounded() && airJumpCounter < maxAirJumps && Input.GetButtonDown("Jump")) {
                pState.jumping = true;
                airJumpCounter++;
                rb.velocity = new Vector3(rb.velocity.x, jumpForce);
            }
        }

        anim.SetBool("Jumping", !Grounded());
    }

    void UpdateJumpVariables() {
        if(Grounded()) {
            pState.jumping = false;
            coyoteTimeCounter = coyoteTime;
            airJumpCounter = 0;
        } else {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump")) {
            jumpBufferCounter = jumpBufferFrames;
        } else {
            jumpBufferCounter--;
        }
    }
}
