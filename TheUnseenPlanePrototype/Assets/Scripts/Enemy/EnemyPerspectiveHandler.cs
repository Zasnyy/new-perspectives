
using UnityEngine;

public class EnemyPerspectiveHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PerspectiveManager perspectiveManager;
    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private EnemyCombat enemyCombat;

    public PerspectiveMode CurrentPerspective { get; private set; }

    private void Awake()
    {
        if (perspectiveManager == null)
            perspectiveManager = FindFirstObjectByType<PerspectiveManager>();

        if (enemyMovement == null)
            enemyMovement = GetComponent<EnemyMovement>();

        if (enemyCombat == null)
            enemyCombat = GetComponent<EnemyCombat>();
    }

    private void OnEnable()
    {
        if (perspectiveManager == null)
            perspectiveManager = FindFirstObjectByType<PerspectiveManager>();

        if (perspectiveManager != null)
        {
            perspectiveManager.PerspectiveChanged += OnPerspectiveChanged;
            CurrentPerspective = perspectiveManager.CurrentPerspective;
        }
    }

   private void Start()
{
    if (perspectiveManager != null)
    {
        CurrentPerspective = perspectiveManager.CurrentPerspective;
        ApplyPerspective(CurrentPerspective);
    }
}

    private void OnDisable()
    {
        if (perspectiveManager != null)
        {
            perspectiveManager.PerspectiveChanged -= OnPerspectiveChanged;
        }
    }

    private void OnPerspectiveChanged(PerspectiveMode newPerspective)
    {
        CurrentPerspective = newPerspective;

        ApplyPerspective(newPerspective);
    }

    private void ApplyPerspective(PerspectiveMode mode)
    {
        if (enemyMovement != null)
            enemyMovement.SetPerspective(mode);

        if (enemyCombat != null)
            enemyCombat.SetPerspective(mode);

        Debug.Log("Enemy perspective updated: " + mode);
    }
}
