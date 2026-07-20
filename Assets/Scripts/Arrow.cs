using UnityEngine;

// Physical arrow: flies with gravity, aligns to its velocity, sticks into
// whatever it hits and applies damage once.
public class Arrow : MonoBehaviour
{
    float damage;
    Rigidbody rb;
    bool stuck;

    public static void Launch(Vector3 position, Vector3 velocity, float damage, Collider owner, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Arrow";
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.045f, 0.045f, 0.8f);
        go.transform.rotation = Quaternion.LookRotation(velocity);
        go.GetComponent<Renderer>().material = LevelBuilder.ColoredMaterial(color, false);

        var arrow = go.AddComponent<Arrow>();
        arrow.damage = damage;
        arrow.rb = go.AddComponent<Rigidbody>();
        arrow.rb.mass = 0.25f;
        arrow.rb.linearVelocity = velocity;
        arrow.rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        if (owner != null) Physics.IgnoreCollision(go.GetComponent<Collider>(), owner);

        Destroy(go, 10f);
    }

    void FixedUpdate()
    {
        if (!stuck && rb.linearVelocity.sqrMagnitude > 1f)
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (stuck) return;
        stuck = true;
        IDamageable target = collision.collider.GetComponentInParent<IDamageable>();
        if (target != null) target.TakeDamage(damage);
        rb.isKinematic = true;
        GetComponent<Collider>().enabled = false;
        transform.SetParent(collision.transform);   // stick into the victim/wall
        Destroy(gameObject, 6f);
    }
}
