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
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (!IsPointerOverUI(touch.position))
                {
                    RaycastToTile(touch.position);
                }
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
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