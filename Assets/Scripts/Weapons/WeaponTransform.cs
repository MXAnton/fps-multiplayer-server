using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponTransform : MonoBehaviour
{
    public Weapon weapon;

    private Vector3 oldPosition;
    private Vector3 oldRotation;

    private void Start()
    {
        weapon = GetComponent<Weapon>();
    }

    private void FixedUpdate()
    {
        if (Vector3.Distance(oldPosition, transform.position) > 0.01f || Vector3.Distance(oldRotation, transform.localEulerAngles) > 0.01f)
        {
            oldPosition = transform.position;
            oldRotation = transform.localEulerAngles;
            ServerSend.WeaponPositionAndRotation(weapon.id, transform.position, transform.localEulerAngles);
            //Debug.Log("Weapon " + weapon.id + "'s position and rotation sent to clients");
        }
    }
}
