using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Weapon : MonoBehaviour
{
    public enum FireModes { Semi, Burst, Auto };

    public static Dictionary<int, Weapon> weapons = new Dictionary<int, Weapon>();
    private static int nextWeaponId = 1;
    public int id;

    public int whichWeapon;
    public int weaponType;

    public WeaponsController userWeaponsController;

    [Header("FireMode")]
    public int currentFireModeState;
    public FireModes currentFireMode;
    public FireModes[] enabledFireModes;
    public float semiFireRate = 0.5f;
    public float burstFireRate = 0.1f;
    public float autoFireRate = 0.2f;

    [Header("Fire")]
    public bool canFire = false;
    public float fireSpread = 0.2f;
    public float shootDistance = 100f;

    [Header("Hit")]
    public float hitDamage = 30;
    public float hitForce = 2f;

    [Header("Ammo")]
    public int currentClipAmmo;
    public int maxClipAmmo = 30;
    [Space]
    public int currentExtraAmmo;
    public int maxExtraAmmo = 120;
    [Space]
    public float reloadTime = 1;
    public bool reloading = false;

    private void Start()
    {
        currentClipAmmo = maxClipAmmo;
        currentExtraAmmo = maxExtraAmmo;

        currentFireModeState = 0;
        currentFireMode = enabledFireModes[currentFireModeState];


        id = nextWeaponId;
        nextWeaponId++;
        weapons.Add(id, this);

        ServerSend.SpawnWeapon(this);

        this.enabled = false;
    }

    private void Awake()
    {
        Debug.Log("Weaponscript activated");
        canFire = true;
        reloading = false;
    }

    private void Update()
    {
        //if (currentFireMode == FireModes.Auto && Input.GetKey(KeyCode.Mouse0))
        //{
        //    Fire();
        //}
        //else if (Input.GetKeyDown(KeyCode.Mouse0))
        //{
        //    Fire();
        //}

        //if (Input.GetKeyDown(KeyCode.R))
        //{
        //    Reload();
        //}

        //if (Input.GetKeyDown(KeyCode.V) && enabledFireModes.Length > 1)
        //{
        //    ChangeFireMode();
        //}
    }

    public void Fire(Vector3 _playerPosition, Vector3 _viewDirection, int _fireModeInt)
    {
        SetFireMode(_fireModeInt);

        if (canFire == true)
        {
            Vector3 _fireOrigin = _playerPosition + userWeaponsController.shootOrigin;
            //Debug.Log("playerpos: " + _playerPosition);
            //Debug.Log("shoot origin before: " + userWeaponsController.shootOrigin);

            switch (currentFireMode)
            {
                case FireModes.Semi:
                    StartCoroutine(SemiShoot(_fireOrigin, _viewDirection));
                    break;
                case FireModes.Burst:
                    StartCoroutine(BurstShoot(_fireOrigin, _viewDirection));
                    break;
                case FireModes.Auto:
                    StartCoroutine(AutoShoot(_fireOrigin, _viewDirection));
                    break;
            }
        }
    }

    IEnumerator SemiShoot(Vector3 _fireOrigin, Vector3 _viewDirection)
    {
        canFire = false;
        FireBullet(_fireOrigin, _viewDirection);

        yield return new WaitForSeconds(semiFireRate);

        canFire = true;
    }
    IEnumerator BurstShoot(Vector3 _fireOrigin, Vector3 _viewDirection)
    {
        canFire = false;

        FireBullet(_fireOrigin, _viewDirection);
        yield return new WaitForSeconds(burstFireRate);

        if (currentClipAmmo > 0)
        {
            FireBullet(_fireOrigin, _viewDirection);
            yield return new WaitForSeconds(burstFireRate);
        }
        if (currentClipAmmo > 0)
        {
            FireBullet(_fireOrigin, _viewDirection);
        }

        yield return new WaitForSeconds(semiFireRate);

        canFire = true;
    }
    IEnumerator AutoShoot(Vector3 _fireOrigin, Vector3 _viewDirection)
    {
        canFire = false;
        FireBullet(_fireOrigin, _viewDirection);

        yield return new WaitForSeconds(autoFireRate);

        canFire = true;
    }

    void FireBullet(Vector3 _fireOrigin, Vector3 _viewDirection)
    {
        if (reloading == true)
        {
            // Can't fire while reloading
            return;
        }
        if (currentClipAmmo <= 0)
        {
            return;
        }
        if (userWeaponsController == null)
        {
            return;
        }

        currentClipAmmo--;

        // Add firespread
        Vector3 _fireDirection = _viewDirection;
        //_fireDirection.x += UnityEngine.Random.Range(-fireSpread, fireSpread);
        //_fireDirection.y += UnityEngine.Random.Range(-fireSpread, fireSpread);
        //_fireDirection.z += UnityEngine.Random.Range(-fireSpread, fireSpread);

        ServerSend.PlayerShot(userWeaponsController.player, _fireOrigin, _fireDirection, id, currentClipAmmo, currentExtraAmmo);

        // Define ray
        Ray fireRay = new Ray(_fireOrigin, _fireDirection);
        Physics.Raycast(fireRay, out RaycastHit _hit, shootDistance);
        
        // If hit
        if (_hit.collider != null)
        {
            // Impact on hit object
            if (_hit.rigidbody != null && _hit.rigidbody.isKinematic == false) // If the object has rigidbody
            {
                // Add force
                _hit.rigidbody.AddForceAtPosition(transform.forward * hitForce, _hit.point, ForceMode.Impulse);
            }

            if (_hit.collider.CompareTag("Player"))
            {
                // If hit own player
                if (_hit.collider.GetComponent<Player>() == userWeaponsController.player)
                {
                    return;
                }
                // If the player hit is dead
                if (_hit.collider.GetComponent<Player>().health <= 0)
                {
                    return;
                }

                _hit.collider.GetComponent<Player>().TakeDamage(hitDamage);
                ServerSend.PlayerHitInfo(userWeaponsController.player.id, _hit.point, hitDamage);

                if (_hit.collider.GetComponent<Player>().health <= 0)
                {
                    userWeaponsController.player.kills++;
                    ServerSend.PlayerKilled(userWeaponsController.player.username, _hit.collider.GetComponent<Player>().username);
                    ServerSend.PlayerDeathsAndKills(userWeaponsController.player);
                }
            }
            else if (_hit.collider.CompareTag("Enemy"))
            {
                if (_hit.collider.GetComponent<Enemy>().health <= 0)
                {
                    return;
                }

                _hit.collider.GetComponent<Enemy>().TakeDamage(hitDamage);
                ServerSend.PlayerHitInfo(userWeaponsController.player.id, _hit.point, hitDamage);

                if (_hit.collider.GetComponent<Enemy>().health <= 0)
                {
                    userWeaponsController.player.kills++;
                    ServerSend.PlayerKilled(userWeaponsController.player.username, "Bot");
                    ServerSend.PlayerDeathsAndKills(userWeaponsController.player);
                }
            }
        }
    }

    void ChangeFireMode()
    {
        if (currentFireModeState == 0)
        {
            currentFireModeState = 1;
        }
        else if (currentFireModeState == 1 && enabledFireModes.Length == 3)
        {
            currentFireModeState = 2;
        }
        else
        {
            currentFireModeState = 0;
        }

        currentFireMode = enabledFireModes[currentFireModeState];
    }
    public void SetFireMode(int _fireModeInt)
    {
        currentFireModeState = _fireModeInt;
        switch (_fireModeInt)
        {
            case 2:
                currentFireMode = FireModes.Auto;
                break;
            case 1:
                currentFireMode = FireModes.Burst;
                break;
            default: // Semi
                currentFireMode = FireModes.Semi;
                break;
        }
    }


    public void Reload()
    {
        if (reloading == true)
        {
            // Already reloading
            return;
        }
        if (currentClipAmmo >= maxClipAmmo)
        {
            // Already full of ammo
            return;
        }

        if (currentExtraAmmo <= 0)
        {
            if (currentClipAmmo <= 0)
            {
                // Completely out of ammo
            }
            // Out of extra ammo

            return;
        }


        // Reload
        reloading = true;

        StartCoroutine(CompleteReload());
    }
    IEnumerator CompleteReload()
    {
        yield return new WaitForSeconds(reloadTime);

        if (reloading == false)
        {
            Debug.Log("Reload has been stopped");
            yield break;
        }

        int reloadAmount = maxClipAmmo - currentClipAmmo;
        if (currentExtraAmmo < reloadAmount)
        {
            reloadAmount = currentExtraAmmo;
        }

        currentExtraAmmo -= reloadAmount;
        currentClipAmmo += reloadAmount;

        reloading = false;
        // Send to reloading to all clients that reload is done
        if (userWeaponsController != null)
        {
            ServerSend.PlayerReloadDone(userWeaponsController.player, id, currentClipAmmo, currentExtraAmmo);
        }
        else
        {
            ServerSend.PlayerReloadDone(null, id, currentClipAmmo, currentExtraAmmo);
        }
    }
}
