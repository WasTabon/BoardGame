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
    public float hexSize = 1f;
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

    [Header("Game State")]
    private Dictionary<HexCoordinates, HexTile> board = new Dictionary<HexCoordinates, HexTile>();
    private Player currentPlayer = Player.PlayerA;
    private AnimalType selectedAnimal = AnimalType.Tiger;
    private bool isAIMode = false;
    private bool gameEnded = false;
    private bool isAnimating = false;

    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private float originalCameraSize;

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

        SetupUI();
        ShowModeSelection();
    }

    private void SetupUI()
    {
        btnTiger.onClick.AddListener(() => SelectAnimal(AnimalType.Tiger));
        btnRabbit.onClick.AddListener(() => SelectAnimal(AnimalType.Rabbit));
        btnDragon.onClick.AddListener(() => SelectAnimal(AnimalType.Dragon));
        btnRestart.onClick.AddListener(RestartGame);
        btnPvP.onClick.AddListener(() => StartGame(false));
        btnVsAI.onClick.AddListener(() => StartGame(true));

        // Добавляем анимацию ко всем кнопкам
        if (enableUIAnimation)
        {
            AddButtonAnimation(btnTiger);
            AddButtonAnimation(btnRabbit);
            AddButtonAnimation(btnDragon);
            AddButtonAnimation(btnRestart);
            AddButtonAnimation(btnPvP);
            AddButtonAnimation(btnVsAI);
        }

        SelectAnimal(AnimalType.Tiger);
        victoryPanel.SetActive(false);
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

    private void ShowModeSelection()
    {
        modeSelectionPanel.SetActive(true);
        if (enableUIAnimation)
        {
            modeSelectionPanel.transform.localScale = Vector3.zero;
            modeSelectionPanel.transform.DOScale(1f, uiAnimDuration * 2f).SetEase(Ease.OutBack);
        }
    }

    private void StartGame(bool aiMode)
    {
        isAIMode = aiMode;
        
        if (enableUIAnimation)
        {
            modeSelectionPanel.transform.DOScale(0f, uiAnimDuration).SetEase(Ease.InBack).OnComplete(() =>
            {
                modeSelectionPanel.SetActive(false);
                GenerateBoard();
                SetupStartingPositions();
                UpdateUI();
            });
        }
        else
        {
            modeSelectionPanel.SetActive(false);
            GenerateBoard();
            SetupStartingPositions();
            UpdateUI();
        }
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

                    // Анимация появления доски
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
        float x = hexSize * (Mathf.Sqrt(3f) * hex.q + Mathf.Sqrt(3f) / 2f * hex.r);
        float z = hexSize * (3f / 2f * hex.r);
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
                tile.SetState(startPlayers[i], startAnimals[i]);
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

        // 1. Анимация размещения фишки
        tile.SetState(player, animal);
        
        if (enablePlacementAnimation)
        {
            yield return StartCoroutine(tile.PlayPlacementAnimation(placementDuration));
        }

        PlaySound(placementSound);
        SpawnParticles(placementParticles, tile.transform.position);

        // 2. Получаем захваченные фишки
        List<HexTile> outflankedTiles = GetOutflankedTiles(tile.coordinates, player);
        List<HexTile> dominatedTiles = GetDominatedTiles(tile.coordinates, player, animal);
        List<HexTile> allCaptured = new List<HexTile>();
        allCaptured.AddRange(outflankedTiles);
        allCaptured.AddRange(dominatedTiles);

        // 3. Анимация захвата
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

        // 4. Zoom эффект для важных ходов
        if (enableCameraZoom && allCaptured.Count >= 3 && mainCamera != null)
        {
            float targetSize = originalCameraSize * zoomAmount;
            mainCamera.DOOrthoSize(targetSize, zoomDuration * 0.5f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                mainCamera.DOOrthoSize(originalCameraSize, zoomDuration * 0.5f).SetEase(Ease.InQuad);
            });
        }

        // 5. Обновление UI и смена хода
        SwitchPlayer();
        UpdateUI();

        // 6. Проверка конца игры
        CheckGameEnd();

        isAnimating = false;

        // 7. AI ход
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
        txtCurrentPlayer.text = $"{currentPlayer} Turn";
        
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

        string winner = scoreA > scoreB ? "Player A Wins!" : scoreB > scoreA ? "Player B Wins!" : "Draw!";
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
        
        if (enableUIAnimation)
        {
            victoryPanel.transform.DOScale(0f, uiAnimDuration).SetEase(Ease.InBack).OnComplete(() =>
            {
                victoryPanel.SetActive(false);
                ShowModeSelection();
            });
        }
        else
        {
            victoryPanel.SetActive(false);
            ShowModeSelection();
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