using System.Collections.Generic;
using UnityEngine;

public enum EnemyState { Idle, Chase, Attack, Dead }

// GDD section 12 — finite state machine AI. One script, configured per era:
// Medieval swordsman (melee rush), WWII rifleman (ranged, strafes),
// Future combat bot (ranged, dashes). ponytail: no NavMesh — open arenas with
// low obstacles only; switch to NavMeshAgent when levels gain real geometry.
public class EnemyAI : MonoBehaviour
{
    public static readonly List<EnemyAI> Alive = new List<EnemyAI>();

    public float moveSpeed = 3.5f;
    public float sightRange = 32f;
    public float attackRange = 2.2f;
    public float attackDamage = 10f;
    public float attackRate = 1f;       // attacks per second
    public float attackWindup = 0.35f;  // telegraph before damage lands
    public bool isRanged;
    public float rangedSpread = 0.06f;
    public bool canDash;                // Future bot sidestep
    public Color tracerColor = new Color(1f, 0.3f, 0.2f);

    [HideInInspector] public EnemyVisual visual;

    EnemyState state = EnemyState.Idle;
    Transform player;
    Health playerHealth;
    Health health;
    CharacterController controller;
    float nextAttackTime;
    float windupEnd;
    bool windingUp;
    float strafeDir = 1f;
    float nextStrafeFlip;
    float nextDashTime;
    float verticalVelocity;

    public EnemyState State { get { return state; } }

    void Awake()
    {
        health = GetComponent<Health>();
        controller = GetComponent<CharacterController>();
        health.onDeath += OnDeath;
        health.onDamaged += OnDamaged;
    }

    void OnEnable() { if (!Alive.Contains(this)) Alive.Add(this); }
    void OnDisable() { Alive.Remove(this); }

    public void SetTarget(Transform target)
    {
        player = target;
        playerHealth = target.GetComponent<Health>();
    }

    void Update()
    {
        if (state == EnemyState.Dead) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (player == null || playerHealth == null || playerHealth.IsDead)
        {
            state = EnemyState.Idle;
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (visual != null)
            visual.SetMoving(state == EnemyState.Chase || (state == EnemyState.Attack && isRanged));

        switch (state)
        {
            case EnemyState.Idle:
                if (dist <= sightRange) state = EnemyState.Chase;
                break;

            case EnemyState.Chase:
                if (dist <= attackRange) { state = EnemyState.Attack; break; }
                FacePlayer();
                MoveHorizontal(transform.forward * moveSpeed);
                break;

            case EnemyState.Attack:
                FacePlayer();
                if (dist > attackRange * 1.2f) { windingUp = false; state = EnemyState.Chase; break; }
                if (isRanged) StrafeAndDash();
                TickAttack(dist);
                break;
        }
        ApplyGravity();
    }

    void FacePlayer()
    {
        Vector3 to = player.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(to), 10f * Time.deltaTime);
    }

    void MoveHorizontal(Vector3 velocity)
    {
        velocity.y = 0f;
        controller.Move(velocity * Time.deltaTime);
    }

    void ApplyGravity()
    {
        verticalVelocity = controller.isGrounded ? -1f : verticalVelocity - 18f * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    void StrafeAndDash()
    {
        if (Time.time >= nextStrafeFlip)
        {
            strafeDir = -strafeDir;
            nextStrafeFlip = Time.time + Random.Range(1.2f, 2.6f);
        }
        float speed = moveSpeed * 0.55f;
        if (canDash && Time.time >= nextDashTime)
        {
            speed = moveSpeed * 4f;  // short burst sidestep
            if (Time.time >= nextDashTime + 0.15f)
                nextDashTime = Time.time + Random.Range(2.5f, 4f);
        }
        MoveHorizontal(transform.right * strafeDir * speed);
    }

    void TickAttack(float dist)
    {
        if (!windingUp)
        {
            if (Time.time < nextAttackTime) return;
            windingUp = true;
            windupEnd = Time.time + attackWindup;
            return;
        }
        if (Time.time < windupEnd) return;

        windingUp = false;
        nextAttackTime = Time.time + 1f / attackRate;

        if (isRanged) FireShot();
        else if (dist <= attackRange * 1.3f) playerHealth.TakeDamage(attackDamage);
    }

    void FireShot()
    {
        Vector3 origin = transform.position + Vector3.up * 0.7f;
        Vector3 aim = (player.position + Vector3.up * 0.4f) - origin;
        origin += aim.normalized * 0.7f;  // clear own collider
        Vector3 dir = aim.normalized
            + transform.right * Random.Range(-rangedSpread, rangedSpread)
            + transform.up * Random.Range(-rangedSpread, rangedSpread);

        Vector3 end = origin + dir * 60f;
        if (Physics.Raycast(origin, dir, out RaycastHit hit, 60f))
        {
            end = hit.point;
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null && (Object)target != (Object)health) target.TakeDamage(attackDamage);
        }
        Tracer.Spawn(origin, end, tracerColor);
    }

    void OnDamaged()
    {
        if (visual != null && state != EnemyState.Dead) visual.Flash();
        if (state == EnemyState.Idle) state = EnemyState.Chase;  // getting shot reveals the player
    }

    void OnDeath()
    {
        state = EnemyState.Dead;
        Alive.Remove(this);  // objective sees the kill immediately, not after cleanup
        if (controller != null) controller.enabled = false;
        transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);  // tip over
        transform.position += Vector3.up * 0.2f;
        Destroy(gameObject, 2.5f);
    }
}
