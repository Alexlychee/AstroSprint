using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Mana Settings")]
    [SerializeField] Image manaStorage;
    [SerializeField] float mana;
    [SerializeField] float manaDrainSpeed;
    [SerializeField] float manaGain;
    [Space(5)]

    [Header("Spell Settings")]
    // Spell Stats
    [SerializeField] float manaSpellCost = 0.3f;
    [SerializeField] float timeBetweenCast = 0.5f;
    [SerializeField] float spellDamage; // Up Spell Explosion and Down Spell Fireball
    [SerializeField] float downSpellForce; // Dive Spell

    // Spell Cast Objects
    [SerializeField] GameObject sideSpellFireball;
    [SerializeField] GameObject upSpellExplosion;
    [SerializeField] GameObject downSpellFireball;
    float timeSinceCast;
    float castOrHealtimer;
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
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        pState = GetComponent<PlayerStateList>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        gravity = rb.gravityScale;
        Mana = mana;
        manaStorage.fillAmount = Mana;

        Health = maxHealth;
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
        if(pState.cutscene) return;
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
        CastSpell();
    }

    private void OnTriggerEnter2D(Collider2D _other) // For up and down cast spell
    {
        if(_other.GetComponent<Enemy>() != null && pState.casting)
        {
            _other.GetComponent<Enemy>().EnemyHit(spellDamage, (_other.transform.position - transform.position).normalized, -recoilYSpeed);
        }
    }

    private void FixedUpdate() {
        if(pState.cutscene) return;
        if (pState.dashing) return;
        Recoil();
    }

    void GetInputs() {
        xAxis = Input.GetAxisRaw("Horizontal");
        yAxis = Input.GetAxisRaw("Vertical");
        attack = Input.GetButtonDown("Attack");

        if (Input.GetButton("Cast/Heal")) {
            castOrHealtimer += Time.deltaTime;
        } 
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
        int _dir = pState.lookingRight ? 1 : -1;
        rb.velocity = new Vector2(_dir * dashSpeed, 0);
        if (Grounded()) Instantiate(dashEffect, transform);
        yield return new WaitForSeconds(dashTime);
        rb.gravityScale = gravity;
        pState.dashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    public IEnumerator WalkIntoNewScene(Vector2 _exitDir, float _delay) {
        // IF exit is upwards
        if(_exitDir.y > 0) {
            rb.velocity = jumpForce * _exitDir;
        }
        
        // IF exit direction required horizontal movement
        if(_exitDir.x != 0) {
            xAxis = _exitDir.x > 0 ? 1 : -1;
            Move();
        }
        yield return new WaitForSeconds(_delay);
        pState.cutscene = false;
    }

    void Attack() {
        timeSinceAttack += Time.deltaTime;
        if (attack && timeSinceAttack >= timeBetweenAttack) {
            timeSinceAttack = 0;
            anim.SetTrigger("Attacking");

            if (yAxis == 0 || yAxis < 0 && Grounded()) {
                int _recoilLeftOrRight = pState.lookingRight ? 1 : -1;
                Hit(SideAttackTransform, SideAttackArea, ref pState.recoilingX, Vector2.right * _recoilLeftOrRight, recoilXSpeed);
                Instantiate(slashEffect, SideAttackTransform);
            } else if (yAxis > 0) {
                Hit(UpAttackTransform, UpAttackArea, ref pState.recoilingY, Vector2.up, recoilYSpeed);
                SlashEffectAtAngle(slashEffect, 80, UpAttackTransform);
            } else if (yAxis < 0 && !Grounded()) {
                Hit(DownAttackTransform, DownAttackArea, ref pState.recoilingY, Vector2.down, recoilYSpeed);
                SlashEffectAtAngle(slashEffect, -90, DownAttackTransform);
            }
        }
    }

    void Hit(Transform _attackTransform, Vector2 _attackArea, ref bool _recoilBool, Vector2 _recoilDir, float _recoilStrength) {
        Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0, attackableLayer);

        if (objectsToHit.Length > 0) {
            _recoilBool = true;
        }
        for (int i = 0; i < objectsToHit.Length; i++) {
            if (objectsToHit[i].GetComponent<Enemy>() != null) {
                objectsToHit[i].GetComponent<Enemy>().EnemyHit(damage, _recoilDir, _recoilStrength);

                if (objectsToHit[i].CompareTag("Enemy")) {
                    Mana += manaGain;
                }
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
        if (Input.GetButton("Cast/Heal") && castOrHealtimer > 0.05f && Health < maxHealth && Mana > 0 && !pState.jumping && !pState.dashing) {
            pState.healing = true;
            anim.SetBool("Healing", true);

            // Healing
            healTimer += Time.deltaTime;
            if (healTimer >= timeToHeal) {
                Health++;
                healTimer = 0;
            }

            // Drain mana
            Mana -= Time.deltaTime * manaDrainSpeed;
        } else {
            pState.healing = false;
            anim.SetBool("Healing", false);
            healTimer = 0;
        }
    }

    float Mana {
        get { return mana; }
        set {
            if(mana != value) {
                mana = Mathf.Clamp(value, 0, 1);
                manaStorage.fillAmount = Mana;
            }
        }
    }

    void CastSpell() {
        if(Input.GetButtonUp("Cast/Heal") && castOrHealtimer <= 0.05f && timeSinceCast >= timeBetweenCast && Mana >= manaSpellCost) {
            pState.casting = true;
            timeSinceCast = 0;
            StartCoroutine(CastCoroutine());
        } else {
            timeSinceCast += Time.deltaTime;
        }

        if(!Input.GetButton("Cast/Heal")) {
            castOrHealtimer = 0;
        }

        if(Grounded())
        {
            // Disable down spell if on the ground
            downSpellFireball.SetActive(false);
        }
        // If down spell is active, force player down until grounded
        if(downSpellFireball.activeInHierarchy)
        {
            rb.velocity += downSpellForce * Vector2.down;
        }
    }

    IEnumerator CastCoroutine() {
        anim.SetBool("Casting", true);
        yield return new WaitForSeconds(0.15f);

        // Side Spell Cast
       if (yAxis == 0 || (yAxis < 0 && Grounded()))
        {
            GameObject _fireBall = Instantiate(sideSpellFireball, SideAttackTransform.position, Quaternion.identity);

            // Flip Fireball
            if(pState.lookingRight)
            {
                _fireBall.transform.eulerAngles = Vector3.zero; // if facing right, fireball continues as per normal
            }
            else
            {
                _fireBall.transform.eulerAngles = new Vector2(_fireBall.transform.eulerAngles.x, 180); 
                // if not facing right, rotate the fireball 180 deg
            }
            pState.recoilingX = true;
        }
        // Up Spell Cast
        else if( yAxis > 0)
        {
            Instantiate(upSpellExplosion, transform);
            rb.velocity = Vector2.zero;
        }

        // Down  Spell Cast
        else if(yAxis < 0 && !Grounded())
        {
            downSpellFireball.SetActive(true);
        }

        Mana -= manaSpellCost;
        yield return new WaitForSeconds(0.35f);
        anim.SetBool("Casting", false);
        pState.casting = false;
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
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0 && !pState.jumping) {
            rb.velocity = new Vector3(rb.velocity.x, jumpForce);
            pState.jumping = true;
        }
        
        if(!Grounded() && airJumpCounter < maxAirJumps && Input.GetButtonDown("Jump")) {
            pState.jumping = true;
            airJumpCounter++;
            rb.velocity = new Vector3(rb.velocity.x, jumpForce);
        }

        if(Input.GetButtonUp("Jump") && rb.velocity.y > 3) {
            rb.velocity = new Vector2(rb.velocity.x, 0);
            pState.jumping = false;
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
