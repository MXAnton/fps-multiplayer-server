using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WeaponUI : MonoBehaviour
{
    WeaponsController weaponsController;

    public Weapon currentWeaponScript;

    public TextMeshProUGUI clipAmmoText;
    public TextMeshProUGUI extraAmmoText;
    public TextMeshProUGUI grenadeAmmountText;

    [Header("Hitinfo")]
    public GameObject hitDamageTextPrefab;
    public float hitDamageShowTime = 0.8f;
    [Space]
    public GameObject hitMarkPrefab;
    public GameObject criticalHitMarkPrefab;
    public float hitMarkShowTime = 0.3f;

    [Space]
    public GameObject uICanvas;
    public GameObject currentWeaponIcon;
    public GameObject weaponIconHolder;

    void Start()
    {
        weaponsController = GetComponent<WeaponsController>();
    }

    void Update()
    {
        if (currentWeaponScript != null)
        {
            clipAmmoText.text = "" + currentWeaponScript.currentClipAmmo;
            extraAmmoText.text = "" + currentWeaponScript.currentExtraAmmo;
        }
        else
        {
            clipAmmoText.text = "";
            extraAmmoText.text = "";
        }
    }

    public void ShowHitDamage(float hitDamage)
    {
        TextMeshProUGUI newHitDamageText = Instantiate(hitDamageTextPrefab, uICanvas.transform).GetComponent<TextMeshProUGUI>();

        newHitDamageText.text = hitDamage + "";
        //newHitDamageText.gameObject.SetActive(true);
        newHitDamageText.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-20, 20));
        newHitDamageText.rectTransform.localPosition = new Vector2(Random.Range(-20, 20), Random.Range(newHitDamageText.rectTransform.localPosition.y - 20,
                                                                                            newHitDamageText.rectTransform.localPosition.y + 20));
        //StopAllCoroutines();
        StartCoroutine(DisableHitDamageText(hitDamageShowTime, newHitDamageText));
    }
    IEnumerator DisableHitDamageText(float time, TextMeshProUGUI newHitDamageText)
    {
        yield return new WaitForSeconds(time);

        Destroy(newHitDamageText.gameObject);
        //newHitDamageText.gameObject.SetActive(false);
    }

    public void ShowHitMark(bool criticalHit)
    {
        GameObject newHitMarkPrefab = hitMarkPrefab;
        if (criticalHit == true)
        {
            newHitMarkPrefab = criticalHitMarkPrefab;
        }

        RectTransform newHitMark = Instantiate(newHitMarkPrefab, uICanvas.transform).GetComponent<RectTransform>();

        newHitMark.gameObject.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-20, 20));
        newHitMark.localPosition = new Vector2(Random.Range(-2, 2), Random.Range(-2, 2));

        StartCoroutine(DeleteHitMark(hitMarkShowTime, newHitMark));
    }
    IEnumerator DeleteHitMark(float time, RectTransform newHitMark)
    {
        yield return new WaitForSeconds(time);

        Destroy(newHitMark.gameObject);
    }


    public void SetGrenadeAmountText(int value)
    {
        grenadeAmmountText.text = "" + value;
    }


    public void SetWeaponIcon(GameObject newIcon)
    {
        foreach (Transform child in weaponIconHolder.transform)
        {
            Destroy(child.gameObject);
        }

        Instantiate(newIcon, weaponIconHolder.transform);
    }
}
