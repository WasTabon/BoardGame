using UnityEngine;

public class HexTile : MonoBehaviour
{
    [Header("Setup")]
    public HexCoordinates coordinates;
    public MeshRenderer tileRenderer;
    
    [Header("Materials")]
    public Material emptyMaterial;
    public Material playerAMaterial;
    public Material playerBMaterial;

    [Header("Animal Prefabs")]
    public GameObject tigerPrefab;
    public GameObject rabbitPrefab;
    public GameObject dragonPrefab;

    [Header("Outline Colors")]
    public Color playerAOutlineColor = Color.blue;
    public Color playerBOutlineColor = Color.red;
    public float outlineWidth = 0.05f;

    // State
    private Player owner = Player.None;
    private AnimalType animalType = AnimalType.None;
    private GameObject currentAnimal;
    private Material outlineMaterial;

    public Player Owner => owner;
    public AnimalType Animal => animalType;
    public bool IsEmpty => owner == Player.None;

    private void Awake()
    {
        if (tileRenderer == null)
            tileRenderer = GetComponent<MeshRenderer>();
    }

    public void SetState(Player newOwner, AnimalType animal)
    {
        owner = newOwner;
        animalType = animal;
        UpdateVisuals();
    }

    public void FlipOwner(Player newOwner)
    {
        if (owner == Player.None) return;
        owner = newOwner;
        UpdateOutline();
        UpdateTileMaterial();
    }

    private void UpdateVisuals()
    {
        UpdateTileMaterial();
        UpdateAnimal();
        UpdateOutline();
    }

    private void UpdateTileMaterial()
    {
        if (tileRenderer == null) return;

        switch (owner)
        {
            case Player.None:
                tileRenderer.material = emptyMaterial;
                break;
            case Player.PlayerA:
                tileRenderer.material = playerAMaterial;
                break;
            case Player.PlayerB:
                tileRenderer.material = playerBMaterial;
                break;
        }
    }

    private void UpdateAnimal()
    {
        // Remove old animal
        if (currentAnimal != null)
            Destroy(currentAnimal);

        if (animalType == AnimalType.None || owner == Player.None)
            return;

        // Spawn new animal
        GameObject prefab = null;
        switch (animalType)
        {
            case AnimalType.Tiger:
                prefab = tigerPrefab;
                break;
            case AnimalType.Rabbit:
                prefab = rabbitPrefab;
                break;
            case AnimalType.Dragon:
                prefab = dragonPrefab;
                break;
        }

        if (prefab != null)
        {
            currentAnimal = Instantiate(prefab, transform.position + Vector3.up * 0.5f, Quaternion.identity, transform);
            UpdateOutline();
        }
    }

    private void UpdateOutline()
    {
        if (currentAnimal == null || owner == Player.None) return;

        // Используем Outline shader или создаем дубликат меша с увеличенным масштабом
        Renderer animalRenderer = currentAnimal.GetComponent<Renderer>();
        if (animalRenderer != null)
        {
            Material[] mats = animalRenderer.materials;
            
            // Если у модели уже есть outline material, обновляем его цвет
            // Иначе добавляем новый материал с outline shader
            Color outlineColor = owner == Player.PlayerA ? playerAOutlineColor : playerBOutlineColor;
            
            // Простой способ: меняем emission color
            foreach (var mat in mats)
            {
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", outlineColor * 2f);
            }
        }
    }
    
    private void CreateOutlineWithLineRenderer()
    {
        LineRenderer lr = currentAnimal.AddComponent<LineRenderer>();
        lr.positionCount = 8;
        lr.loop = true;
        lr.startWidth = outlineWidth;
        lr.endWidth = outlineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = owner == Player.PlayerA ? playerAOutlineColor : playerBOutlineColor;
        lr.endColor = lr.startColor;
    
        // Создай позиции вокруг модели
        Bounds bounds = currentAnimal.GetComponent<Renderer>().bounds;
        // ... расставь точки по кругу
    }

    // Для клика по тайлу
    private void OnMouseDown()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnTileClicked(this);
    }

    // Highlight для мобильных устройств
    public void SetHighlight(bool active)
    {
        if (tileRenderer == null) return;
        // Можно добавить эффект свечения
    }
}