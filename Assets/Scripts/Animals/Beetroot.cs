using UnityEngine;

public class Beetroot : FoodItem
{
    [Header("Remolacha")]
    [SerializeField] private float bounceForce = 1.5f;
    [SerializeField] private float settleTime = 1.2f;

    private Rigidbody rb;
    private bool hasSettled = false;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
        Invoke(nameof(Settle), settleTime);
    }

    private void Settle()
    {
        hasSettled = true;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    private void OnCollisionEnter(Collision col)
    {
        if (hasSettled) return;
        if (rb != null && col.contacts.Length > 0)
        {
            Vector3 bounce = col.contacts[0].normal * bounceForce;
            rb.AddForce(bounce, ForceMode.Impulse);
        }
    }
}
