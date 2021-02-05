using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public static Dictionary<int, Projectile> projectiles = new Dictionary<int, Projectile>();
    private static int nextProjectileId = 1;

    public int id;
    public Rigidbody rigidBody;
    public int thrownByPlayer;
    public Vector3 initialForce;
    public float explosionRadius = 1.5f;
    public float explosionDamage = 75f;
    public float explodeDelay = 0.2f;

    private void Start()
    {
        id = nextProjectileId;
        nextProjectileId++;
        projectiles.Add(id, this);

        ServerSend.SpawnProjectile(this, thrownByPlayer);

        rigidBody.AddForce(initialForce);
        StartCoroutine(ExplodeAfterTime());
    }

    private void FixedUpdate()
    {
        ServerSend.ProjectilePosition(this);
    }

    private void OnCollisionEnter(Collision collision)
    {
        StartCoroutine(Explode());
    }

    public void Initialize(Vector3 _initialMovementDirection, float _initialForceStrength, int _thrownByPlayer)
    {
        initialForce = _initialMovementDirection * _initialForceStrength;
        thrownByPlayer = _thrownByPlayer;
    }

    private IEnumerator Explode()
    {
        yield return new WaitForSeconds(explodeDelay);

        ServerSend.ProjectileExploded(this);

        Collider[] _colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider _collider in _colliders)
        {
            float _hitDistance = Vector3.Distance(transform.position, _collider.transform.position);
            float _damageToDeal = explosionRadius / _hitDistance;
            _damageToDeal /= explosionRadius;
            _damageToDeal *= explosionDamage;
            _damageToDeal = Mathf.Round(_damageToDeal);

            if (_collider.CompareTag("Player"))
            {
                _collider.GetComponent<Player>().TakeDamage(_damageToDeal);
                ServerSend.PlayerHitInfo(thrownByPlayer, _collider.transform.position, _damageToDeal);

                if (_collider.GetComponent<Player>().health <= 0)
                {
                    _collider.GetComponent<Player>().kills++;
                    ServerSend.PlayerKilled(Server.clients[thrownByPlayer].player.username, _collider.GetComponent<Player>().username);
                    ServerSend.PlayerDeathsAndKills(Server.clients[thrownByPlayer].player);
                }
            }
            else if (_collider.CompareTag("Enemy"))
            {
                _collider.GetComponent<Enemy>().TakeDamage(_damageToDeal);
                ServerSend.PlayerHitInfo(thrownByPlayer, _collider.transform.position, _damageToDeal);

                if (_collider.GetComponent<Enemy>().health <= 0)
                {
                    _collider.GetComponent<Player>().kills++;
                    ServerSend.PlayerKilled(Server.clients[thrownByPlayer].player.username, "Bot");
                    ServerSend.PlayerDeathsAndKills(Server.clients[thrownByPlayer].player);
                }
            }
        }

        projectiles.Remove(id);
        Destroy(gameObject);
    }

    private IEnumerator ExplodeAfterTime()
    {
        yield return new WaitForSeconds(10);

        Explode();
    }
}
