using UnityEngine;

public class AttackCardManager : MonoBehaviour
{
    public CreatureCombat activeCreature;

    // Remember whether the attack card is currently visible
    bool attackCardVisible = false;

    public void SetActiveCreature(CreatureCombat creature)
    {
        activeCreature = creature;
        Debug.Log("Active creature set: " + creature.name);

        // If the attack card is already visible, enable attack immediately
        if (attackCardVisible)
        {
            activeCreature.SetCanAttack(true);
            Debug.Log("Attack card already visible -> enabled attack on newly registered creature");
        }
        else
        {
            activeCreature.SetCanAttack(false);
        }
    }

    public void OnAttackCardFound()
    {
        attackCardVisible = true;
        Debug.Log("Attack card found. Active creature = " + (activeCreature ? activeCreature.name : "NULL"));

        if (activeCreature != null)
            activeCreature.SetCanAttack(true);
    }

    public void OnAttackCardLost()
    {
        attackCardVisible = false;
        Debug.Log("Attack card lost");

        if (activeCreature != null)
            activeCreature.SetCanAttack(false);
    }
}
