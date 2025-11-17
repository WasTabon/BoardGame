using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using DG.Tweening;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Setup")]
    public int boardRadius = 3;
    public GameObject hexTilePrefab;
    public Transform boardParent;

    [Header("UI References")]
    public Button btnTiger;
    public Button btnRabbit;
    public Button btnDragon;
    public TextMeshProUGUI txtCurrentPlayer;
    public TextMeshProUGUI txtScoreA;
    public TextMeshProUGUI txtScoreB;
    public GameObject victoryPanel;
    public TextMeshProUGUI txtVictoryMessage;
    public Button btnRestart;
    public GameObject modeSelectionPanel;
    public Button btnPvP;
    public Button btnVsAI;
    public GameObject currentPlayerPanel;
    public GameObject animalSelectionPanel;
    
    [Header("Field Size Panel")]
    public GameObject fieldSizeChoosePanel;
    public TextMeshProUGUI txtFieldSize;
    public Button btnFieldSizePlus;
    public Button btnFieldSizeMinus;

    [Header("Animation Settings")]
    [SerializeField] private bool enablePlacementAnimation = true;
    [SerializeField] private bool enableFlipAnimation = true;
    [SerializeField] private bool enableUIAnimation = true;
    [SerializeField] private bool enableCameraShake = true;
    [SerializeField] private bool enableCameraZoom = true;
    [SerializeField] private bool enableParticles = true;
    [SerializeField] private bool enableSounds = true;

    [Header("Animation Timing")]
    [SerializeField] private float placementDuration = 0.4f;
    [SerializeField] private float flipDuration = 0.35f;
    [SerializeField] private float uiAnimDuration = 0.25f;
    [SerializeField] private float delayBetweenFlips = 0.05f;

    [Header("Camera Animation")]
    [SerializeField] private float shakeStrength = 0.2f;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float zoomAmount = 0.8f;
    [SerializeField] private float zoomDuration = 0.3f;

    [Header("Audio & Effects")]
    [SerializeField] private AudioClip placementSound;
    [SerializeField] private AudioClip flipSound;
    [SerializeField] private AudioClip captureSound;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private GameObject placementParticles;
    [SerializeField] private GameObject captureParticles;

    [Header("Hex Spacing")]
    [SerializeField] private float hexSpacingMultiplier = 1.05f; // Додатковий відступ між тайлами (5%)

    [Header("Game State")]
    private Dictionary<HexCoordinates, HexTile> board = new Dictionary<HexCoordinates, HexTile>();
    private Player currentPlayer = Player.PlayerA;
    private AnimalType selectedAnimal = AnimalType.Tiger;
    private bool isAIMode = false;
    private bool gameEnded = false;
    private bool isAnimating = false;
    private int minFieldSize = 1;
    private int maxFieldSize = 5;
    private float calculatedHexSize = 1f; // Автоматично розрахований розмір

    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private float originalCameraSize;

    public float HexSize => calculatedHexSize; // Публічний доступ для інших скриптів

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.position;
            originalCameraSize = mainCamera.orthographicSize;
        }

        CalculateHexSize();
        SetupUI();
        ShowInitialPanels();
    }

    private void CalculateHexSize()
    {
        if (hexTilePrefab == null)
        {
            Debug.LogError("HexTilePrefab is not assigned!");
            calculatedHexSize = 1f;
            return;
        }

        // Створюємо тимчасовий об'єкт для вимірювання
        GameObject tempHex = Instantiate(hexTilePrefab, Vector3.zero, Quaternion.identity);
        
        // Шукаємо MeshFilter або MeshRenderer
        MeshFilter meshFilter = tempHex.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = tempHex.GetComponent<MeshRenderer>();
        
        if (meshRenderer != null)
        {
            // Використовуємо bounds renderer'а
            Bounds bounds = meshRenderer.bounds;
            
            // Для правильного hexagon spacing потрібна відстань від центру до вершини
            // Це максимальна з ширини або глибини
            float width = bounds.size.x;
            float depth = bounds.size.z;
            
            // Для flat-top hexagon (як у нас) беремо більшу з координат
            calculatedHexSize = Mathf.Max(width, depth) / 2f;
            
            // Додаємо spacing multiplier для запобігання накладанню
            calculatedHexSize *= hexSpacingMultiplier;
            
            Debug.Log($"Calculated hex size: {calculatedHexSize} (bounds: {width}x{depth})");
        }
        else if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            // Якщо є тільки MeshFilter
            Bounds bounds = meshFilter.sharedMesh.bounds;
            float width = bounds.size.x * tempHex.transform.localScale.x;
            float depth = bounds.size.z * tempHex.transform.localScale.z;
            
            calculatedHexSize = Mathf.Max(width, depth) / 2f;
            calculatedHexSize *= hexSpacingMultiplier;
            
            Debug.Log($"Calculated hex size from mesh: {calculatedHexSize}");
        }
        else
        {
            Debug.LogWarning("Could not find MeshFilter or MeshRenderer on hex prefab. Using default size.");
            calculatedHexSize = 1f;
        }
        
        Destroy(tempHex);
    }

    private void SetupUI()
    {
        btnTiger.onClick.AddListener(() => SelectAnimal(AnimalType.Tiger));
        btnRabbit.onClick.AddListener(() => SelectAnimal(AnimalType.Rabbit));
        btnDragon.onClick.AddListener(() => SelectAnimal(AnimalType.Dragon));
        btnRestart.onClick.AddListener(RestartGame);
        btnPvP.onClick.AddListener(() => StartGame(false));
        btnVsAI.onClick.AddListener(() => StartGame(true));
        
        btnFieldSizePlus.onClick.AddListener(() => ChangeFieldSize(1));
        btnFieldSizeMinus.onClick.AddListener(() => ChangeFieldSize(-1));

        if (enableUIAnimation)
        {
            AddButtonAnimation(btnTiger);
            AddButtonAnimation(btnRabbit);
            AddButtonAnimation(btnDragon);
            AddButtonAnimation(btnRestart);
            AddButtonAnimation(btnPvP);
            AddButtonAnimation(btnVsAI);
            AddButtonAnimation(btnFieldSizePlus);
            AddButtonAnimation(btnFieldSizeMinus);
        }

        SelectAnimal(AnimalType.Tiger);
        victoryPanel.SetActive(false);
        currentPlayerPanel.SetActive(false);
        animalSelectionPanel.SetActive(false);
    }

    private void ShowInitialPanels()
    {
        modeSelectionPanel.SetActive(true);
        fieldSizeChoosePanel.SetActive(true);
        
        UpdateFieldSizeText();
        
        if (enableUIAnimation)
        {
            modeSelectionPanel.transform.localScale = Vector3.zero;
            modeSelectionPanel.transform.DOScale(1f, uiAnimDuration * 2f).SetEase(Ease.OutBack);
            
            fieldSizeChoosePanel.transform.localScale = Vector3.zero;
            fieldSizeChoosePanel.transform.DOScale(1f, uiAnimDuration * 2f).SetEase(Ease.OutBack).SetDelay(0.1f);
        }
    }

    private void ChangeFieldSize(int delta)
    {
        boardRadius = Mathf.Clamp(boardRadius + delta, minFieldSize, maxFieldSize);
        UpdateFieldSizeText();
    }

    private void UpdateFieldSizeText()
    {
        txtFieldSize.text = boardRadius.ToString();
        
        if (enableUIAnimation)
        {
            txtFieldSize.transform.DOPunchScale(Vector3.one * 0.3f, uiAnimDuration, 5, 0.5f);
        }
    }

    private void AddButtonAnimation(Button button)
    {
        button.onClick.AddListener(() =>
        {
            PlaySound(buttonClickSound);
            button.transform.DOScale(0.9f, uiAnimDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    button.transform.DOScale(1f, uiAnimDuration * 0.5f).SetEase(Ease.OutBack);
                });
        });
    }

    private void StartGame(bool aiMode)
    {
        isAIMode = aiMode;
        
        if (enableUIAnimation)
        {
            Sequence hideSequence = DOTween.Sequence();
            
            hideSequence.Append(modeSelectionPanel.transform.DOScale(0f, uiAnimDuration).SetEase(Ease.InBack));
            hideSequence.Join(fieldSizeChoosePanel.transform.DOScale(0f, uiAnimDuration).SetEase(Ease.InBack));
            
            hideSequence.OnComplete(() =>
            {
                modeSelectionPanel.SetActive(false);
                fieldSizeChoosePanel.SetActive(false);
                StartCoroutine(GenerateBoardAndShowUI());
            });
        }
        else
        {
            modeSelectionPanel.SetActive(false);
            fieldSizeChoosePanel.SetActive(false);
            StartCoroutine(GenerateBoardAndShowUI());
        }
    }

    private IEnumerator GenerateBoardAndShowUI()
    {
        GenerateBoard();
        SetupStartingPositions();
        
        yield return new WaitForSeconds(placementDuration);
        
        animalSelectionPanel.SetActive(true);
        currentPlayerPanel.SetActive(true);
        
        if (enableUIAnimation)
        {
            animalSelectionPanel.transform.localScale = Vector3.zero;
            animalSelectionPanel.transform.DOScale(1f, uiAnimDuration * 1.5f).SetEase(Ease.OutBack);
            
            currentPlayerPanel.transform.localScale = Vector3.zero;
            currentPlayerPanel.transform.DOScale(1f, uiAnimDuration * 1.5f).SetEase(Ease.OutBack).SetDelay(0.1f);
        }
        
        UpdateUI();
    }

    private void GenerateBoard()
    {
        board.Clear();
        
        foreach (Transform child in boardParent)
            Destroy(child.gameObject);

        for (int q = -boardRadius; q <= boardRadius; q++)
        {
            int r1 = Mathf.Max(-boardRadius, -q - boardRadius);
            int r2 = Mathf.Min(boardRadius, -q + boardRadius);

            for (int r = r1; r <= r2; r++)
            {
                HexCoordinates coord = new HexCoordinates(q, r);
                Vector3 position = HexToWorld(coord);

                GameObject tileObj = Instantiate(hexTilePrefab, position, Quaternion.identity, boardParent);
                HexTile tile = tileObj.GetComponent<HexTile>();
                
                if (tile != null)
                {
                    tile.coordinates = coord;
                    board[coord] = tile;

                    if (enablePlacementAnimation)
                    {
                        tile.transform.localScale = Vector3.zero;
                        float delay = Vector3.Distance(position, Vector3.zero) * 0.02f;
                        tile.transform.DOScale(1f, placementDuration * 0.8f)
                            .SetDelay(delay)
                            .SetEase(Ease.OutBack);
                    }
                }
            }
        }
    }

    private Vector3 HexToWorld(HexCoordinates hex)
    {
        // Використовуємо розрахований розмір hex
        float x = calculatedHexSize * (Mathf.Sqrt(3f) * hex.q + Mathf.Sqrt(3f) / 2f * hex.r);
        float z = calculatedHexSize * (3f / 2f * hex.r);
        return new Vector3(x, 0, z);
    }

    private void SetupStartingPositions()
    {
        HexCoordinates[] startPositions = new HexCoordinates[]
        {
            new HexCoordinates(0, 0),
            new HexCoordinates(1, 0),
            new HexCoordinates(0, -1),
            new HexCoordinates(1, -1)
        };

        Player[] startPlayers = new Player[] { Player.PlayerA, Player.PlayerB, Player.PlayerA, Player.PlayerB };
        AnimalType[] startAnimals = new AnimalType[] { AnimalType.Tiger, AnimalType.Tiger, AnimalType.Rabbit, AnimalType.Dragon };

        for (int i = 0; i < startPositions.Length; i++)
        {
            if (board.TryGetValue(startPositions[i], out HexTile tile))
            {
                tile.SetState(startPlayers[i], startAnimals[i], isInitial: true);
            }
        }
    }

    private void SelectAnimal(AnimalType animal)
    {
        selectedAnimal = animal;
        
        UpdateAnimalButtonVisuals(btnTiger, animal == AnimalType.Tiger);
        UpdateAnimalButtonVisuals(btnRabbit, animal == AnimalType.Rabbit);
        UpdateAnimalButtonVisuals(btnDragon, animal == AnimalType.Dragon);
    }

    private void UpdateAnimalButtonVisuals(Button button, bool selected)
    {
        Image img = button.GetComponent<Image>();
        Color targetColor = selected ? Color.yellow : Color.white;
        
        if (enableUIAnimation)
        {
            img.DOColor(targetColor, uiAnimDuration);
            if (selected)
            {
                button.transform.DOPunchScale(Vector3.one * 0.2f, uiAnimDuration, 5, 0.5f);
            }
        }
        else
        {
            img.color = targetColor;
        }
    }

    public void OnTileClicked(HexTile tile)
    {
        if (gameEnded || isAnimating) return;
        if (isAIMode && currentPlayer == Player.PlayerB) return;
        if (!tile.IsEmpty) return;

        StartCoroutine(PlacePieceWithAnimation(tile, currentPlayer, selectedAnimal));
    }

    private IEnumerator PlacePieceWithAnimation(HexTile tile, Player player, AnimalType animal)
    {
        isAnimating = true;

        tile.SetState(player, animal);
        
        PlaySound(placementSound);
        SpawnParticles(placementParticles, tile.transform.position);
        
        if (enablePlacementAnimation)
        {
            yield return StartCoroutine(tile.PlayPlacementAnimation(placementDuration));
        }

        List<HexTile> outflankedTiles = GetOutflankedTiles(tile.coordinates, player);
        List<HexTile> dominatedTiles = GetDominatedTiles(tile.coordinates, player, animal);
        List<HexTile> allCaptured = new List<HexTile>();
        allCaptured.AddRange(outflankedTiles);
        allCaptured.AddRange(dominatedTiles);

        if (allCaptured.Count > 0)
        {
            if (enableCameraShake && mainCamera != null)
            {
                mainCamera.transform.DOShakePosition(shakeDuration, shakeStrength, 10, 90, false, true);
            }

            PlaySound(captureSound);

            if (enableFlipAnimation)
            {
                foreach (var capturedTile in allCaptured)
                {
                    StartCoroutine(capturedTile.PlayFlipAnimation(player, flipDuration));
                    SpawnParticles(captureParticles, capturedTile.transform.position);
                    yield return new WaitForSeconds(delayBetweenFlips);
                }
            }
            else
            {
                foreach (var capturedTile in allCaptured)
                {
                    capturedTile.FlipOwner(player);
                }
            }
        }

        if (enableCameraZoom && allCaptured.Count >= 3 && mainCamera != null)
        {
            float targetSize = originalCameraSize * zoomAmount;
            mainCamera.DOOrthoSize(targetSize, zoomDuration * 0.5f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                mainCamera.DOOrthoSize(originalCameraSize, zoomDuration * 0.5f).SetEase(Ease.InQuad);
            });
        }

        SwitchPlayer();
        UpdateUI();

        CheckGameEnd();

        isAnimating = false;

        if (isAIMode && currentPlayer == Player.PlayerB && !gameEnded)
        {
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(AITurnCoroutine());
        }
    }

    private List<HexTile> GetOutflankedTiles(HexCoordinates placed, Player player)
    {
        List<HexTile> captured = new List<HexTile>();
        HexCoordinates[] directions = HexCoordinates.GetDirections();

        foreach (var dir in directions)
        {
            List<HexTile> line = new List<HexTile>();
            HexCoordinates current = placed + dir;

            while (board.TryGetValue(current, out HexTile tile))
            {
                if (tile.Owner == Player.None)
                    break;
                
                if (tile.Owner != player)
                {
                    line.Add(tile);
                    current = current + dir;
                }
                else
                {
                    captured.AddRange(line);
                    break;
                }
            }
        }

        return captured;
    }

    private List<HexTile> GetDominatedTiles(HexCoordinates placed, Player player, AnimalType attackerAnimal)
    {
        List<HexTile> captured = new List<HexTile>();
        HexCoordinates[] neighbors = HexCoordinates.GetNeighbors(placed);

        foreach (var neighborCoord in neighbors)
        {
            if (board.TryGetValue(neighborCoord, out HexTile tile))
            {
                if (tile.Owner != Player.None && tile.Owner != player)
                {
                    if (AnimalRules.Dominates(attackerAnimal, tile.Animal))
                    {
                        captured.Add(tile);
                    }
                }
            }
        }

        return captured;
    }

    private void SwitchPlayer()
    {
        currentPlayer = currentPlayer == Player.PlayerA ? Player.PlayerB : Player.PlayerA;
    }

    private void UpdateUI()
    {
        string playerAName = "Player A";
        string playerBName = isAIMode ? "Bot" : "Player B";
        
        string currentPlayerName = currentPlayer == Player.PlayerA ? playerAName : playerBName;
        txtCurrentPlayer.text = $"{currentPlayerName} Turn";
        
        int scoreA = board.Values.Count(t => t.Owner == Player.PlayerA);
        int scoreB = board.Values.Count(t => t.Owner == Player.PlayerB);
        
        if (enableUIAnimation)
        {
            AnimateScoreChange(txtScoreA, scoreA);
            AnimateScoreChange(txtScoreB, scoreB);
        }
        else
        {
            txtScoreA.text = $"{scoreA}";
            txtScoreB.text = $"{scoreB}";
        }
    }

    private void AnimateScoreChange(TextMeshProUGUI scoreText, int newScore)
    {
        int currentScore = int.Parse(scoreText.text);
        if (currentScore != newScore)
        {
            scoreText.transform.DOPunchScale(Vector3.one * 0.3f, uiAnimDuration, 5, 0.5f);
        }
        
        DOTween.To(() => currentScore, x =>
        {
            currentScore = x;
            scoreText.text = currentScore.ToString();
        }, newScore, uiAnimDuration);
    }

    private void CheckGameEnd()
    {
        bool hasEmptyTiles = board.Values.Any(t => t.IsEmpty);

        if (!hasEmptyTiles)
        {
            StartCoroutine(EndGameWithAnimation());
        }
    }

    private IEnumerator EndGameWithAnimation()
    {
        gameEnded = true;
        yield return new WaitForSeconds(0.5f);

        int scoreA = board.Values.Count(t => t.Owner == Player.PlayerA);
        int scoreB = board.Values.Count(t => t.Owner == Player.PlayerB);

        string playerAName = isAIMode ? "Player" : "Player A";
        string playerBName = isAIMode ? "Bot" : "Player B";
        
        string winner = scoreA > scoreB ? $"{playerAName} Wins!" : 
                       scoreB > scoreA ? $"{playerBName} Wins!" : 
                       "Draw!";
        txtVictoryMessage.text = $"{winner}\nScore: {scoreA} - {scoreB}";
        
        victoryPanel.SetActive(true);

        if (enableUIAnimation)
        {
            victoryPanel.transform.localScale = Vector3.zero;
            victoryPanel.transform.DOScale(1f, uiAnimDuration * 2f).SetEase(Ease.OutElastic);
        }
    }

    private IEnumerator AITurnCoroutine()
    {
        isAnimating = true;
        yield return new WaitForSeconds(0.3f);

        List<HexTile> emptyTiles = board.Values.Where(t => t.IsEmpty).ToList();
        
        if (emptyTiles.Count == 0)
        {
            yield return StartCoroutine(EndGameWithAnimation());
            isAnimating = false;
            yield break;
        }

        HexTile randomTile = emptyTiles[Random.Range(0, emptyTiles.Count)];
        AnimalType randomAnimal = (AnimalType)Random.Range(1, 4);

        yield return StartCoroutine(PlacePieceWithAnimation(randomTile, Player.PlayerB, randomAnimal));
    }

    private void RestartGame()
    {
        gameEnded = false;
        currentPlayer = Player.PlayerA;
        boardRadius = 3;
        
        if (enableUIAnimation)
        {
            Sequence hideSequence = DOTween.Sequence();
            
            hideSequence.Append(victoryPanel.transform.DOScale(0f, uiAnimDuration).SetEase(Ease.InBack));
            hideSequence.Join(currentPlayerPanel.transform.DOScale(0f, uiAnimDuration).SetEase(Ease.InBack));
            hideSequence.Join(animalSelectionPanel.transform.DOScale(0f, uiAnimDuration).SetEase(Ease.InBack));
            
            hideSequence.OnComplete(() =>
            {
                victoryPanel.SetActive(false);
                currentPlayerPanel.SetActive(false);
                animalSelectionPanel.SetActive(false);
                ShowInitialPanels();
            });
        }
        else
        {
            victoryPanel.SetActive(false);
            currentPlayerPanel.SetActive(false);
            animalSelectionPanel.SetActive(false);
            ShowInitialPanels();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (enableSounds && clip != null && MusicController.Instance != null)
        {
            MusicController.Instance.PlaySpecificSound(clip);
        }
    }

    private void SpawnParticles(GameObject particlePrefab, Vector3 position)
    {
        if (enableParticles && particlePrefab != null)
        {
            GameObject particles = Instantiate(particlePrefab, position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(particles, 2f);
        }
    }

    public bool IsAnimating => isAnimating;
}