using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeSpawner : MonoBehaviour
{
    public static Dictionary<int, GrenadeSpawner> spawners = new Dictionary<int, GrenadeSpawner>();
    private static int nextSpawnerId = 1;

    public int spawnerId;
    public bool hasGrenade = false;

    private void Start()
    {
        hasGrenade = false;
        spawnerId = nextSpawnerId;
        nextSpawnerId++;
        spawners.Add(spawnerId, this);

        StartCoroutine(SpawnItem());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasGrenade && other.CompareTag("Player"))
        {
            Player _player = other.GetComponent<Player>();
            if (_player.weaponsController.grenadeCount < _player.weaponsController.maxGrenadeCount)
            {
                _player.weaponsController.grenadeCount++;
                GrenadePickedUp(_player.id);
            }
        }
    }

    private IEnumerator SpawnItem()
    {
        yield return new WaitForSeconds(10f);

        hasGrenade = true;
        ServerSend.GrenadeSpawned(spawnerId);
    }

    private void GrenadePickedUp(int _byPlayer)
    {
        hasGrenade = false;
        ServerSend.GrenadePickedUp(spawnerId, _byPlayer);

        StartCoroutine(SpawnItem());
    }
}
