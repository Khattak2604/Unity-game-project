using UnityEngine;

// GDD section 9 — one reusable player controller; era-specific abilities
// (blocking, double jump, dash) are toggled per chapter, never duplicated.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float sprintSpeed = 9f;
    public float jumpSpeed = 5.5f;
    public float gravity = 18f;
    public float lookSensitivity = 2.2f;

    [HideInInspector] public WeaponBase weapon;
    [HideInInspector] public bool advancedMovement;  // Future era: double jump + dash

    public bool IsBlocking { get; private set; }
    public const float DashCooldown = 2f;
    public float DashReadyIn { get { return Mathf.Max(0f, dashCooldownEnd - Time.time); } }

    CharacterController controller;
    Health health;
    Transform cam;
    float pitch;
    float verticalVelocity;
    int airJumpsLeft;
    float dashCooldownEnd;
    float dashEnd;
    Vector3 dashDir;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        health = GetComponent<Health>();
        // Medieval shield: blocking absorbs 80% of incoming damage.
        health.damageModifier = d => IsBlocking ? d * 0.2f : d;
    }

    public void AttachCamera(Transform cameraTransform)
    {
        cam = cameraTransform;
        cam.SetParent(transform);
        cam.localPosition = new Vector3(0f, 0.65f, 0f);
        cam.localRotation = Quaternion.identity;
        pitch = 0f;
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (health.IsDead) return;
        Look();
        Move();
        Combat();
    }

    void Look()
    {
        transform.Rotate(0f, Input.GetAxis("Mouse X") * lookSensitivity, 0f);
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * lookSensitivity, -85f, 85f);
        if (cam != null) cam.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    void Move()
    {
        bool grounded = controller.isGrounded;
        if (grounded)
        {
            airJumpsLeft = advancedMovement ? 1 : 0;
            if (verticalVelocity < 0f) verticalVelocity = -1f;
        }

        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        input = Vector3.ClampMagnitude(input, 1f);
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
        Vector3 move = transform.TransformDirection(input) * speed;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (grounded) verticalVelocity = jumpSpeed;
            else if (airJumpsLeft > 0) { verticalVelocity = jumpSpeed; airJumpsLeft--; }
        }

        if (advancedMovement && Input.GetKeyDown(KeyCode.Q) && DashReadyIn <= 0f)
        {
            dashDir = move.sqrMagnitude > 0.1f ? move.normalized : transform.forward;
            dashEnd = Time.time + 0.18f;
            dashCooldownEnd = Time.time + DashCooldown;
        }
        if (Time.time < dashEnd) move = dashDir * 22f;

        verticalVelocity -= gravity * Time.deltaTime;
        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }

    void Combat()
    {
        if (weapon == null) return;

        MeleeWeapon melee = weapon as MeleeWeapon;
        IsBlocking = melee != null && Input.GetMouseButton(1);

        if (Input.GetMouseButton(0) && !IsBlocking) weapon.UseWeapon();

        FirearmWeapon firearm = weapon as FirearmWeapon;
        if (firearm != null && Input.GetKeyDown(KeyCode.R)) firearm.StartReload();
    }
}
