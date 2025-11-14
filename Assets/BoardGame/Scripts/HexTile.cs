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
    [SerializeField] private float animalHeightOffset = 0f;
    
    [Header("Initial Animal Y Offsets")]
    [SerializeField] private float initialTigerYOffset = 0f;
    [SerializeField] private float initialRabbitYOffset = 0f;
    [SerializeField] private float initialDragonYOffset = 0f;

    // State
    private Player owner = Player.None;
    private AnimalType animalType = AnimalType.None;
    private GameObject currentAnimal;
    private Vector3 originalScale;
    private bool isHovered = false;
    private bool isInitialAnimal = false; // Чи це стартова тварина

    public Player Owner => owner;
    public AnimalType Animal => animalType;
    public bool IsEmpty => owner == Player.None;

    private void Awake()
    {
        if (tileRenderer == null)
            tileRenderer = GetComponent<MeshRenderer>();
        
        originalScale = transform.localScale;
    }

    public void SetState(Player newOwner, AnimalType animal, bool isInitial = false)
    {
        owner = newOwner;
        animalType = animal;
        isInitialAnimal = isInitial;
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

        yield return new WaitForSeconds(0.1f);

        Vector3 finalPosition = currentAnimal.transform.position;
        Vector3 startPos = finalPosition + Vector3.up * 5f;
        
        currentAnimal.transform.position = startPos;
        currentAnimal.transform.localScale = Vector3.zero;

        currentAnimal.transform.DOMove(finalPosition, duration * 0.7f).SetEase(Ease.OutBounce);
        currentAnimal.transform.DOScale(Vector3.one, duration * 0.7f).SetEase(Ease.OutBack);
        currentAnimal.transform.DORotate(new Vector3(0, 360, 0), duration * 0.7f, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuad);

        tileRenderer.transform.DOPunchScale(Vector3.one * 0.2f, duration * 0.5f, 5, 0.5f);

        yield return new WaitForSeconds(duration);
    }

    public IEnumerator PlayFlipAnimation(Player newOwner, float duration)
    {
        if (currentAnimal == null) yield break;

        Vector3 currentCorrectPosition = currentAnimal.transform.position;

        owner = newOwner;

        Vector3 middleScale = new Vector3(0.1f, 1f, 1f);
        
        Sequence flipSequence = DOTween.Sequence();
        
        flipSequence.Append(currentAnimal.transform.DOScale(middleScale, duration * 0.5f).SetEase(Ease.InQuad));
        
        flipSequence.AppendCallback(() =>
        {
            UpdateOutline();
            UpdateTileMaterial();
            currentAnimal.transform.position = currentCorrectPosition;
        });
        
        flipSequence.Append(currentAnimal.transform.DOScale(Vector3.one, duration * 0.5f).SetEase(Ease.OutBack));
        
        currentAnimal.transform.DORotate(new Vector3(0, 180, 0), duration, RotateMode.FastBeyond360)
            .SetEase(Ease.InOutQuad)
            .SetRelative(true);

        yield return flipSequence.WaitForCompletion();
        
        currentAnimal.transform.position = currentCorrectPosition;
    }

    private void UpdateVisuals()
    {
        UpdateTileMaterial();
        UpdateAnimal();
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
            Vector3 tempPosition = transform.position + Vector3.up * 2f;
            currentAnimal = Instantiate(prefab, tempPosition, Quaternion.identity, transform);
            
            StartCoroutine(PositionAnimalAfterPhysicsUpdate());
        }
    }

    private IEnumerator PositionAnimalAfterPhysicsUpdate()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        
        if (currentAnimal != null)
        {
            Vector3 correctPosition = GetAnimalGroundPosition();
            currentAnimal.transform.position = correctPosition;
            
            Debug.Log($"[{animalType}] Final correct position: {correctPosition}");
            
            UpdateOutline();
        }
    }

    private Vector3 GetAnimalGroundPosition()
    {
        Vector3 tileTop = transform.position;
        
        if (tileRenderer != null)
        {
            tileTop.y = tileRenderer.bounds.max.y;
        }

        Debug.Log($"[{animalType}] Tile top Y: {tileTop.y}");

        if (currentAnimal == null)
        {
            return tileTop;
        }

        // Для стартових тварин використовуємо прості офсети
        if (isInitialAnimal)
        {
            float yOffset = animalType switch
            {
                AnimalType.Tiger => initialTigerYOffset,
                AnimalType.Rabbit => initialRabbitYOffset,
                AnimalType.Dragon => initialDragonYOffset,
                _ => 0f
            };

            Vector3 finalPosition = tileTop;
            finalPosition.y = tileTop.y + yOffset;

            Debug.Log($"[{animalType}] Initial animal - using simple offset: {yOffset}, final Y: {finalPosition.y}");

            return finalPosition;
        }

        // Для тварин що створюються під час гри - використовуємо складну логіку з колайдерами
        float lowestWorldY = float.MaxValue;
        BoxCollider[] colliders = currentAnimal.GetComponentsInChildren<BoxCollider>();
        
        if (colliders.Length == 0)
        {
            Debug.LogWarning($"No BoxCollider on {animalType}!");
            return tileTop;
        }

        foreach (var collider in colliders)
        {
            float colliderBottom = collider.bounds.min.y;
            Debug.Log($"[{animalType}] Collider '{collider.name}': bounds.min.y = {colliderBottom}");
            
            if (colliderBottom < lowestWorldY)
            {
                lowestWorldY = colliderBottom;
            }
        }

        Debug.Log($"[{animalType}] Lowest world Y: {lowestWorldY}");
        Debug.Log($"[{animalType}] Animal center Y: {currentAnimal.transform.position.y}");

        float offsetFromCenter = currentAnimal.transform.position.y - lowestWorldY;
        
        Debug.Log($"[{animalType}] Offset from center: {offsetFromCenter}");

        Vector3 finalPos = tileTop;
        finalPos.y = tileTop.y + offsetFromCenter + animalHeightOffset;

        Debug.Log($"[{animalType}] Final position Y: {finalPos.y}");

        return finalPos;
    }

    private void UpdateOutline()
    {
        if (currentAnimal == null || owner == Player.None) return;

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
        transform.DOKill();
        if (currentAnimal != null)
            currentAnimal.transform.DOKill();
        if (tileRenderer != null && tileRenderer.material != null)
            tileRenderer.material.DOKill();
    }
}