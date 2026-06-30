using System.Collections;
using UnityEngine;

public class CreatureCombat : MonoBehaviour
{
    [Header("References")]
    public Transform muzzle;
    public GameObject glowRing;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 2.0f;
    public float projectileLife = 2.0f;

    [Header("Attack State")]
    public bool canAttack = false;
    public float attackCooldown = 1.0f;

    bool onCooldown;

    void Start()
    {
        SetGlow(false);
    }

    public void SetCanAttack(bool value)
    {
        canAttack = value;
        SetGlow(canAttack && !onCooldown);
    }

    public void TryAttack()
    {
        if (!canAttack || onCooldown)
            return;

        if (muzzle != null && projectilePrefab != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
            var body = projectile.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.useGravity = false;
                body.linearVelocity = muzzle.forward * projectileSpeed;
            }

            Destroy(projectile, projectileLife);
        }

        StartCoroutine(Cooldown());
    }

    void SetGlow(bool on)
    {
        if (glowRing != null)
            glowRing.SetActive(on);
    }

    IEnumerator Cooldown()
    {
        onCooldown = true;
        SetGlow(false);
        yield return new WaitForSeconds(attackCooldown);
        onCooldown = false;
        SetGlow(canAttack);
    }
}
