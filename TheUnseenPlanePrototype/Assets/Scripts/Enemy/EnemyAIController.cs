
using UnityEngine;

public enum EnemyAIState
{
    Idle,
    Chase,
    Telegraph,
    Attack,
    Recovery,
    Dead
}

public class EnemyAIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyDetection detection;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyCombat combat;
    [SerializeField] private EnemyHealth health;

    [Header("Combat Timing")]
    [SerializeField] private float telegraphDuration = 0.7f;
    [SerializeField] private float attackDuration = 0.2f;
    [SerializeField] private float recoveryDuration = 1.3f;

    [Header("Attack Range")]
    [SerializeField] private float attackRange = 1.8f;

    [Header("2D Alignment")]
    [SerializeField] private float maximumAttackDepthDifference = 0.3f;

    private EnemyAIState currentState = EnemyAIState.Idle;

    private float stateTimer;

    private Transform target;

    public EnemyAIState CurrentState => currentState;

    private void Awake()
    {
        if (detection == null)
            detection = GetComponent<EnemyDetection>();

        if (movement == null)
            movement = GetComponent<EnemyMovement>();

        if (combat == null)
            combat = GetComponent<EnemyCombat>();

        if (health == null)
            health = GetComponent<EnemyHealth>();
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            ChangeState(EnemyAIState.Dead);
        }

        if (currentState == EnemyAIState.Dead)
            return;

        switch (currentState)
        {
            case EnemyAIState.Idle:
                UpdateIdle();
                break;

            case EnemyAIState.Chase:
                UpdateChase();
                break;

            case EnemyAIState.Telegraph:
                UpdateTelegraph();
                break;

            case EnemyAIState.Attack:
                UpdateAttack();
                break;

            case EnemyAIState.Recovery:
                UpdateRecovery();
                break;
        }
    }

    private void UpdateIdle()
    {
        if (detection == null)
            return;

        if (detection.CanDetectPlayer())
        {
            target = detection.Player;
            ChangeState(EnemyAIState.Chase);
        }
    }

    private void UpdateChase()
    {
        if (target == null)
        {
            ChangeState(EnemyAIState.Idle);
            return;
        }

        if (detection.ShouldLoseTarget())
        {
            target = null;
            ChangeState(EnemyAIState.Idle);
            return;
        }

        if (movement == null)
            return;

        movement.MoveTowards(target);

        float distance = GetAttackDistance();

        bool canAttack = distance <= attackRange;

        // In 2D, the enemy must also be close enough to the plane.
        if (movement.CurrentPerspective == PerspectiveMode.TwoD)
        {
            canAttack &= movement.GetDepthDifference(target)
                         <= maximumAttackDepthDifference;
        }

        if (canAttack)
        {
            movement.FaceTarget(target);
            ChangeState(EnemyAIState.Telegraph);
        }
    }

    private void UpdateTelegraph()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer >= telegraphDuration)
        {
            ChangeState(EnemyAIState.Attack);
        }
    }

    private void UpdateAttack()
    {
        if (stateTimer <= 0f)
        {
            if (combat != null && target != null)
            {
                combat.ExecuteAttack(target);
            }
        }

        stateTimer += Time.deltaTime;

        if (stateTimer >= attackDuration)
        {
            ChangeState(EnemyAIState.Recovery);
        }
    }

    private void UpdateRecovery()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer >= recoveryDuration)
        {
            if (combat != null)
                combat.ResetAttack();

            if (target == null || detection.ShouldLoseTarget())
            {
                target = null;
                ChangeState(EnemyAIState.Idle);
            }
            else
            {
                ChangeState(EnemyAIState.Chase);
            }
        }
    }

    private float GetAttackDistance()
{
    if (target == null || movement == null)
        return Mathf.Infinity;

    if (movement.CurrentPerspective == PerspectiveMode.TwoD)
    {
        return movement.Get2DPlanarDistance(target);
    }

    Vector3 difference = target.position - transform.position;
    difference.y = 0f;

    return difference.magnitude;
}

    

    private void ChangeState(EnemyAIState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        stateTimer = 0f;

        switch (newState)
        {
            case EnemyAIState.Idle:
            case EnemyAIState.Chase:
            case EnemyAIState.Recovery:
                if (movement != null)
                    movement.SetMovementEnabled(true);
                break;

            case EnemyAIState.Telegraph:
            case EnemyAIState.Attack:
                if (movement != null)
                    movement.SetMovementEnabled(false);
                break;

            case EnemyAIState.Dead:
                if (movement != null)
                    movement.SetMovementEnabled(false);
                break;
        }

        Debug.Log("Enemy state changed to: " + newState);
    }
}
