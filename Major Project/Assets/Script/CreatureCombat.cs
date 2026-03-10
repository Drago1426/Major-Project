using System.Collections;
using UnityEngine;

public class CreatureCombat : MonoBehaviour
{
    [Header("References")]
    public Transform muzzle;
    public GameObject glowRing;

    [Header("Projectile")]
    public GameObject fireballPrefab;
    public float fireballSpeed = 2.0f;
    public float fireballLife = 2.0f;

    [Header("Attack State")]
    public bool canAttack = false;
    public float attackCooldown = 1.0f;

    bool _onCooldown;

    void Start()
    {
        SetGlow(false);
    }

    public void SetCanAttack(bool value)
    {
        canAttack = value;
        SetGlow(canAttack && !_onCooldown);
    }

    void SetGlow(bool on)
    {
        if (glowRing != null) glowRing.SetActive(on);
    }

    public void TryAttack()
    {
        if (!canAttack || _onCooldown) return;
        if (muzzle == null || fireballPrefab == null) return;

        GameObject fb = Instantiate(fireballPrefab, muzzle.position, muzzle.rotation);

        var rb = fb.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = muzzle.forward * fireballSpeed;
        }

        Destroy(fb, fireballLife);

        StartCoroutine(Cooldown());
    }

    IEnumerator Cooldown()
    {
        _onCooldown = true;
        SetGlow(false);
        yield return new WaitForSeconds(attackCooldown);
        _onCooldown = false;
        SetGlow(canAttack);
    }
}
