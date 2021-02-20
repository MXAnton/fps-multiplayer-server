using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeController : MonoBehaviour
{
    public WeaponsController userWeaponsController;

    [Header("Fire")]
    public bool canFire = true;
    public float normalHitCooldown = 0.5f;
    public float hardHitCooldown = 1;
    [Space]
    public Collider hitCheckCollider;

    [Header("Hit")]
    public float hitDamage = 40;
    public float hardHitMultiplier = 2f;
    float currentHitDamageMultiplier = 1;
    public float normalHitForce = 2f;

    void Start()
    {
        hitCheckCollider.gameObject.SetActive(false);
        canFire = true;
    }

    public IEnumerator NormalHit()
    {
        if (canFire == false)
        {
            yield break;
        }

        canFire = false;
        currentHitDamageMultiplier = 1;

        hitCheckCollider.gameObject.SetActive(true);

        yield return new WaitForSeconds(normalHitCooldown);

        hitCheckCollider.gameObject.SetActive(false);
        canFire = true;
    }

    public IEnumerator HardHit()
    {
        if (canFire == false)
        {
            yield break;
        }

        canFire = false;
        currentHitDamageMultiplier = hardHitMultiplier;

        hitCheckCollider.gameObject.SetActive(true);

        yield return new WaitForSeconds(hardHitCooldown);

        hitCheckCollider.gameObject.SetActive(false);
        canFire = true;
    }


    public void OnHit(GameObject hitGameObject)
    {
        Debug.Log(hitGameObject.GetComponent<Rigidbody>());
        // Impact on hit object
        if (hitGameObject.GetComponent<Rigidbody>() != null && hitGameObject.GetComponent<Rigidbody>().isKinematic == false) // If the object has rigidbody
        {
            // Add force
            hitGameObject.GetComponent<Rigidbody>().AddForceAtPosition(transform.forward * normalHitForce * currentHitDamageMultiplier, hitGameObject.transform.position, ForceMode.Impulse);
        }
    }
}
