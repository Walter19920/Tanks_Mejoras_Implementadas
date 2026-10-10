using UnityEngine;

public class UFODashAbility : TankSpecialAbility
{
    [Header("Ajustes de Dash (OVNI)")]
    public float dashForce = 25f;

    private Rigidbody m_Rigidbody;

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    public override void ActivateAbility()
    {
        if (m_Rigidbody != null)
        {
            // Detener cualquier rotación o giro acumulado antes del dash
            m_Rigidbody.angularVelocity = Vector3.zero;

            // Aplicar el impulso hacia adelante
            m_Rigidbody.AddForce(transform.forward * dashForce, ForceMode.VelocityChange);
            Debug.Log("OVNI usó Dash!");
        }
    }
}