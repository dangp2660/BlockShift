using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpawnGridController : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform spawnSlot;
    public GameObject blockPrefab;
    public BlockMaterialLibrary materialLibrary;

    [Header("Holder Settings")]
    public BlockHolder.SpawnCountOption spawnCount = BlockHolder.SpawnCountOption.One;
    public BlockHolder.TwoBlockMode twoBlockMode = BlockHolder.TwoBlockMode.Auto;
    public float cellSizeOverride = 0f;

    [Header("Raycast")]
    public LayerMask pickLayer;
    public LayerMask gridLayer;

    [Header("Input Settings")]
    public float throwDuration = 0.2f;
    public float nearestCellSnapRadius = 2f;
    public float selectionRadius = 0.5f;

    [Header("Depth (Z)")]
    public float dragPlaneZ = -5f;
    public float gridPlaneZ = -1f;

    [Header("Debug")]
    public bool debugInput = true;
    public bool debugRays = true;
    private string debugText = "";

    private BlockHolder activeHolder;
    private Camera cam;
    private bool dragging = false;
    private Vector3 dragOffset;
    private GridCell hoverCell;

    // === Highlight debug variables ===
    private GridCell lastHoverCell;
    private Color originalColor = Color.white;
    private Vector3 preDragPosition;
    private Transform preDragParent;

    void Start()
    {
        Debug.Log("SpawnGridController initialized");
        cam = Camera.main;
        if (cam == null)
        {
            cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
        }
        SpawnNextPiece();
    }

    void Update()
    {

        if (activeHolder == null) return;

        bool tryStartDrag = PointerPressedThisFrame() || (!dragging && PointerIsPressed());
        if (tryStartDrag)
        {
            var h = RaycastBlockHolder();

            bool nearSpawnPiece = Vector3.Distance(
                GetPointerWorld(dragPlaneZ),
                new Vector3(activeHolder.transform.position.x, activeHolder.transform.position.y, dragPlaneZ)
            ) <= selectionRadius;

            if (h == activeHolder || (PointerPressedThisFrame() && nearSpawnPiece))
            {
                dragging = true;
                var pointer = GetPointerWorld(dragPlaneZ);
                var holderPos = activeHolder.transform.position;
                preDragPosition = activeHolder.transform.position;
                preDragParent = activeHolder.transform.parent;
                dragOffset = new Vector3(holderPos.x, holderPos.y, dragPlaneZ) - pointer;
                activeHolder.transform.SetParent(null);
            }
        }

        if (dragging)
        {
            var pointer = GetPointerWorld(dragPlaneZ) + dragOffset;
            activeHolder.transform.position = new Vector3(pointer.x, pointer.y, dragPlaneZ);

            hoverCell = RaycastGridCell();
            if (hoverCell == null)
                hoverCell = FindNearestFreeCell(activeHolder.transform.position);
        }

        // === Debug highlight for hovered grid cell ===
        if (hoverCell != lastHoverCell)
        {
            // Reset color of previous hover cell
            if (lastHoverCell != null)
            {
                var prevRenderer = lastHoverCell.GetComponent<Renderer>();
                if (prevRenderer != null)
                    prevRenderer.material.color = originalColor;
            }

            // Apply highlight to new cell
            if (hoverCell != null)
            {
                var rend = hoverCell.GetComponent<Renderer>();
                if (rend != null)
                {
                    originalColor = rend.material.color;
                    rend.material.color = Color.green; // highlight color
                }
            }

            lastHoverCell = hoverCell;
        }

        if (PointerReleasedThisFrame() && dragging)
        {
            dragging = false;
            StartCoroutine(HandleRelease());
        }

        // === Debug info ===
        if (debugInput)
        {
            var mouse = Mouse.current;
            bool mPresent = mouse != null;
            var screenPos = GetPointerScreen();
            var worldPos = GetPointerWorld(dragPlaneZ);
            bool pressed = mPresent && mouse.leftButton.isPressed;
            bool pressedFrame = mPresent && mouse.leftButton.wasPressedThisFrame;
            bool releasedFrame = mPresent && mouse.leftButton.wasReleasedThisFrame;
            string camName = cam != null ? cam.name : "null";
            float camZ = cam != null ? cam.transform.position.z : float.NaN;
            string holderName = activeHolder != null ? activeHolder.name : "null";
            string hover = hoverCell != null ? $"{hoverCell.name}" : "null";

            debugText =
                $"Mouse present: {mPresent}\n" +
                $"Screen: {screenPos}\n" +
                $"World({dragPlaneZ}): {worldPos}\n" +
                $"Pressed: {pressed}  PressedThisFrame: {pressedFrame}  ReleasedThisFrame: {releasedFrame}\n" +
                $"Camera: {camName}  Z: {camZ}\n" +
                $"ActiveHolder: {holderName}  Dragging: {dragging}\n" +
                $"HoverCell: {hover}";
        }

        if (debugRays && cam != null)
        {
            var ray = cam.ScreenPointToRay(GetPointerScreen());
            Debug.DrawRay(ray.origin, ray.direction * 5f, dragging ? Color.green : Color.yellow);
        }
    }

    private void OnGUI()
    {
        if (!debugInput) return;
        var style = new GUIStyle(GUI.skin.label);
        style.fontSize = 12;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(10, 10, 480, 200), debugText, style);
    }

    private IEnumerator HandleRelease()
    {
        var holder = activeHolder;
        var gm = Object.FindFirstObjectByType<GridManager>();
        if (gm == null) gm = Object.FindAnyObjectByType<GridManager>();

        // Reset hover highlight after placing
        if (lastHoverCell != null)
        {
            var rend = lastHoverCell.GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = originalColor;
            lastHoverCell = null;
        }

        if (gm != null && hoverCell != null)
        {
            bool placed = gm.TryPlaceAndSettle(holder, hoverCell, throwDuration);
            if (placed)
            {
                activeHolder = null;
                yield return new WaitForSeconds(0.05f);
                SpawnNextPiece();
                yield break;
            }
        }

        yield return SnapBack(holder);
    }

    private void SpawnNextPiece()
    {
        if (spawnSlot == null) spawnSlot = transform;

        GameObject go = new GameObject("Holder_Spawn");
        go.transform.SetParent(spawnSlot, true);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        var holder = go.AddComponent<BlockHolder>();
        holder.blockPrefab = blockPrefab;
        holder.library = materialLibrary;
        holder.autoBindToNearestCell = false;
        holder.useSpawnCount = true;
        holder.spawnCount = spawnCount;
        holder.twoBlockMode = twoBlockMode;

        float cs = cellSizeOverride;
        if (cs <= 0f)
        {
            var sampleCell = Object.FindFirstObjectByType<GridCell>();
            if (sampleCell == null) sampleCell = Object.FindAnyObjectByType<GridCell>();
            cs = sampleCell != null ? sampleCell.transform.localScale.x : 1f;
        }
        holder.cellSize = cs;

        var rootCol = go.GetComponent<BoxCollider>();
        if (rootCol == null) rootCol = go.AddComponent<BoxCollider>();
        rootCol.size = new Vector3(cs * 0.95f, cs * 0.95f, Mathf.Max(0.1f, cs * 0.1f));
        rootCol.center = Vector3.zero;

        var p = go.transform.position;
        go.transform.position = new Vector3(p.x, p.y, dragPlaneZ);

        activeHolder = holder;
    }

    private bool PointerPressedThisFrame()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began) return true;
        var mouse = Mouse.current;
        var touch = Touchscreen.current;
        return (mouse != null && mouse.leftButton.wasPressedThisFrame)
            || (touch != null && touch.primaryTouch.press.wasPressedThisFrame);
    }

    private bool PointerReleasedThisFrame()
    {
        if (Input.touchCount > 0)
        {
            var ph = Input.GetTouch(0).phase;
            if (ph == UnityEngine.TouchPhase.Ended || ph == UnityEngine.TouchPhase.Canceled) return true;
        }
        var mouse = Mouse.current;
        var touch = Touchscreen.current;
        return (mouse != null && mouse.leftButton.wasReleasedThisFrame)
            || (touch != null && touch.primaryTouch.press.wasReleasedThisFrame);
    }

    private bool PointerIsPressed()
    {
        if (Input.touchCount > 0)
        {
            var ph = Input.GetTouch(0).phase;
            if (ph == UnityEngine.TouchPhase.Moved || ph == UnityEngine.TouchPhase.Stationary || ph == UnityEngine.TouchPhase.Began) return true;
        }
        var mouse = Mouse.current;
        var touch = Touchscreen.current;
        return (mouse != null && mouse.leftButton.isPressed)
            || (touch != null && touch.primaryTouch.press.isPressed);
    }

    private Vector2 GetPointerScreen()
    {
        if (Input.touchCount > 0) return Input.GetTouch(0).position;
        var touch = Touchscreen.current;
        if (touch != null) return touch.primaryTouch.position.ReadValue();
        var mouse = Mouse.current;
        if (mouse != null) return mouse.position.ReadValue();
        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private GridCell RaycastGridCell()
    {
        Ray ray = cam.ScreenPointToRay(GetPointerScreen());
        int mask = gridLayer.value == 0 ? ~0 : gridLayer.value;

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, mask))
        {
            var cell = hit.transform.GetComponent<GridCell>();
            if (cell != null) return cell;
        }
        return null;
    }

    private Vector3 GetPointerWorld(float planeZ)
    {
        Ray ray = cam.ScreenPointToRay(GetPointerScreen());
        Plane plane = new Plane(-cam.transform.forward, new Vector3(0f, 0f, planeZ));
        if (plane.Raycast(ray, out float dist))
            return ray.GetPoint(dist);
        return new Vector3(0f, 0f, planeZ);
    }

    private BlockHolder RaycastBlockHolder()
    {
        if (activeHolder == null) return null;

        Ray ray = cam.ScreenPointToRay(GetPointerScreen());
        int mask = pickLayer.value == 0 ? ~0 : pickLayer.value;

        var rootCol = activeHolder.GetComponent<Collider>();
        if (rootCol != null && rootCol.Raycast(ray, out RaycastHit _, 500f))
            return activeHolder;

        if (Physics.SphereCast(ray, selectionRadius, out RaycastHit sphereHit, 500f, mask, QueryTriggerInteraction.Collide))
        {
            var holder = sphereHit.transform.GetComponentInParent<BlockHolder>();
            if (holder == activeHolder) return holder;
        }

        var hits = Physics.RaycastAll(ray, 500f, mask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            var holder = hits[i].transform.GetComponentInParent<BlockHolder>();
            if (holder == activeHolder) return holder;
        }

        return null;
    }

    private IEnumerator SnapBack(BlockHolder holder)
    {
        Vector3 start = holder.transform.position;
        Vector3 end = preDragPosition;
        float t = 0f;
        float d = Mathf.Max(0.1f, throwDuration * 0.75f);

        while (t < d)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / d);
            holder.transform.position = Vector3.Lerp(start, end, p);
            yield return null;
        }

        // Restore spawn parent while keeping the world position
        if (spawnSlot != null)
        {
            holder.transform.SetParent(spawnSlot, true);
        }
    }

    private GridCell FindNearestFreeCell(Vector3 worldPos)
    {
        var cells = Object.FindObjectsByType<GridCell>(FindObjectsSortMode.None);
        GridCell best = null;
        float bestDist = Mathf.Infinity;
        foreach (var c in cells)
        {
            if (c == null || c.isOccupied) continue;
            float d = Vector3.Distance(worldPos, c.transform.position);
            if (d < bestDist && d <= nearestCellSnapRadius)
            {
                bestDist = d;
                best = c;
            }
        }
        return best;
    }
}
