using UnityEngine;

public class InspectableBuilding : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private SelectionWheel selectionWheel;
    [SerializeField] private Camera mainCamera;

    // =========================================================
    // SPRITES
    // =========================================================

    [Header("Sprites")]
    [Tooltip("Add every SpriteRenderer that should react to hovering.")]
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    // =========================================================
    // OUTLINE
    // =========================================================

    [Header("Outline")]
    [SerializeField] private Material outlineMaterial;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // =========================================================
    // PRIVATE
    // =========================================================

    private Material[] normalMaterials;

    private bool isHovered;
    private bool outlineActive;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // =====================================================
        // CAMERA
        // =====================================================

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // =====================================================
        // SELECTION WHEEL
        // =====================================================

        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }

        // =====================================================
        // FIND ALL SPRITE RENDERERS
        // =====================================================

        if (spriteRenderers == null ||
            spriteRenderers.Length == 0)
        {
            spriteRenderers =
                GetComponentsInChildren<SpriteRenderer>(
                    true
                );
        }

        // =====================================================
        // SAVE ORIGINAL MATERIALS
        // =====================================================

        normalMaterials =
            new Material[
                spriteRenderers.Length
            ];

        for (int i = 0;
             i < spriteRenderers.Length;
             i++)
        {
            if (spriteRenderers[i] == null)
            {
                continue;
            }

            normalMaterials[i] =
                spriteRenderers[i].sharedMaterial;
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =====================================================
        // MUST BE INSPECTOR MODE
        // =====================================================

        if (!IsInspectorModeActive())
        {
            SetHovered(false);
            return;
        }

        // =====================================================
        // DON'T INSPECT WHILE TAB WHEEL IS OPEN
        // =====================================================

        if (selectionWheel != null &&
            selectionWheel.IsWheelOpen())
        {
            SetHovered(false);
            return;
        }

        // =====================================================
        // CHECK MOUSE
        // =====================================================

        bool mouseIsOverBuilding =
            IsMouseOverBuilding();

        SetHovered(
            mouseIsOverBuilding
        );
    }

    // =========================================================
    // INSPECTOR MODE
    // =========================================================

    private bool IsInspectorModeActive()
    {
        if (selectionWheel == null)
        {
            return false;
        }

        return selectionWheel.IsInspectorMode();
    }

    // =========================================================
    // MOUSE DETECTION
    // =========================================================

    private bool IsMouseOverBuilding()
    {
        if (mainCamera == null)
        {
            return false;
        }

        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        mouseWorldPosition.z = 0f;

        // =====================================================
        // CHECK EVERY SPRITE RENDERER
        // =====================================================

        for (int i = 0;
             i < spriteRenderers.Length;
             i++)
        {
            SpriteRenderer renderer =
                spriteRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            if (!renderer.enabled)
            {
                continue;
            }

            Bounds bounds =
                renderer.bounds;

            // SpriteRenderer bounds are 3D,
            // so force the mouse Z to match.
            Vector3 checkPosition =
                mouseWorldPosition;

            checkPosition.z =
                bounds.center.z;

            if (bounds.Contains(
                checkPosition
            ))
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // HOVER STATE
    // =========================================================

    private void SetHovered(
        bool hovered)
    {
        if (isHovered == hovered)
        {
            return;
        }

        isHovered = hovered;

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " Hovered = " +
                isHovered
            );
        }

        SetOutline(
            hovered
        );
    }

    // =========================================================
    // OUTLINE
    // =========================================================

    private void SetOutline(
        bool enabled)
    {
        if (outlineActive == enabled)
        {
            return;
        }

        // =====================================================
        // OUTLINE ON
        // =====================================================

        if (enabled)
        {
            if (outlineMaterial == null)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning(
                        gameObject.name +
                        " has no Outline Material assigned."
                    );
                }

                return;
            }

            for (int i = 0;
                 i < spriteRenderers.Length;
                 i++)
            {
                if (spriteRenderers[i] == null)
                {
                    continue;
                }

                spriteRenderers[i].material =
                    outlineMaterial;
            }
        }

        // =====================================================
        // OUTLINE OFF
        // =====================================================

        else
        {
            for (int i = 0;
                 i < spriteRenderers.Length;
                 i++)
            {
                if (spriteRenderers[i] == null)
                {
                    continue;
                }

                if (i >=
                    normalMaterials.Length)
                {
                    continue;
                }

                spriteRenderers[i].material =
                    normalMaterials[i];
            }
        }

        outlineActive = enabled;
    }

    // =========================================================
    // DISABLE
    // =========================================================

    private void OnDisable()
    {
        isHovered = false;

        // Restore original materials.
        if (spriteRenderers != null &&
            normalMaterials != null)
        {
            for (int i = 0;
                 i < spriteRenderers.Length;
                 i++)
            {
                if (spriteRenderers[i] == null)
                {
                    continue;
                }

                if (i >=
                    normalMaterials.Length)
                {
                    continue;
                }

                spriteRenderers[i].material =
                    normalMaterials[i];
            }
        }

        outlineActive = false;
    }

    // =========================================================
    // DEBUG GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (spriteRenderers == null)
        {
            return;
        }

        foreach (
            SpriteRenderer renderer
            in spriteRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Gizmos.DrawWireCube(
                renderer.bounds.center,
                renderer.bounds.size
            );
        }
    }
}