using System.Collections;
using UnityEngine;

public enum ViewModelKind { Rifle, Pistol, Sword, Knife, Bow, EnergyBlade }

// First-person weapon model built from primitives, parented to the camera.
// Handles idle bob, attack animation (kick / swing / bow snap), guard pose and
// muzzle flash. ponytail: code-built detailed primitives, no Animator/art —
// swap for modeled viewmodels + animation clips when asset packs are imported.
public class WeaponViewModel : MonoBehaviour
{
    ViewModelKind kind;
    Transform pivot;
    Vector3 restPos;
    Quaternion restRot;
    Light muzzleLight;
    GameObject nockedArrow;
    float attackStart = -99f;
    float attackDuration = 0.2f;
    bool guarding;
    float bobPhase;

    static readonly Color DarkSteel = new Color(0.16f, 0.17f, 0.19f);
    static readonly Color Leather = new Color(0.3f, 0.19f, 0.1f);

    public static WeaponViewModel Create(Camera cam, ViewModelKind kind, Color main, Color accent, bool emissiveAccent)
    {
        var root = new GameObject("ViewModel_" + kind);
        root.transform.SetParent(cam.transform);
        var vm = root.AddComponent<WeaponViewModel>();
        vm.kind = kind;

        vm.pivot = new GameObject("Pivot").transform;
        vm.pivot.SetParent(root.transform);
        vm.pivot.localPosition = Vector3.zero;
        vm.pivot.localRotation = Quaternion.identity;

        switch (kind)
        {
            case ViewModelKind.Rifle:
                root.transform.localPosition = new Vector3(0.26f, -0.24f, 0.45f);
                vm.attackDuration = 0.12f;
                vm.Part(new Vector3(0f, -0.015f, -0.22f), new Vector3(0.05f, 0.095f, 0.2f), main);            // stock
                vm.Part(new Vector3(0f, -0.015f, -0.325f), new Vector3(0.055f, 0.1f, 0.02f), DarkSteel);      // butt plate
                vm.Part(new Vector3(0f, 0.005f, 0.02f), new Vector3(0.05f, 0.09f, 0.26f), accent, emissiveAccent); // receiver
                vm.PartRotated(new Vector3(0f, -0.09f, -0.06f), new Vector3(0.04f, 0.11f, 0.06f), new Vector3(25f, 0f, 0f), main); // grip
                vm.Part(new Vector3(0f, -0.055f, 0.02f), new Vector3(0.035f, 0.02f, 0.08f), DarkSteel);       // trigger guard
                vm.PartRotated(new Vector3(0f, -0.1f, 0.08f), new Vector3(0.04f, 0.13f, 0.07f), new Vector3(-8f, 0f, 0f), accent * 0.85f); // magazine
                vm.Part(new Vector3(0f, 0.01f, 0.42f), new Vector3(0.026f, 0.026f, 0.4f), DarkSteel);         // barrel
                vm.Part(new Vector3(0f, 0.005f, 0.25f), new Vector3(0.045f, 0.06f, 0.18f), main);             // handguard
                vm.Part(new Vector3(0f, 0.06f, 0.58f), new Vector3(0.012f, 0.05f, 0.012f), DarkSteel);        // front sight
                vm.Part(new Vector3(0f, 0.065f, -0.02f), new Vector3(0.05f, 0.03f, 0.015f), DarkSteel);       // rear sight
                vm.Part(new Vector3(0.045f, 0.02f, 0f), new Vector3(0.05f, 0.02f, 0.02f), new Color(0.55f, 0.56f, 0.6f)); // bolt handle
                vm.AddMuzzle(new Vector3(0f, 0.01f, 0.63f), emissiveAccent ? accent : new Color(1f, 0.8f, 0.4f));
                break;

            case ViewModelKind.Pistol:
                root.transform.localPosition = new Vector3(0.24f, -0.25f, 0.42f);
                vm.attackDuration = 0.1f;
                vm.Part(new Vector3(0f, 0.03f, 0.04f), new Vector3(0.042f, 0.05f, 0.2f), accent);             // slide
                vm.Part(new Vector3(0f, -0.005f, 0.03f), new Vector3(0.04f, 0.035f, 0.18f), main);            // frame
                vm.PartRotated(new Vector3(0f, -0.08f, -0.045f), new Vector3(0.038f, 0.12f, 0.07f), new Vector3(18f, 0f, 0f), main); // grip
                vm.Part(new Vector3(0f, -0.045f, 0.03f), new Vector3(0.03f, 0.018f, 0.07f), DarkSteel);       // trigger guard
                vm.Part(new Vector3(0f, 0.065f, 0.12f), new Vector3(0.01f, 0.02f, 0.015f), DarkSteel);        // front sight
                vm.Part(new Vector3(0f, 0.065f, -0.05f), new Vector3(0.04f, 0.02f, 0.015f), DarkSteel);       // rear sight
                vm.AddMuzzle(new Vector3(0f, 0.03f, 0.17f), new Color(1f, 0.8f, 0.4f));
                break;

            case ViewModelKind.Sword:
                root.transform.localPosition = new Vector3(0.3f, -0.26f, 0.45f);
                vm.attackDuration = 0.24f;
                vm.Part(new Vector3(0f, -0.14f, 0f), new Vector3(0.04f, 0.14f, 0.04f), Leather);              // grip
                vm.Part(new Vector3(0f, -0.22f, 0f), new Vector3(0.055f, 0.045f, 0.055f), DarkSteel);         // pommel
                vm.Part(new Vector3(0f, -0.055f, 0f), new Vector3(0.24f, 0.03f, 0.05f), accent);              // crossguard
                vm.Part(new Vector3(0f, 0.25f, 0f), new Vector3(0.055f, 0.58f, 0.016f), accent);              // blade
                vm.Part(new Vector3(0f, 0.6f, 0f), new Vector3(0.035f, 0.14f, 0.014f), accent);               // tapered tip
                vm.Part(new Vector3(0f, 0.25f, 0.009f), new Vector3(0.014f, 0.5f, 0.004f), accent * 0.7f);    // fuller
                vm.pivot.localRotation = Quaternion.Euler(35f, 0f, 0f);
                break;

            case ViewModelKind.Knife:
                root.transform.localPosition = new Vector3(0.28f, -0.27f, 0.42f);
                vm.attackDuration = 0.16f;
                vm.Part(new Vector3(0f, -0.07f, 0f), new Vector3(0.036f, 0.11f, 0.036f), new Color(0.14f, 0.12f, 0.1f)); // grip
                vm.Part(new Vector3(0f, -0.005f, 0f), new Vector3(0.09f, 0.018f, 0.03f), main);               // guard
                vm.Part(new Vector3(0f, 0.11f, 0f), new Vector3(0.032f, 0.2f, 0.012f), main);                 // blade
                vm.Part(new Vector3(0.012f, 0.11f, 0f), new Vector3(0.008f, 0.2f, 0.01f), main * 1.25f);      // edge bevel
                vm.pivot.localRotation = Quaternion.Euler(40f, 0f, 0f);
                break;

            case ViewModelKind.EnergyBlade:
                root.transform.localPosition = new Vector3(0.3f, -0.26f, 0.45f);
                vm.attackDuration = 0.2f;
                vm.Part(new Vector3(0f, -0.11f, 0f), new Vector3(0.05f, 0.16f, 0.05f), main);                 // hilt
                vm.Part(new Vector3(0f, -0.02f, 0f), new Vector3(0.065f, 0.03f, 0.065f), main * 1.2f);        // emitter
                vm.Part(new Vector3(0f, 0.28f, 0f), new Vector3(0.035f, 0.6f, 0.02f), accent, true);          // energy core
                vm.Part(new Vector3(0f, 0.28f, 0.012f), new Vector3(0.05f, 0.62f, 0.006f), accent, true);     // edge glow
                vm.pivot.localRotation = Quaternion.Euler(35f, 0f, 0f);
                break;

            case ViewModelKind.Bow:
                root.transform.localPosition = new Vector3(-0.16f, -0.2f, 0.5f);
                vm.attackDuration = 0.18f;
                vm.Part(new Vector3(0f, 0f, 0f), new Vector3(0.05f, 0.2f, 0.055f), Leather);                  // wrapped grip
                vm.PartRotated(new Vector3(0f, 0.22f, 0.03f), new Vector3(0.038f, 0.3f, 0.04f), new Vector3(-12f, 0f, 0f), main);   // upper limb
                vm.PartRotated(new Vector3(0f, 0.46f, 0.1f), new Vector3(0.032f, 0.26f, 0.036f), new Vector3(-30f, 0f, 0f), main);
                vm.PartRotated(new Vector3(0f, -0.22f, 0.03f), new Vector3(0.038f, 0.3f, 0.04f), new Vector3(12f, 0f, 0f), main);   // lower limb
                vm.PartRotated(new Vector3(0f, -0.46f, 0.1f), new Vector3(0.032f, 0.26f, 0.036f), new Vector3(30f, 0f, 0f), main);
                var stringGo = new GameObject("BowString");
                stringGo.transform.SetParent(vm.pivot);
                stringGo.transform.localPosition = Vector3.zero;
                var line = stringGo.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = 2;
                line.SetPosition(0, new Vector3(0f, 0.56f, 0.16f));
                line.SetPosition(1, new Vector3(0f, -0.56f, 0.16f));
                line.startWidth = 0.008f;
                line.endWidth = 0.008f;
                line.material = LevelBuilder.UnlitMaterial(new Color(0.85f, 0.82f, 0.7f));
                // nocked arrow, visible when ready to shoot
                vm.nockedArrow = new GameObject("NockedArrow");
                vm.nockedArrow.transform.SetParent(vm.pivot);
                vm.nockedArrow.transform.localPosition = Vector3.zero;
                vm.NockPart(new Vector3(0f, 0.02f, 0.3f), new Vector3(0.018f, 0.018f, 0.6f), Leather * 1.4f);
                vm.NockPart(new Vector3(0f, 0.02f, 0.62f), new Vector3(0.032f, 0.032f, 0.06f), DarkSteel);    // arrowhead
                break;
        }
        vm.restPos = root.transform.localPosition;
        vm.restRot = vm.pivot.localRotation;
        return vm;
    }

    void Part(Vector3 pos, Vector3 size, Color color, bool emissive = false)
    {
        PartRotated(pos, size, Vector3.zero, color, emissive);
    }

    void PartRotated(Vector3 pos, Vector3 size, Vector3 euler, Color color, bool emissive = false)
    {
        MakePart(pivot, pos, size, euler, color, emissive);
    }

    void NockPart(Vector3 pos, Vector3 size, Color color)
    {
        MakePart(nockedArrow.transform, pos, size, Vector3.zero, color, false);
    }

    static void MakePart(Transform parent, Vector3 pos, Vector3 size, Vector3 euler, Color color, bool emissive)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(p.GetComponent<Collider>());
        p.transform.SetParent(parent);
        p.transform.localPosition = pos;
        p.transform.localRotation = Quaternion.Euler(euler);
        p.transform.localScale = size;
        p.GetComponent<Renderer>().material = LevelBuilder.ColoredMaterial(color, emissive);
    }

    void AddMuzzle(Vector3 pos, Color color)
    {
        var go = new GameObject("MuzzleFlash");
        go.transform.SetParent(pivot);
        go.transform.localPosition = pos;
        muzzleLight = go.AddComponent<Light>();
        muzzleLight.type = LightType.Point;
        muzzleLight.color = color;
        muzzleLight.intensity = 3.5f;
        muzzleLight.range = 7f;
        muzzleLight.enabled = false;
    }

    public void PlayAttack()
    {
        attackStart = Time.time;
        if (muzzleLight != null) StartCoroutine(Flash());
        if (nockedArrow != null) StartCoroutine(Renock());
    }

    public void SetGuard(bool value) { guarding = value; }

    IEnumerator Flash()
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(0.05f);
        if (muzzleLight != null) muzzleLight.enabled = false;
    }

    IEnumerator Renock()
    {
        nockedArrow.SetActive(false);
        yield return new WaitForSeconds(0.45f);
        if (nockedArrow != null) nockedArrow.SetActive(true);
    }

    void Update()
    {
        // idle bob, driven by player movement
        var gm = GameManager.Instance;
        bool moving = gm != null && gm.IsPlaying && gm.Player != null && gm.Player.IsMoving;
        bobPhase += Time.deltaTime * (moving ? 9f : 2.2f);
        float bobAmp = moving ? 0.014f : 0.004f;
        Vector3 bob = new Vector3(Mathf.Sin(bobPhase * 0.5f) * bobAmp, Mathf.Sin(bobPhase) * bobAmp, 0f);

        float t = Mathf.Clamp01((Time.time - attackStart) / attackDuration);
        float curve = Mathf.Sin(t * 3.14159f);   // 0 -> 1 -> 0

        Vector3 pos = restPos + bob;
        Quaternion rot = restRot;

        switch (kind)
        {
            case ViewModelKind.Rifle:
            case ViewModelKind.Pistol:
                pos += new Vector3(0f, 0.012f * curve, -0.07f * curve);          // recoil kick
                rot = restRot * Quaternion.Euler(-7f * curve, 0f, 0f);
                break;
            case ViewModelKind.Sword:
            case ViewModelKind.Knife:
            case ViewModelKind.EnergyBlade:
                if (guarding)
                    rot = Quaternion.Slerp(pivot.localRotation, Quaternion.Euler(0f, 0f, 80f), 12f * Time.deltaTime);
                else
                    rot = restRot * Quaternion.Euler(70f * curve, -35f * curve, 0f);  // slash arc
                break;
            case ViewModelKind.Bow:
                pos += new Vector3(0f, 0f, -0.06f * curve);                       // draw + release
                break;
        }
        transform.localPosition = pos;
        pivot.localRotation = rot;
    }
}
