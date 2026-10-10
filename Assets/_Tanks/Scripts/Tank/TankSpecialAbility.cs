using UnityEngine;

public abstract class TankSpecialAbility : MonoBehaviour
{
    [Header("Configuración de Habilidad")]
    public KeyCode abilityKey = KeyCode.Space;
    public float cooldown = 5f;

    protected float lastUsedTime = -999f;

    public bool IsReady => Time.time >= lastUsedTime + cooldown;

    protected virtual void Update()
    {
        if (Input.GetKeyDown(abilityKey) && IsReady)
        {
            ActivateAbility();
            lastUsedTime = Time.time;
        }
    }

    // Método abstracto que implementará cada tanque según su habilidad
    public abstract void ActivateAbility();
}