using System.Collections;
using UnityEngine;

public enum ViewModelKind { Rifle, Pistol, Sword, Knife, Bow, EnergyBlade }

// First-person weapon model built from primitives, parented to the camera.
// Handles idle bob, attack animation (kick / swing / bow snap), guard pose and
// muzzle flash. ponytail: code-lerped primitives, no Animator/art — replace
// with modeled viewmodels + animation clips in the polish phase.
public class WeaponViewModel : MonoBehaviour
{
    ViewModelKind kind;
    Transform pivot;
    Vector3 restPos;
    Quaternion restRot;
    Light muzzleLight;
    float attackStart = -99f;
    float attackDuration = 0.2f;
    bool guarding;
    float bobPhase;

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
                vm.Part(new Vector3(0f, -0.02f, -0.18f), new Vector3(0.055f, 0.1f, 0.24f), main);            // stock
                vm.Part(new Vector3(0f, 0f, 0.08f), new Vector3(0.05f, 0.085f, 0.34f), accent, emissiveAccent); // body
                vm.Part(new Vector3(0f, 0.005f, 0.38f), new Vector3(0.03f, 0.03f, 0.3f), main);              // barrel
                vm.Part(new Vector3(0f, -0.09f, 0.1f), new Vector3(0.04f, 0.12f, 0.07f), main);              // magazine
                vm.AddMuzzle(new Vector3(0f, 0.005f, 0.55f), emissiveAccent ? accent : new Color(1f, 0.8f, 0.4f));
                break;
            case ViewModelKind.Pistol:
                root.transform.localPosition = new Vector3(0.24f, -0.25f, 0.42f);
                vm.attackDuration = 0.1f;
                vm.Part(new Vector3(0f, 0.02f, 0.05f), new Vector3(0.04f, 0.07f, 0.2f), accent);
                vm.Part(new Vector3(0f, -0.07f, -0.02f), new Vector3(0.038f, 0.12f, 0.06f), main);
                vm.AddMuzzle(new Vector3(0f, 0.03f, 0.17f), new Color(1f, 0.8f, 0.4f));
                break;
            case ViewModelKind.Sword:
                root.transform.localPosition = new Vector3(0.3f, -0.28f, 0.45f);
                vm.attackDuration = 0.24f;
                vm.Part(new Vector3(0f, -0.12f, 0f), new Vector3(0.045f, 0.16f, 0.045f), new Color(0.35f, 0.22f, 0.1f)); // grip
                vm.Part(new Vector3(0f, -0.03f, 0f), new Vector3(0.22f, 0.035f, 0.05f), main);               // crossguard
                vm.Part(new Vector3(0f, 0.32f, 0f), new Vector3(0.05f, 0.68f, 0.018f), accent);              // blade
                vm.pivot.localRotation = Quaternion.Euler(35f, 0f, 0f);                                       // resting tilt
                break;
            case ViewModelKind.Knife:
                root.transform.localPosition = new Vector3(0.28f, -0.27f, 0.42f);
                vm.attackDuration = 0.16f;
                vm.Part(new Vector3(0f, -0.06f, 0f), new Vector3(0.04f, 0.1f, 0.04f), new Color(0.2f, 0.15f, 0.1f));
                vm.Part(new Vector3(0f, 0.09f, 0f), new Vector3(0.035f, 0.22f, 0.014f), main);
                vm.pivot.localRotation = Quaternion.Euler(40f, 0f, 0f);
                break;
            case ViewModelKind.EnergyBlade:
                root.transform.localPosition = new Vector3(0.3f, -0.28f, 0.45f);
                vm.attackDuration = 0.2f;
                vm.Part(new Vector3(0f, -0.1f, 0f), new Vector3(0.05f, 0.14f, 0.05f), main);
                vm.Part(new Vector3(0f, 0.28f, 0f), new Vector3(0.045f, 0.62f, 0.02f), accent, true);
                vm.pivot.localRotation = Quaternion.Euler(35f, 0f, 0f);
                break;
            case ViewModelKind.Bow:
                root.transform.localPosition = new Vector3(-0.16f, -0.2f, 0.5f);
                vm.attackDuration = 0.18f;
                vm.Part(new Vector3(0f, 0f, 0f), new Vector3(0.045f, 0.22f, 0.05f), main);                    // grip
                vm.PartRotated(new Vector3(0f, 0.3f, 0.05f), new Vector3(0.035f, 0.42f, 0.035f), new Vector3(-16f, 0f, 0f), main);  // upper limb
                vm.PartRotated(new Vector3(0f, -0.3f, 0.05f), new Vector3(0.035f, 0.42f, 0.035f), new Vector3(16f, 0f, 0f), main);  // lower limb
                var stringGo = new GameObject("BowString");
                stringGo.transform.SetParent(vm.pivot);
                stringGo.transform.localPosition = Vector3.zero;
                var line = stringGo.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = 2;
                line.SetPosition(0, new Vector3(0f, 0.48f, 0.1f));
                line.SetPosition(1, new Vector3(0f, -0.48f, 0.1f));
                line.startWidth = 0.008f;
                line.endWidth = 0.008f;
                line.material = LevelBuilder.UnlitMaterial(new Color(0.85f, 0.82f, 0.7f));
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
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(p.GetComponent<Collider>());
        p.transform.SetParent(pivot);
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
    }

    public void SetGuard(bool value) { guarding = value; }

    IEnumerator Flash()
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(0.05f);
        if (muzzleLight != null) muzzleLight.enabled = false;
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
