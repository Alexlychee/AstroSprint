using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Bat : Enemy
{
    [SerializeField] private float chaseDistance;
    [SerializeField] private float stunDuration;
    float timer;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        ChangeState(EnemyStates.Bat_Idle);
    }

    protected override void Update()
    {
        base.Update();
        if (!playerController.Instance.pState.alive) {
            ChangeState(EnemyStates.Bat_Idle);
        }
    }

    protected override void UpdateEnemyStates()
    {
        float _dist = Vector2.Distance(transform.position, playerController.Instance.transform.position);

        switch(GetCurrentEnemyState) 
        {
            case EnemyStates.Bat_Idle:
                if(_dist < chaseDistance) {
                    ChangeState(EnemyStates.Bat_Chase);
                }
                break;

            case EnemyStates.Bat_Chase:
                rb.MovePosition(Vector2.MoveTowards(transform.position, playerController.Instance.transform.position, Time.deltaTime * speed));
                FlipBat();
                break;

            case EnemyStates.Bat_Stunned:
                timer += Time.deltaTime;
                if(timer > stunDuration) {
                    ChangeState(EnemyStates.Bat_Idle);
                    timer = 0;
                }
                break;

            case EnemyStates.Bat_Death:
                Death(Random.Range(5, 10));
                break;
            
        }
    }

    public override void EnemyHit(float _damageDone, Vector2 _hitDirection, float _hitForce)
    {
        base.EnemyHit(_damageDone, _hitDirection, _hitForce);
        if(health > 0) {
            ChangeState(EnemyStates.Bat_Stunned);
        } else {
            ChangeState(EnemyStates.Bat_Death);
        }
    }

    protected override void Death(float _destroyTime)
    {
        rb.gravityScale = 12;
        base.Death(_destroyTime);
    }

    protected override void ChangeCurrentAnimation()
    {
        anim.SetBool("Idle", GetCurrentEnemyState == EnemyStates.Bat_Idle);
        anim.SetBool("Chase", GetCurrentEnemyState == EnemyStates.Bat_Chase);
        anim.SetBool("Stunned", GetCurrentEnemyState == EnemyStates.Bat_Stunned);
        if(GetCurrentEnemyState == EnemyStates.Bat_Death) {
            anim.SetTrigger("Death");
            int LayerIgnoreRaycast = LayerMask.NameToLayer("Ignore Player");
            gameObject.layer = LayerIgnoreRaycast;
        }
    }

    void FlipBat() {
        sr.flipX = playerController.Instance.transform.position.x < transform.position.x;
    }
}
