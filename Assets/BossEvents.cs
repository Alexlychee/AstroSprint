using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossEvents : MonoBehaviour
{
    void SlashDamagePlayer()
    {
        if (playerController.Instance.transform.position.x - transform.position.x != 0)
        {
            Hit(Boss.Instance.SideAttackTransform, Boss.Instance.SideAttackArea);
        }
        else if (playerController.Instance.transform.position.y > transform.position.y)
        {
            Hit(Boss.Instance.UpAttackTransform, Boss.Instance.UpAttackArea);
        }
        else if (playerController.Instance.transform.position.y < transform.position.y)
        {
            Hit(Boss.Instance.DownAttackTransform, Boss.Instance.DownAttackArea);
        }
    }
    void Hit(Transform _attackTransform, Vector2 _attackArea)
    {
        Collider2D _objectsToHit = Physics2D.OverlapBox(_attackTransform.position, _attackArea, 0);

        if (_objectsToHit.GetComponent<playerController>() != null)
        {
            _objectsToHit.GetComponent<playerController>().TakeDamage(Boss.Instance.damage);
        }
    }

    void Parrying() {
        Boss.Instance.parrying = true;
    }
}
