using UnityEngine;

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
                RaycastToTile(touch.position);
            }
        }
        // ПК клик (дублирование для надежности)
        else if (Input.GetMouseButtonDown(0))
        {
            RaycastToTile(Input.mousePosition);
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
}