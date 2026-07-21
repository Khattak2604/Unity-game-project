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

    [HideInInspector] public bool advancedMovement;  // Future era: double jump + dash

    // Era loadout (GDD section 20: 2-4 weapons per era) — switch with 1-5 / scroll.
    public System.Collections.Generic.List<WeaponBase> weapons = new System.Collections.Generic.List<WeaponBase>();
    int activeWeapon;
    public WeaponBase CurrentWeapon { get { return weapons.Count > 0 ? weapons[activeWeapon] : null; } }
    public int ActiveWeaponIndex { get { return activeWeapon; } }

    public bool IsBlocking { get; private set; }
    public bool IsMoving { get; private set; }
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
    float bobPhase;

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

    public void SetWeapons(System.Collections.Generic.List<WeaponBase> list)
    {
        weapons = list;
        activeWeapon = 0;
        RefreshViewModels();
    }

    void RefreshViewModels()
    {
        ShowViewModels(true);
    }

    // Cutscenes hide the first-person weapon; restore shows only the active one.
    public void ShowViewModels(bool show)
    {
        for (int i = 0; i < weapons.Count; i++)
            if (weapons[i].viewModel != null)
                weapons[i].viewModel.gameObject.SetActive(show && i == activeWeapon);
    }

    void SwitchWeapon(int index)
    {
        if (index < 0 || index >= weapons.Count || index == activeWeapon) return;
        activeWeapon = index;
        IsBlocking = false;
        RefreshViewModels();
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
        IsMoving = grounded && input.sqrMagnitude > 0.05f;
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
        Vector3 move = transform.TransformDirection(input) * speed;

        // head bob
        if (cam != null)
        {
            bobPhase += Time.deltaTime * (IsMoving ? (speed > moveSpeed ? 13f : 10f) : 0f);
            float bobY = IsMoving ? Mathf.Sin(bobPhase) * 0.035f : 0f;
            Vector3 target = new Vector3(0f, 0.65f + bobY, 0f);
            cam.localPosition = Vector3.Lerp(cam.localPosition, target, 12f * Time.deltaTime);
        }

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
        // weapon switching: number keys + mouse wheel
        for (int i = 0; i < weapons.Count && i < 5; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) SwitchWeapon(i);
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (weapons.Count > 1 && Mathf.Abs(scroll) > 0.01f)
            SwitchWeapon((activeWeapon + (scroll > 0f ? 1 : weapons.Count - 1)) % weapons.Count);

        WeaponBase weapon = CurrentWeapon;
        if (weapon == null) return;

        MeleeWeapon melee = weapon as MeleeWeapon;
        IsBlocking = melee != null && Input.GetMouseButton(1);
        if (melee != null && melee.viewModel != null) melee.viewModel.SetGuard(IsBlocking);

        FirearmWeapon firearm = weapon as FirearmWeapon;
        bool holdToFire = firearm != null && firearm.autoFire;   // semi-auto, bow and melee-hold feel
        bool firing = melee != null || holdToFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        if (firing && !IsBlocking) weapon.UseWeapon();

        if (firearm != null && Input.GetKeyDown(KeyCode.R)) firearm.StartReload();
    }
}
