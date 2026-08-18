using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Quản lý Camera cho bản đồ Dungeon Tilemap:
/// - Chiếu vào RenderTexture 700x600 px hiển thị trên UI RawImage (khung 7x6 ô cố định).
/// - Xử lý thao tác rê chuột (Hover highlight) và Click di chuyển nhân vật chính xác trên RawImage.
/// - Xử lý thao tác kéo chuột cuộn map lên/xuống (Drag to Pan) và lăn con lăn chuột.
/// - Khóa biên cuộn (Clamp) từ đáy tầng đến đỉnh tầng.
/// - Tự động bám theo Player khi bước đi (Auto-Focus), tạm dừng khi đang kéo tay.
/// - Vẽ Gizmo trong Scene View để dễ dàng quan sát khung 7x6 ô.
/// </summary>
public class DungeonCameraController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IScrollHandler, IPointerMoveHandler, IPointerExitHandler, IPointerEnterHandler
{
    public static DungeonCameraController Instance { get; private set; }

    [Header("Tham Chiếu Camera & UI RawImage")]
    public Camera dungeonCamera;
    public RectTransform rawImageRect;
    public Transform playerTransform;

    [Header("Cài Đặt Khung Nhìn (7x7 Ô)")]
    public int currentFloorHeight = 7;
    public float cellSize = 1f;
    [Tooltip("Dịch chuyển tâm Camera lên/xuống theo trục Y")]
    public float cameraOffsetY = 0f;

    [Header("Cài Đặt Kéo Cuộn")]
    public float dragSensitivity = 0.01f;
    public float scrollSensitivity = 1f;
    public float smoothSpeed = 10f;
    [Tooltip("Ngưỡng pixel để phân biệt giữa Click chọn ô và Kéo cuộn camera")]
    public float dragThreshold = 10f;

    private Vector2 pointerDownPos;
    private bool isDragging = false;
    private bool isPointerOverViewport = false;
    private float targetCameraY = 3.0f;
    private float minCameraY = 3.0f;
    private float maxCameraY = 3.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dungeonCamera == null)
            dungeonCamera = GetComponent<Camera>();
        if (dungeonCamera == null)
            dungeonCamera = Camera.main;

        if (dungeonCamera != null)
        {
            dungeonCamera.orthographic = true;
            dungeonCamera.orthographicSize = 3.5f; // 3.5 * 2 = 7 ô chiều cao
            dungeonCamera.transform.position = new Vector3(3.5f, 3.5f, -10f);
        }

        HookRawImageViewport();
    }

    private void Start()
    {
        HookRawImageViewport();
    }

    /// <summary>
    /// Tự động tìm RawImage_DungeonViewport trên Canvas và gắn DungeonViewportInput nếu cần.
    /// </summary>
    public void HookRawImageViewport()
    {
        if (rawImageRect == null)
        {
            GameObject rawImgObj = GameObject.Find("RawImage_DungeonViewport");
            if (rawImgObj != null) rawImageRect = rawImgObj.GetComponent<RectTransform>();
        }

        if (rawImageRect != null)
        {
            if (rawImageRect.GetComponent<DungeonViewportInput>() == null)
            {
                rawImageRect.gameObject.AddComponent<DungeonViewportInput>();
            }
        }
    }

    /// <summary>
    /// Thiết lập biên độ cuộn camera theo chiều dài thực tế của tầng.
    /// Khung nhìn cố định: 7 ô ngang (X = 0..7) và 7 ô dọc (Y = 0..7).
    /// </summary>
    public void SetupFloorBounds(int height)
    {
        currentFloorHeight = height;

        if (dungeonCamera != null)
        {
            dungeonCamera.orthographic = true;
            dungeonCamera.orthographicSize = 3.5f; // 3.5 * 2 = 7 ô chiều cao (Tỉ lệ 1:1)
        }

        minCameraY = 3.5f; // Tâm của 7 ô đáy (Y từ 0 đến 7)
        maxCameraY = minCameraY + Mathf.Max(0, height - 7) * cellSize;
        targetCameraY = minCameraY;

        if (dungeonCamera != null)
        {
            // Trục X đặt cố định tại X = 3.5f (chính giữa 7 ô từ 0 đến 7)
            dungeonCamera.transform.position = new Vector3(3.5f, targetCameraY, -10f);
        }
    }

    private void Update()
    {
        if (dungeonCamera == null) return;
        Vector3 pos = dungeonCamera.transform.position;
        pos.x = 3.5f; // Cố định tâm trục X chính giữa 7 ô (0..7)
        pos.y = Mathf.Lerp(pos.y, targetCameraY + cameraOffsetY, Time.deltaTime * smoothSpeed);
        dungeonCamera.transform.position = pos;
    }

    private void LateUpdate()
    {
        // Cập nhật Hover Highlight mượt mà mỗi frame khi chuột đang ở trong RawImage
        if (isPointerOverViewport && !isDragging && rawImageRect != null && dungeonCamera != null)
        {
            Vector2 mouseScreenPos = UnityEngine.InputSystem.Mouse.current != null 
                ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() 
                : (Vector2)Input.mousePosition;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rawImageRect, mouseScreenPos, null, out Vector2 localPoint))
            {
                Rect r = rawImageRect.rect;
                if (r.Contains(localPoint))
                {
                    float u = (localPoint.x - r.x) / r.width;
                    float v = (localPoint.y - r.y) / r.height;

                    float camHeight = dungeonCamera.orthographicSize * 2f;
                    float camWidth  = camHeight * dungeonCamera.aspect;

                    float worldX = (dungeonCamera.transform.position.x - camWidth * 0.5f) + (u * camWidth);
                    float worldY = (dungeonCamera.transform.position.y - camHeight * 0.5f) + (v * camHeight);

                    Vector3Int cell = new Vector3Int(Mathf.FloorToInt(worldX), Mathf.FloorToInt(worldY), 0);
                    PlayerMovement.Instance?.HandleHoverAndClickFromViewport(cell, false);
                }
                else
                {
                    PlayerMovement.Instance?.ClearHighlight();
                }
            }
        }
    }

    /// <summary>
    /// Tự động di chuyển camera bám theo bước chân của Player.
    /// Tạm dừng nếu người chơi đang chủ động giữ chuột kéo tay.
    /// </summary>
    public void FocusOnPlayer(Vector3 playerPos)
    {
        if (isDragging) return;
        targetCameraY = Mathf.Clamp(playerPos.y, minCameraY, maxCameraY);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPos = eventData.position;
        isDragging = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float deltaDistance = Vector2.Distance(eventData.position, pointerDownPos);
        if (deltaDistance > dragThreshold)
        {
            isDragging = true;
            targetCameraY = Mathf.Clamp(targetCameraY - eventData.delta.y * dragSensitivity, minCameraY, maxCameraY);
            if (PlayerMovement.Instance != null) PlayerMovement.Instance.ClearHighlight();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging)
        {
            // Khoảng cách nhả chuột < 10px -> Tính là Click di chuyển vào ô
            Vector3Int? cell = GetCellFromPointer(eventData);
            if (cell.HasValue && PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.HandleHoverAndClickFromViewport(cell.Value, true);
            }
        }
        isDragging = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOverViewport = true;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        isPointerOverViewport = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOverViewport = false;
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.ClearHighlight();
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        targetCameraY = Mathf.Clamp(targetCameraY - eventData.scrollDelta.y * scrollSensitivity, minCameraY, maxCameraY);
    }

    /// <summary>
    /// Chuyển đổi tọa độ chạm từ UI RawImage sang tọa độ Grid Cell của bản đồ Dungeon.
    /// </summary>
    private Vector3Int? GetCellFromPointer(PointerEventData eventData)
    {
        if (rawImageRect == null || dungeonCamera == null) return null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rawImageRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            Rect r = rawImageRect.rect;
            
            // 1. Chuyển đổi sang tỉ lệ Normalized UV (0.0 đến 1.0)
            float u = (localPoint.x - r.x) / r.width;
            float v = (localPoint.y - r.y) / r.height;

            // 2. Tính tọa độ World Position thực tế theo khung nhìn của Camera
            float camHeight = dungeonCamera.orthographicSize * 2f; // = 6.0f
            float camWidth  = camHeight * dungeonCamera.aspect;    // = 7.0f (với tỉ lệ 700/600)

            float worldX = (dungeonCamera.transform.position.x - camWidth * 0.5f) + (u * camWidth);
            float worldY = (dungeonCamera.transform.position.y - camHeight * 0.5f) + (v * camHeight);

            // 3. Chuyển sang ô Grid Cell
            return new Vector3Int(Mathf.FloorToInt(worldX), Mathf.FloorToInt(worldY), 0);
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        // Vẽ khung nhìn 7x7 ô trong Scene View để dễ dàng quan sát khi xếp UI
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(3.5f, 3.5f, 0f), new Vector3(7f, 7f, 0.1f));
    }
}
