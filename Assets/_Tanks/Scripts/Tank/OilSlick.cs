using UnityEngine;

public class OilSlick : MonoBehaviour
{
    public float lifeTime = 6f;
    public AudioSource spinAudio;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody targetRigidbody = other.GetComponentInParent<Rigidbody>();
        if (targetRigidbody != null && !other.transform.root.name.Contains("Shark"))
        {
            // Hace rotar sin control al tanque enemigo que pisa el aceite
            targetRigidbody.AddTorque(Vector3.up * 800f, ForceMode.Impulse);
            Debug.Log(other.name + " resbaló en el aceite!");

            if (spinAudio != null && !spinAudio.isPlaying)
                spinAudio.Play();
        }
    }
}