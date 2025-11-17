using UnityEngine;
using UnityEngine.EventSystems;

public class TouchInput : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Мобильный тач
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                // Проверяем, не попали ли мы по UI
                if (!IsPointerOverUI(touch.position))
                {
                    RaycastToTile(touch.position);
                }
            }
        }
        // ПК клик
        else if (Input.GetMouseButtonDown(0))
        {
            // Проверяем, не попали ли мы по UI
            if (!IsPointerOverUI(Input.mousePosition))
            {
                RaycastToTile(Input.mousePosition);
            }
        }
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

    /// <summary>
    /// Проверяет, находится ли указатель над UI элементом
    /// </summary>
    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        // Для мобильных устройств (тач)
        if (Input.touchCount > 0)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = screenPosition;
            
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            return results.Count > 0;
        }
        // Для ПК (мышь)
        else
        {
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}