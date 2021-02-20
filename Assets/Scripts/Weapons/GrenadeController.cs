using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeController : MonoBehaviour
{
    public WeaponsController weaponsController;

    public float maxExplodeTime = 1;
    float timer;

    public float explosionForce = 15;
    public float maxExplodeDamage = 50f;

    public float explosionRadius = 10f;

    public AudioClip explosionClip;
    public GameObject explosionEffect;

    void Start()
    {
        timer = maxExplodeTime;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            Explode();
        }
    }

    void Explode()
    {
        GameObject newExplosionEffect = Instantiate(explosionEffect, transform.position, Quaternion.identity);
        newExplosionEffect.GetComponent<AudioSource>().PlayOneShot(explosionClip);


        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider nearbyObject in colliders)
        {
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
            //if (nearbyObject.GetComponent<HealthComponent>() && nearbyObject.name == "Body")
            //{
            //    float hitDistanceFromExplosion = Vector3.Distance(nearbyObject.transform.position, transform.position);
            //    float hitDamage = maxExplodeDamage / (1 + (hitDistanceFromExplosion / explosionRadius));

            //    hitDamage = Mathf.Ceil(hitDamage);

            //    nearbyObject.GetComponent<HealthComponent>().Damage(hitDamage, weaponsController);
            //}
        }

        Destroy(gameObject);
    }
}
