

using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("References")]
    [SerializeField] private EnemyMovement enemyMovement;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float loseTargetRange = 18f;

    [Header("Optional Line of Sight")]
    [SerializeField] private bool requireLineOfSight = false;
    [SerializeField] private LayerMask obstacleMask;

    public Transform Player => player;
    public bool HasTarget => player != null;

    private void Awake()
    {
        if (enemyMovement == null)
            enemyMovement = GetComponent<EnemyMovement>();
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player == null)
        {
            Debug.LogWarning(
                "EnemyDetection: Player not assigned.",
                this);
        }
    }

    private float GetDistanceToPlayer()
    {
        if (player == null)
            return Mathf.Infinity;

        Vector3 difference = player.position - transform.position;

        if (enemyMovement != null &&
            enemyMovement.CurrentPerspective == PerspectiveMode.TwoD)
        {
            return enemyMovement.Get2DPlanarDistance(player);
        }

        difference.y = 0f;

        return difference.magnitude;
    }

    public bool CanDetectPlayer()
    {
        if (player == null)
            return false;

        if (GetDistanceToPlayer() > detectionRange)
            return false;

        return !requireLineOfSight || HasLineOfSight();
    }

    public bool ShouldLoseTarget()
    {
        if (player == null)
            return true;

        return GetDistanceToPlayer() > loseTargetRange;
    }

    private bool HasLineOfSight()
    {
        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Vector3 destination = player.position + Vector3.up * 0.8f;

        Vector3 direction = destination - origin;

        return !Physics.Raycast(
            origin,
            direction.normalized,
            direction.magnitude,
            obstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    public void SetTarget(Transform newTarget)
    {
        player = newTarget;
    }
}
