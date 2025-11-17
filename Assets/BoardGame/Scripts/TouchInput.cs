using UnityEngine;
using UnityEngine.EventSystems;

public class TouchInput : MonoBehaviour
{
    [Header("Camera References")]
    private Camera mainCamera;
    private Transform cameraTransform;
    
    [Header("Pan Settings")]
    [SerializeField] private float panSpeed = 0.003f;
    [SerializeField] private float panSmoothTime = 0.1f;
    [SerializeField] private float mobileSwipeThreshold = 30f;
    [SerializeField] private float pcSwipeThreshold = 5f;
    
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 1f;
    [SerializeField] private float zoomSmoothTime = 0.15f;
    [SerializeField] private float pcZoomStep = 0.1f;
    [SerializeField] private float scrollZoomSpeed = 0.5f;
    
    [Header("Board Bounds")]
    [SerializeField] private float boundsPadding = 2f;
    
    private Vector3 targetCameraPosition;
    private Vector3 cameraPanVelocity;
    private float targetZoom;
    private float zoomVelocity;
    
    private float initialCameraY;
    private float minZoom;
    private float maxZoom;
    
    private Vector2 lastInputPosition;
    private Vector2 inputStartPosition;
    private bool isDragging = false;
    private bool wasDragging = false;
    
    private Vector2 touch0StartPos;
    private Vector2 touch1StartPos;
    private float initialPinchDistance;
    private bool isPinching = false;
    
    private Vector2 boardBoundsMin;
    private Vector2 boardBoundsMax;
    private bool boundsCalculated = false;

    private void Start()
    {
        mainCamera = Camera.main;
        cameraTransform = mainCamera.transform;
        targetCameraPosition = cameraTransform.position;
        targetZoom = mainCamera.orthographicSize;
        initialCameraY = cameraTransform.position.y;
        
        CalculateZoomBounds();
    }

    private void CalculateZoomBounds()
    {
        float currentZoom = mainCamera.orthographicSize;
        minZoom = currentZoom * 0.5f;
        maxZoom = currentZoom * 2f;
    }
    
    private void CalculateBoardBounds()
    {
        if (boundsCalculated) return;
        
        if (GameManager.Instance == null) return;
        
        int radius = GameManager.Instance.boardRadius;
        if (radius <= 0) return;
        
        float hexSize = GameManager.Instance.HexSize;
        
        float maxX = 0f;
        float maxZ = 0f;
        
        for (int q = -radius; q <= radius; q++)
        {
            int r1 = Mathf.Max(-radius, -q - radius);
            int r2 = Mathf.Min(radius, -q + radius);

            for (int r = r1; r <= r2; r++)
            {
                float x = hexSize * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r);
                float z = hexSize * (3f / 2f * r);
                
                maxX = Mathf.Max(maxX, Mathf.Abs(x));
                maxZ = Mathf.Max(maxZ, Mathf.Abs(z));
            }
        }
        
        boardBoundsMin = new Vector2(-maxX - boundsPadding, -maxZ - boundsPadding);
        boardBoundsMax = new Vector2(maxX + boundsPadding, maxZ + boundsPadding);
        boundsCalculated = true;
        
        Debug.Log($"Board bounds calculated: Min({boardBoundsMin.x}, {boardBoundsMin.y}), Max({boardBoundsMax.x}, {boardBoundsMax.y})");
    }

    private void Update()
    {
        if (!boundsCalculated)
        {
            CalculateBoardBounds();
        }
        
        if (Input.touchCount > 0)
        {
            HandleMobileInput();
        }
        else
        {
            HandlePCInput();
        }
        
        ApplyCameraMovement();
    }

    private void HandleMobileInput()
    {
        if (Input.touchCount == 2)
        {
            HandlePinchZoom();
            isDragging = false;
            return;
        }

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                if (!IsPointerOverUI(touch.position))
                {
                    inputStartPosition = touch.position;
                    lastInputPosition = touch.position;
                    isDragging = false;
                    wasDragging = false;
                }
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                if (!IsPointerOverUI(touch.position))
                {
                    float distance = Vector2.Distance(inputStartPosition, touch.position);
                    
                    if (!isDragging && distance > mobileSwipeThreshold)
                    {
                        isDragging = true;
                        wasDragging = true;
                        lastInputPosition = touch.position;
                    }
                    
                    if (isDragging)
                    {
                        Vector2 delta = touch.position - lastInputPosition;
                        PanCamera(delta);
                        lastInputPosition = touch.position;
                    }
                }
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                if (!isDragging && !wasDragging)
                {
                    if (!IsPointerOverUI(touch.position))
                    {
                        float distance = Vector2.Distance(inputStartPosition, touch.position);
                        if (distance < mobileSwipeThreshold)
                        {
                            RaycastToTile(touch.position);
                        }
                    }
                }
                isDragging = false;
            }
            else if (touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
            }
        }
        else
        {
            if (wasDragging)
            {
                wasDragging = false;
            }
            isDragging = false;
            isPinching = false;
        }
    }

    private void HandlePinchZoom()
    {
        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
        {
            touch0StartPos = touch0.position;
            touch1StartPos = touch1.position;
            initialPinchDistance = Vector2.Distance(touch0StartPos, touch1StartPos);
            isPinching = true;
        }
        else if (isPinching && (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved))
        {
            float currentDistance = Vector2.Distance(touch0.position, touch1.position);
            float deltaDistance = initialPinchDistance - currentDistance;
            
            float zoomDelta = (deltaDistance / Screen.height) * zoomSpeed * 3f;
            targetZoom = Mathf.Clamp(targetZoom + zoomDelta, minZoom, maxZoom);
            
            initialPinchDistance = currentDistance;
        }
    }

    private void HandlePCInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUI(Input.mousePosition))
            {
                inputStartPosition = Input.mousePosition;
                lastInputPosition = Input.mousePosition;
                isDragging = false;
                wasDragging = false;
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (!IsPointerOverUI(Input.mousePosition))
            {
                float distance = Vector2.Distance(inputStartPosition, Input.mousePosition);
                
                if (!isDragging && distance > pcSwipeThreshold)
                {
                    isDragging = true;
                    wasDragging = true;
                    lastInputPosition = Input.mousePosition;
                }
                
                if (isDragging)
                {
                    Vector2 delta = (Vector2)Input.mousePosition - lastInputPosition;
                    PanCamera(delta);
                    lastInputPosition = Input.mousePosition;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (!isDragging && !wasDragging)
            {
                if (!IsPointerOverUI(Input.mousePosition))
                {
                    float distance = Vector2.Distance(inputStartPosition, Input.mousePosition);
                    if (distance < pcSwipeThreshold)
                    {
                        RaycastToTile(Input.mousePosition);
                    }
                }
            }
            isDragging = false;
        }

        float scrollDelta = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            targetZoom = Mathf.Clamp(targetZoom - scrollDelta * scrollZoomSpeed, minZoom, maxZoom);
        }

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            targetZoom = Mathf.Clamp(targetZoom * (1f - pcZoomStep), minZoom, maxZoom);
        }
        else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            targetZoom = Mathf.Clamp(targetZoom * (1f + pcZoomStep), minZoom, maxZoom);
        }
    }

    private void PanCamera(Vector2 screenDelta)
    {
        float zoomFactor = targetZoom * panSpeed;
        
        Vector3 cameraRight = cameraTransform.right;
        Vector3 cameraForward = cameraTransform.forward;
        
        cameraRight.y = 0;
        cameraForward.y = 0;
        cameraRight.Normalize();
        cameraForward.Normalize();
        
        Vector3 move = (-cameraRight * screenDelta.x - cameraForward * screenDelta.y) * zoomFactor;
        
        targetCameraPosition += move;
        targetCameraPosition.y = initialCameraY;
        
        if (boundsCalculated)
        {
            targetCameraPosition.x = Mathf.Clamp(targetCameraPosition.x, boardBoundsMin.x, boardBoundsMax.x);
            targetCameraPosition.z = Mathf.Clamp(targetCameraPosition.z, boardBoundsMin.y, boardBoundsMax.y);
        }
    }

    private void ApplyCameraMovement()
    {
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            cameraTransform.position, 
            targetCameraPosition, 
            ref cameraPanVelocity, 
            panSmoothTime
        );
        
        smoothedPosition.y = initialCameraY;
        cameraTransform.position = smoothedPosition;
        
        mainCamera.orthographicSize = Mathf.SmoothDamp(
            mainCamera.orthographicSize, 
            targetZoom, 
            ref zoomVelocity, 
            zoomSmoothTime
        );
    }

    private void RaycastToTile(Vector3 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            HexTile tile = hit.collider.GetComponent<HexTile>();
            if (tile != null && GameManager.Instance != null)
            {
                GameManager.Instance.OnTileClicked(tile);
            }
        }
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (Input.touchCount > 0)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = screenPosition;
            
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            return results.Count > 0;
        }
        else
        {
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}