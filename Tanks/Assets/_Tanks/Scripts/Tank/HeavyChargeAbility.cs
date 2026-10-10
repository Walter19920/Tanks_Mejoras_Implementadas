using System.Collections;
using Tanks.Complete;
using UnityEngine;

public class HeavyChargeAbility : TankSpecialAbility
{
    [Header("Ajustes de Embestida (Heavy)")]
    public float chargeForce = 25f;       // Velocidad con la que avanza tu tanque
    public float pushForce = 30f;         // Fuerza con la que sale despedido el enemigo
    public float chargeDamage = 35f;      // Daño infligido al chocar
    public float chargeDuration = 0.8f;   // Tiempo de la ventana de impacto

    private Rigidbody m_Rigidbody;
    private bool m_IsCharging = false;

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    public override void ActivateAbility()
    {
        if (m_Rigidbody != null)
        {
            // Detener giros indeseados y aplicar impulso instantáneo
            m_Rigidbody.angularVelocity = Vector3.zero;
            m_Rigidbody.AddForce(transform.forward * chargeForce, ForceMode.VelocityChange);

            // Activar el estado de embestida temporalmente
            StopAllCoroutines();
            StartCoroutine(ChargeRoutine());

            Debug.Log("Heavy activó Embestida!");
        }
    }

    private IEnumerator ChargeRoutine()
    {
        m_IsCharging = true;
        yield return new WaitForSeconds(chargeDuration);
        m_IsCharging = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (m_IsCharging)
        {
            TankHealth targetHealth = collision.gameObject.GetComponentInParent<TankHealth>();

            if (targetHealth != null && collision.gameObject.transform.root != transform.root)
            {
                // 1. Infligir daño
                targetHealth.TakeDamage(chargeDamage);

                // 2. Empujar al enemigo
                Rigidbody targetRb = collision.gameObject.GetComponentInParent<Rigidbody>();
                if (targetRb != null)
                {
                    Vector3 pushDirection = transform.forward;
                    pushDirection.y = 0; // Mantener la fuerza en horizontal

                    // Se utiliza linearVelocity para compatibilidad con Unity 6
                    targetRb.linearVelocity = pushDirection.normalized * pushForce;
                }

                Debug.Log("¡Embestida impactó exitosamente!");
                m_IsCharging = false;
            }
        }
    }
}