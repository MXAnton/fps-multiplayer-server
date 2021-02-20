using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponsController : MonoBehaviour
{
    public Player player;

    public Vector3 shootOrigin;

    public float pickupDistance = 10f;
    public LayerMask weaponPickupIgnore;

    public float weaponDropForce = 5f;

    public GameObject weaponsHolder;

    public GameObject[] weaponsEquiped; // 0 = primary, 1 = secondary, 2 = melee

    public int weaponUsed = 0; // 0 = primary, 1 = secondary, 2 = melee

    void Start()
    {
        weaponUsed = 0;

        if (weaponsEquiped[0] != null)
        {
            weaponsEquiped[0].SetActive(true);

            if (weaponsEquiped[1] != null)
            {
                weaponsEquiped[1].SetActive(false);
            }
        }
        else if (weaponsEquiped[1] != null)
        {
            weaponsEquiped[1].SetActive(true);
        }
        if (weaponsEquiped[2] != null)
        {
            weaponsEquiped[2].SetActive(false);
        }
    }

    void Update()
    {
        CheckChangeWeaponInput();
    }


    public void TryPickUpWeapon(Vector3 _direction)
    {
        // Define ray
        Ray _pickupRay = new Ray(player.transform.position + shootOrigin, _direction);
        Physics.Raycast(_pickupRay, out RaycastHit _hit, pickupDistance, weaponPickupIgnore, QueryTriggerInteraction.Collide);

        if (_hit.collider != null)
        {
            if (_hit.transform.GetComponent<Weapon>())
            {
                // search all weapons in weaponsholder, then drop every weapon that is the same weapontype as the one to be picked up
                Weapon[] _weapons = player.weaponsController.weaponsHolder.transform.GetComponentsInChildren<Weapon>(true);
                foreach (Weapon _weapon in _weapons)
                {
                    //Debug.Log("id: " + _weapon.id);

                    if (_weapon.weaponType == _hit.transform.GetComponent<Weapon>().weaponType)
                    {
                        TryDropWeapon(_weapon.id, _hit.transform.GetComponent<Weapon>().weaponType, _direction);
                    }
                }

                _hit.collider.GetComponent<Rigidbody>().isKinematic = true;
                _hit.collider.enabled = false;
                _hit.transform.GetComponent<WeaponTransform>().enabled = false;

                _hit.transform.parent = weaponsHolder.transform;
                _hit.transform.position = weaponsHolder.transform.position;
                _hit.transform.rotation = weaponsHolder.transform.rotation;

                weaponsEquiped[_hit.transform.GetComponent<Weapon>().weaponType] = _hit.transform.gameObject;

                // Send to clients that player picked up weapon
                ServerSend.PlayerPickedWeapon(player.id, _hit.transform.GetComponent<Weapon>());

                _hit.transform.GetComponent<Weapon>().userWeaponsController = this;
                _hit.transform.GetComponent<Weapon>().enabled = true;
                _hit.transform.GetComponent<Weapon>().canFire = true;
            }
        }
    }

    public void TryDropWeapon(int _weaponId, int _weaponTypeUsed, Vector3 _direction)
    {
        //Debug.Log("Try drop weapon: " + _weaponId);
        if (Weapon.weapons[_weaponId].transform.parent == weaponsHolder.transform)
        {
            //Debug.Log("Dropped weapon");

            if (weaponsEquiped[_weaponTypeUsed] == Weapon.weapons[_weaponId])
            {
                weaponsEquiped[_weaponTypeUsed] = null;
            }
            else
            {
                Debug.Log("Didn't drop usedweapon");
            }

            Weapon.weapons[_weaponId].transform.parent = null;
            Weapon.weapons[_weaponId].GetComponent<Weapon>().userWeaponsController = null;
            Weapon.weapons[_weaponId].GetComponent<Weapon>().enabled = false;
            Weapon.weapons[_weaponId].GetComponent<Weapon>().reloading = false;

            Weapon.weapons[_weaponId].gameObject.SetActive(true);

            Weapon.weapons[_weaponId].GetComponent<WeaponTransform>().enabled = true;
            Weapon.weapons[_weaponId].GetComponent<Rigidbody>().isKinematic = false;

            // Add drop throw force
            Vector3 _throwForce = _direction;
            //Vector3 _throwForce = weaponsEquiped[_usedWeapon].transform.right;
            _throwForce *= weaponDropForce * Time.deltaTime;
            Weapon.weapons[_weaponId].GetComponent<Rigidbody>().AddForce(_throwForce, ForceMode.Acceleration);

            Weapon.weapons[_weaponId].GetComponent<Collider>().enabled = true;

            // Send to clients that player dropped weapon
            ServerSend.PlayerDroppedWeapon(player.id, Weapon.weapons[_weaponId].GetComponent<Weapon>());
        }
        else
        {
            Debug.Log("No weapon to drop!!!");
        }
    }
    public void TryDropWeapon(int _weaponId, int _usedWeapon, Vector3 _throwStartPos, Vector3 _throwStartRot, Vector3 _direction)
    {
        if (Weapon.weapons[_weaponId].transform.parent == weaponsHolder.transform)
        {
            Debug.Log("Dropped weapon");

            Weapon.weapons[_weaponId].gameObject.SetActive(true);

            Weapon.weapons[_weaponId].transform.parent = null;

            weaponsEquiped[_usedWeapon].transform.position = _throwStartPos;
            weaponsEquiped[_usedWeapon].transform.eulerAngles = _throwStartRot;

            Weapon.weapons[_weaponId].GetComponent<Weapon>().userWeaponsController = null;
            Weapon.weapons[_weaponId].GetComponent<Weapon>().enabled = false;
            Weapon.weapons[_weaponId].GetComponent<Weapon>().reloading = false;

            Weapon.weapons[_weaponId].GetComponent<WeaponTransform>().enabled = true;

            Weapon.weapons[_weaponId].GetComponent<Rigidbody>().isKinematic = false;

            // Add drop throw force
            Vector3 _throwForce = _direction;
            //Vector3 _throwForce = weaponsEquiped[_usedWeapon].transform.right;
            _throwForce *= weaponDropForce * Time.deltaTime;
            Weapon.weapons[_weaponId].GetComponent<Rigidbody>().AddForce(_throwForce, ForceMode.Force);

            Weapon.weapons[_weaponId].GetComponent<Collider>().enabled = true;

            // Send to clients that player dropped weapon
            ServerSend.PlayerDroppedWeapon(player.id, Weapon.weapons[_weaponId].GetComponent<Weapon>());

            if (weaponsEquiped[_usedWeapon] == Weapon.weapons[_weaponId].gameObject)
            {
                weaponsEquiped[_usedWeapon] = null;
            }
            else
            {
                Debug.Log("Didn't drop usedweapon");
            }
        }
        else
        {
            Debug.Log("No weapon to drop!!!");
        }
    }

    void CheckChangeWeaponInput()
    {
        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    // Select primary
        //    weaponUsed = 0;
        //    ChangeWeaponUsed();
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha2))
        //{
        //    // Select secondary
        //    weaponUsed = 1;
        //    ChangeWeaponUsed();
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha3))
        //{
        //    // Select melee
        //    weaponUsed = 2;
        //    ChangeWeaponUsed();
        //}


        //// If scrolling
        //float scrollWheelInput = Input.GetAxis("Mouse ScrollWheel");
        //if (scrollWheelInput < 0) // If scrolling up
        //{
        //    if (weaponUsed == 2)
        //    {
        //        weaponUsed = 0;
        //    }
        //    else
        //    {
        //        weaponUsed++;
        //    }

        //    ChangeWeaponUsed();
        //}
        //else if (scrollWheelInput > 0) // If scrolling down
        //{
        //    if (weaponUsed == 0)
        //    {
        //        weaponUsed = 2;
        //    }
        //    else
        //    {
        //        weaponUsed--;
        //    }

        //    ChangeWeaponUsed();
        //}
    }
    public void ChangeWeaponUsed()
    {
        switch (weaponUsed)
        {
            case 0:
                if (weaponsEquiped[0] != null)
                {
                    weaponsEquiped[0].SetActive(true);
                }
                if (weaponsEquiped[1] != null)
                {
                    weaponsEquiped[1].SetActive(false);
                }
                if (weaponsEquiped[2] != null)
                {
                    weaponsEquiped[2].SetActive(false);
                }
                break;
            case 1:
                if (weaponsEquiped[0] != null)
                {
                    weaponsEquiped[0].SetActive(false);
                }
                if (weaponsEquiped[1] != null)
                {
                    weaponsEquiped[1].SetActive(true);
                }
                if (weaponsEquiped[2] != null)
                {
                    weaponsEquiped[2].SetActive(false);
                }
                break;
            default:
                if (weaponsEquiped[0] != null)
                {
                    weaponsEquiped[0].SetActive(false);
                }
                if (weaponsEquiped[1] != null)
                {
                    weaponsEquiped[1].SetActive(false);
                }
                if (weaponsEquiped[2] != null)
                {
                    weaponsEquiped[2].SetActive(true);
                }
                break;
        }

        ServerSend.PlayerWeaponUsed(player.id, weaponUsed);
    }

    public void Reload(int _weapon)
    {
        weaponUsed = _weapon;
        ChangeWeaponUsed();

        if (weaponsEquiped[weaponUsed] != null)
        {
            weaponsEquiped[weaponUsed].GetComponent<Weapon>().Reload();
        }
    }
}
