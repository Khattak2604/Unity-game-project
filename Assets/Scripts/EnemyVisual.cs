using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Procedural humanoid soldier body: head + helmet, torso, swinging arms/legs,
// hand-held weapon prop. ponytail: blocky primitive humanoids, no rigs — swap
// for skinned models + Animator once art assets enter the project.
public class EnemyVisual : MonoBehaviour
{
    Transform leftLeg, rightLeg, leftArm, rightArm;
    readonly List<Renderer> renderers = new List<Renderer>();
    readonly List<Color> baseColors = new List<Color>();
    bool moving;
    bool aiming;   // ranged enemies keep arms raised instead of swinging
    float phase;

    public void SetMoving(bool value) { moving = value; }

    public static EnemyVisual Build(Transform root, WarEra era, Color uniform, Color helmetColor, bool ranged)
    {
        var vroot = new GameObject("Visual");
        vroot.transform.SetParent(root);
        vroot.transform.localPosition = Vector3.zero;
        vroot.transform.localRotation = Quaternion.identity;
        var v = vroot.AddComponent<EnemyVisual>();
        v.aiming = ranged;

        Color skin = new Color(0.8f, 0.62f, 0.48f);
        Color dark = uniform * 0.75f;

        // torso + head
        v.Part(vroot.transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.52f, 0.72f, 0.3f), uniform);
        v.Part(vroot.transform, new Vector3(0f, 0.74f, 0f), new Vector3(0.3f, 0.3f, 0.28f), skin);
        // helmet: Future bots get a full glowing visor, others a cap on top
        if (era == WarEra.Future)
            v.Part(vroot.transform, new Vector3(0f, 0.76f, 0.02f), new Vector3(0.34f, 0.34f, 0.32f), helmetColor, true);
        else
            v.Part(vroot.transform, new Vector3(0f, 0.93f, 0f), new Vector3(0.36f, 0.14f, 0.34f), helmetColor);

        // limbs hang from pivots so they can swing
        v.leftArm = v.Limb(vroot.transform, new Vector3(-0.34f, 0.5f, 0f), new Vector3(0.15f, 0.55f, 0.15f), dark, v);
        v.rightArm = v.Limb(vroot.transform, new Vector3(0.34f, 0.5f, 0f), new Vector3(0.15f, 0.55f, 0.15f), dark, v);
        v.leftLeg = v.Limb(vroot.transform, new Vector3(-0.15f, -0.16f, 0f), new Vector3(0.17f, 0.8f, 0.17f), dark, v);
        v.rightLeg = v.Limb(vroot.transform, new Vector3(0.15f, -0.16f, 0f), new Vector3(0.17f, 0.8f, 0.17f), dark, v);

        // weapon prop in the right hand
        if (ranged)
        {
            v.Part(v.rightArm, new Vector3(0f, -0.5f, 0.32f), new Vector3(0.07f, 0.09f, 0.75f),
                era == WarEra.Future ? new Color(0.15f, 0.2f, 0.28f) : new Color(0.28f, 0.2f, 0.12f),
                era == WarEra.Future);
        }
        else
        {
            v.Part(v.rightArm, new Vector3(0f, -0.55f, 0.35f), new Vector3(0.05f, 0.08f, 0.85f),
                new Color(0.75f, 0.75f, 0.8f));
        }

        // ranged pose: raise both arms toward the target
        if (ranged)
        {
            v.leftArm.localRotation = Quaternion.Euler(-65f, 0f, 0f);
            v.rightArm.localRotation = Quaternion.Euler(-75f, 0f, 0f);
        }
        return v;
    }

    Transform Limb(Transform parent, Vector3 pivotPos, Vector3 size, Color color, EnemyVisual v)
    {
        var pivot = new GameObject("Limb").transform;
        pivot.SetParent(parent);
        pivot.localPosition = pivotPos;
        pivot.localRotation = Quaternion.identity;
        v.Part(pivot, new Vector3(0f, -size.y * 0.5f, 0f), size, color);
        return pivot;
    }

    void Part(Transform parent, Vector3 localPos, Vector3 size, Color color, bool emissive = false)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(p.GetComponent<Collider>());   // hit detection stays on the CharacterController
        p.transform.SetParent(parent);
        p.transform.localPosition = localPos;
        p.transform.localRotation = Quaternion.identity;
        p.transform.localScale = size;
        var r = p.GetComponent<Renderer>();
        r.material = LevelBuilder.ColoredMaterial(color, emissive);
        renderers.Add(r);
        baseColors.Add(color);
    }

    void Update()
    {
        if (moving)
        {
            phase += Time.deltaTime * 9f;
            float swing = Mathf.Sin(phase) * 30f;
            leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
            rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            if (!aiming)
            {
                leftArm.localRotation = Quaternion.Euler(-swing * 0.8f, 0f, 0f);
                rightArm.localRotation = Quaternion.Euler(swing * 0.8f, 0f, 0f);
            }
        }
        else
        {
            leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.identity, 8f * Time.deltaTime);
            rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.identity, 8f * Time.deltaTime);
        }
    }

    public void Flash() { StartCoroutine(FlashRoutine()); }

    IEnumerator FlashRoutine()
    {
        foreach (var r in renderers) if (r != null) r.material.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        for (int i = 0; i < renderers.Count; i++)
            if (renderers[i] != null) renderers[i].material.color = baseColors[i];
    }
}
