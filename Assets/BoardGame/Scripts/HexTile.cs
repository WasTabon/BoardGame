using UnityEngine;
using DG.Tweening;
using System.Collections;

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

    [Header("Animation")]
    [SerializeField] private bool enableHoverEffect = true;
    [SerializeField] private float hoverHeight = 0.1f;
    [SerializeField] private float hoverDuration = 0.2f;

    [Header("Animal Positioning")]
    [SerializeField] private float animalHeightOffset = 0f; // Додаткове зміщення якщо потрібно

    // State
    private Player owner = Player.None;
    private AnimalType animalType = AnimalType.None;
    private GameObject currentAnimal;
    private Vector3 originalScale;
    private bool isHovered = false;

    public Player Owner => owner;
    public AnimalType Animal => animalType;
    public bool IsEmpty => owner == Player.None;

    private void Awake()
    {
        if (tileRenderer == null)
            tileRenderer = GetComponent<MeshRenderer>();
        
        originalScale = transform.localScale;
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

    public IEnumerator PlayPlacementAnimation(float duration)
    {
        if (currentAnimal == null) yield break;

        // Отримуємо правильну позицію на поверхні тайла
        Vector3 finalPosition = GetAnimalGroundPosition();
        
        // Анімація "падення з неба"
        Vector3 startPos = finalPosition + Vector3.up * 5f;
        
        currentAnimal.transform.position = startPos;
        currentAnimal.transform.localScale = Vector3.zero;

        // Падення
        currentAnimal.transform.DOMove(finalPosition, duration * 0.7f).SetEase(Ease.OutBounce);
        
        // Появление с вращением
        currentAnimal.transform.DOScale(Vector3.one, duration * 0.7f).SetEase(Ease.OutBack);
        currentAnimal.transform.DORotate(new Vector3(0, 360, 0), duration * 0.7f, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuad);

        // Пульсация тайла
        tileRenderer.transform.DOPunchScale(Vector3.one * 0.2f, duration * 0.5f, 5, 0.5f);

        yield return new WaitForSeconds(duration);
    }

    public IEnumerator PlayFlipAnimation(Player newOwner, float duration)
    {
        if (currentAnimal == null) yield break;

        owner = newOwner;

        // Flip анимация на 180 градусов по Y
        Vector3 middleScale = new Vector3(0.1f, 1f, 1f);
        
        Sequence flipSequence = DOTween.Sequence();
        
        // Сжатие
        flipSequence.Append(currentAnimal.transform.DOScale(middleScale, duration * 0.5f).SetEase(Ease.InQuad));
        
        // Смена визуала в середине
        flipSequence.AppendCallback(() =>
        {
            UpdateOutline();
            UpdateTileMaterial();
        });
        
        // Расширение
        flipSequence.Append(currentAnimal.transform.DOScale(Vector3.one, duration * 0.5f).SetEase(Ease.OutBack));
        
        // Вращение
        currentAnimal.transform.DORotate(new Vector3(0, 180, 0), duration, RotateMode.FastBeyond360)
            .SetEase(Ease.InOutQuad)
            .SetRelative(true);

        yield return flipSequence.WaitForCompletion();
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

        Material targetMaterial = owner switch
        {
            Player.None => emptyMaterial,
            Player.PlayerA => playerAMaterial,
            Player.PlayerB => playerBMaterial,
            _ => emptyMaterial
        };

        tileRenderer.material = targetMaterial;
    }

    private void UpdateAnimal()
    {
        if (currentAnimal != null)
            Destroy(currentAnimal);

        if (animalType == AnimalType.None || owner == Player.None)
            return;

        GameObject prefab = animalType switch
        {
            AnimalType.Tiger => tigerPrefab,
            AnimalType.Rabbit => rabbitPrefab,
            AnimalType.Dragon => dragonPrefab,
            _ => null
        };

        if (prefab != null)
        {
            // Створюємо тварину на правильній висоті
            Vector3 spawnPosition = GetAnimalGroundPosition(prefab);
            currentAnimal = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
            UpdateOutline();
        }
    }

    private Vector3 GetAnimalGroundPosition(GameObject prefab = null)
{
    // Отримуємо верхню точку тайла
    Vector3 tileTop = transform.position;
    
    if (tileRenderer != null)
    {
        // Використовуємо bounds рендерера щоб знайти верхню точку
        tileTop.y = tileRenderer.bounds.max.y;
    }

    Debug.Log($"Tile top Y: {tileTop.y}");

    // Знаходимо найнижчу точку колайдера тварини
    float animalBottomOffset = 0f;

    if (currentAnimal != null)
    {
        animalBottomOffset = GetAnimalBottomOffset(currentAnimal);
    }
    else if (prefab != null)
    {
        // Якщо тварини ще немає, робимо попереднє інстанціювання для вимірювання
        GameObject tempAnimal = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        animalBottomOffset = GetAnimalBottomOffset(tempAnimal);
        Destroy(tempAnimal);
    }

    Debug.Log($"Animal bottom offset: {animalBottomOffset}");

    // ВИПРАВЛЕННЯ: віднімаємо offset замість додавання
    tileTop.y -= animalBottomOffset;

    // Додаємо користувацьке зміщення
    tileTop.y += animalHeightOffset;

    Debug.Log($"Final position Y: {tileTop.y}");

    return tileTop;
}

private float GetAnimalBottomOffset(GameObject animal)
{
    // Шукаємо всі Box Collider на тварині та її дочірніх об'єктах
    BoxCollider[] colliders = animal.GetComponentsInChildren<BoxCollider>();
    
    if (colliders.Length == 0)
    {
        Debug.LogWarning($"No BoxCollider found on animal {animal.name}. Animal positioning may be incorrect.");
        return 0f;
    }

    Debug.Log($"Found {colliders.Length} colliders on {animal.name}");

    // Знаходимо найнижчу точку серед всіх колайдерів
    float lowestPoint = float.MaxValue;
    
    foreach (var collider in colliders)
    {
        // Отримуємо bounds колайдера в world space
        Bounds bounds = collider.bounds;
        float bottomY = bounds.min.y;
        
        Debug.Log($"Collider {collider.name}: center={collider.bounds.center.y}, min={bottomY}");
        
        if (bottomY < lowestPoint)
        {
            lowestPoint = bottomY;
        }
    }

    // Повертаємо відстань від найнижчої точки до центру тварини
    if (lowestPoint != float.MaxValue)
    {
        float offset = lowestPoint - animal.transform.position.y;
        Debug.Log($"Lowest point: {lowestPoint}, Animal center: {animal.transform.position.y}, Offset: {offset}");
        return offset;
    }

    Debug.Log("No valid collider bounds found");
    
    return 0f;
}

    private void UpdateOutline()
    {
        if (currentAnimal == null || owner == Player.None) return;

        // Перевіряємо всі рендерери (включно з дочірніми)
        Renderer[] allRenderers = currentAnimal.GetComponentsInChildren<Renderer>();
        Color outlineColor = owner == Player.PlayerA ? playerAOutlineColor : playerBOutlineColor;
        
        foreach (var renderer in allRenderers)
        {
            if (renderer != null && renderer.materials != null)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat != null)
                    {
                        if (mat.HasProperty("_EmissionColor"))
                            mat.SetColor("_EmissionColor", outlineColor * 2f);
                        
                        if (mat.HasProperty("_Color"))
                        {
                            Color baseColor = mat.color;
                            mat.DOColor(Color.Lerp(baseColor, outlineColor, 0.3f), 0.3f);
                        }
                    }
                }
            }
        }
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsAnimating)
            GameManager.Instance.OnTileClicked(this);
    }

    private void OnMouseEnter()
    {
        if (!enableHoverEffect || !IsEmpty || GameManager.Instance.IsAnimating) return;

        isHovered = true;
        transform.DOMoveY(transform.position.y + hoverHeight, hoverDuration).SetEase(Ease.OutQuad);
        transform.DOScale(originalScale * 1.1f, hoverDuration).SetEase(Ease.OutQuad);
        
        if (tileRenderer != null)
        {
            tileRenderer.material.DOColor(Color.white * 1.3f, hoverDuration);
        }
    }

    private void OnMouseExit()
    {
        if (!enableHoverEffect || !isHovered) return;

        isHovered = false;
        transform.DOMoveY(transform.position.y - hoverHeight, hoverDuration).SetEase(Ease.InQuad);
        transform.DOScale(originalScale, hoverDuration).SetEase(Ease.InQuad);
        
        if (tileRenderer != null && owner == Player.None)
        {
            tileRenderer.material.DOColor(Color.white, hoverDuration);
        }
    }

    public void SetHighlight(bool active)
    {
        if (tileRenderer == null) return;
        
        Color highlightColor = active ? Color.yellow : Color.white;
        tileRenderer.material.DOColor(highlightColor, 0.2f);
    }

    private void OnDestroy()
    {
        // Очищаем все tweens связанные с этим объектом
        transform.DOKill();
        if (currentAnimal != null)
            currentAnimal.transform.DOKill();
        if (tileRenderer != null && tileRenderer.material != null)
            tileRenderer.material.DOKill();
    }
}