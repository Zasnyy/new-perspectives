
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackRange = 1.8f;

    [Header("2D Attack Coverage")]
    [SerializeField] private float attackWidth2D = 1.4f;
    [SerializeField] private float attackHeight2D = 1.5f;
    [SerializeField] private float attackDepth2D = 0.8f;

    [Header("3D Attack Coverage")]
    [SerializeField] private float attackRadius3D = 1.8f;
    [SerializeField, Range(0f, 180f)]
    private float attackAngle3D = 100f;

    [Header("References")]
    [SerializeField] private Transform twoDPlane;

    [Header("Collision")]
    [SerializeField] private LayerMask playerLayer;

    private PerspectiveMode currentPerspective =
        PerspectiveMode.ThreeD;

    private bool hasAttackedThisSwing;

    public void SetPerspective(PerspectiveMode mode)
    {
        currentPerspective = mode;
    }

    public void ExecuteAttack(Transform target)
    {
        if (target == null || hasAttackedThisSwing)
            return;

        hasAttackedThisSwing = true;

        if (currentPerspective == PerspectiveMode.TwoD)
        {
            Perform2DAttack(target);
        }
        else
        {
            Perform3DAttack(target);
        }
    }

    private void Perform2DAttack(Transform target)
    {
        if (twoDPlane == null)
        {
            Debug.LogError("EnemyCombat: 2D Plane not assigned.", this);
            return;
        }

        Vector3 planeNormal = twoDPlane.forward.normalized;

        // Orient the box to the 2D plane.
        Quaternion boxRotation = Quaternion.LookRotation(
            planeNormal,
            Vector3.up);

        Vector3 planeRight = Vector3.ProjectOnPlane(
            twoDPlane.right,
            Vector3.up).normalized;

        Vector3 attackCenter =
            transform.position +
            planeRight * attackRange * 0.5f +
            Vector3.up * 0.5f;

        Vector3 halfExtents = new Vector3(
            attackWidth2D * 0.5f,
            attackHeight2D * 0.5f,
            attackDepth2D * 0.5f);

        Collider[] hits = Physics.OverlapBox(
            attackCenter,
            halfExtents,
            boxRotation,
            playerLayer,
            QueryTriggerInteraction.Ignore);

        CheckForPlayerHit(hits, target);
    }

    private void Perform3DAttack(Transform target)
    {
        Vector3 attackCenter =
            transform.position +
            transform.forward * attackRange * 0.5f +
            Vector3.up * 0.5f;

        Collider[] hits = Physics.OverlapSphere(
            attackCenter,
            attackRadius3D,
            playerLayer,
            QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            if (!IsTargetCollider(hit, target))
                continue;

            Vector3 direction = target.position - transform.position;

            direction.y = 0f;

            float angle = Vector3.Angle(
                transform.forward,
                direction);

            if (angle <= attackAngle3D * 0.5f)
            {
                ApplyPlayerDamage(target);
                return;
            }
        }
    }

    private void CheckForPlayerHit(
        Collider[] hits,
        Transform target)
    {
        foreach (Collider hit in hits)
        {
            if (IsTargetCollider(hit, target))
            {
                ApplyPlayerDamage(target);
                return;
            }
        }
    }

    private bool IsTargetCollider(Collider hit, Transform target)
    {
        return hit.transform == target ||
               hit.transform.IsChildOf(target);
    }

    private void ApplyPlayerDamage(Transform target)
    {
        // The player's health component can receive this message.
        target.gameObject.SendMessage(
            "TakeDamage",
            attackDamage,
            SendMessageOptions.DontRequireReceiver);

        Debug.Log("Enemy hit player for " + attackDamage + " damage.");
    }

    public void ResetAttack()
    {
        hasAttackedThisSwing = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (currentPerspective == PerspectiveMode.TwoD)
        {
            Vector3 center =
                transform.position +
                transform.forward * attackRange * 0.5f +
                Vector3.up * 0.5f;

            Gizmos.color = Color.yellow;

            Gizmos.DrawWireCube(
                center,
                new Vector3(
                    attackWidth2D,
                    attackHeight2D,
                    attackDepth2D));
        }
        else
        {
            Vector3 center =
                transform.position +
                transform.forward * attackRange * 0.5f +
                Vector3.up * 0.5f;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, attackRadius3D);
        }
    }
}
