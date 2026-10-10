using UnityEngine;

public class SharkOilAbility : TankSpecialAbility
{
    [Header("Ajustes de Trampa (Tiburón)")]
    public GameObject oilSlickPrefab;

    [Tooltip("Altura sobre el suelo para que el aceite no quede enterrado")]
    public float heightOffset = 0.3f;

    public override void ActivateAbility()
    {   
        if (oilSlickPrefab != null)
        {
            // Justo detrás del tanque y un poco elevado sobre el piso
            Vector3 spawnPosition = transform.position
                                    - (transform.forward * 1.5f)
                                    + (Vector3.up * heightOffset);

            GameObject oil = Instantiate(oilSlickPrefab, spawnPosition, Quaternion.identity);
            Debug.Log("Tiburón soltó trampa de aceite en: " + oil.transform.position);
        }
    }
}