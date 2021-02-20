using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadesController : MonoBehaviour
{
    WeaponsController weaponsController;
    WeaponUI weaponUI;

    public int grenadesAmount;
    public int maxGrenades = 3;

    public GameObject grenadePrefab;
    public Transform grenadeThrowPos;

    public float grenadeThrowSpeed = 80;

    void Start()
    {
        weaponsController = GetComponent<WeaponsController>();
        weaponUI = GetComponent<WeaponUI>();

        grenadesAmount = maxGrenades;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (grenadesAmount <= 0)
            {
                return;
            }
            grenadesAmount--;
            weaponUI.SetGrenadeAmountText(grenadesAmount);

            GameObject newGrenade = Instantiate(grenadePrefab, grenadeThrowPos.position, Quaternion.LookRotation(transform.forward));

            newGrenade.GetComponent<GrenadeController>().weaponsController = weaponsController;

            newGrenade.GetComponent<Rigidbody>().AddForce(grenadeThrowSpeed * newGrenade.transform.forward, ForceMode.Impulse);
        }
    }
}
