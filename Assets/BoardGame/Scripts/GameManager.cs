using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Setup")]
    public int boardRadius = 3; // 3 for 7x7, 4 for 9x9
    public GameObject hexTilePrefab;
    public float hexSize = 1f;
    public Transform boardParent;

    [Header("UI References")]
    public Button btnTiger;
    public Button btnRabbit;
    public Button btnDragon;
    public Text txtCurrentPlayer;
    public Text txtScoreA;
    public Text txtScoreB;
    public GameObject victoryPanel;
    public Text txtVictoryMessage;
    public Button btnRestart;
    public GameObject modeSelectionPanel;
    public Button btnPvP;
    public Button btnVsAI;

    [Header("Game State")]
    private Dictionary<HexCoordinates, HexTile> board = new Dictionary<HexCoordinates, HexTile>();
    private Player currentPlayer = Player.PlayerA;
    private AnimalType selectedAnimal = AnimalType.Tiger;
    private bool isAIMode = false;
    private bool gameEnded = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
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

        SelectAnimal(AnimalType.Tiger);
        victoryPanel.SetActive(false);
    }

    private void ShowModeSelection()
    {
        modeSelectionPanel.SetActive(true);
    }

    private void StartGame(bool aiMode)
    {
        isAIMode = aiMode;
        modeSelectionPanel.SetActive(false);
        GenerateBoard();
        SetupStartingPositions();
        UpdateUI();
    }

    private void GenerateBoard()
    {
        board.Clear();
        
        // Очистка старых тайлов
        foreach (Transform child in boardParent)
            Destroy(child.gameObject);

        // Генерация гексагональной сетки
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
        // Стартовая позиция 2x2 в центре
        // A🐇 B🐉
        // B🐅 A🐅
        
        HexCoordinates[] startPositions = new HexCoordinates[]
        {
            new HexCoordinates(0, 0),   // A Tiger
            new HexCoordinates(1, 0),   // B Tiger
            new HexCoordinates(0, -1),  // A Rabbit
            new HexCoordinates(1, -1)   // B Dragon
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
        
        // Визуальная индикация выбранной кнопки
        btnTiger.GetComponent<Image>().color = animal == AnimalType.Tiger ? Color.yellow : Color.white;
        btnRabbit.GetComponent<Image>().color = animal == AnimalType.Rabbit ? Color.yellow : Color.white;
        btnDragon.GetComponent<Image>().color = animal == AnimalType.Dragon ? Color.yellow : Color.white;
    }

    public void OnTileClicked(HexTile tile)
    {
        if (gameEnded) return;
        if (isAIMode && currentPlayer == Player.PlayerB) return; // AI's turn
        if (!tile.IsEmpty) return;

        PlacePiece(tile, currentPlayer, selectedAnimal);
    }

    private void PlacePiece(HexTile tile, Player player, AnimalType animal)
    {
        // 1. Разместить фишку
        tile.SetState(player, animal);

        // 2. Outflank captures
        List<HexTile> outflankedTiles = GetOutflankedTiles(tile.coordinates, player);
        foreach (var t in outflankedTiles)
            t.FlipOwner(player);

        // 3. Dominance captures
        List<HexTile> dominatedTiles = GetDominatedTiles(tile.coordinates, player, animal);
        foreach (var t in dominatedTiles)
            t.FlipOwner(player);

        // 4. Смена хода
        SwitchPlayer();
        UpdateUI();

        // 5. Проверка конца игры
        CheckGameEnd();

        // 6. AI ход если нужно
        if (isAIMode && currentPlayer == Player.PlayerB && !gameEnded)
        {
            Invoke(nameof(AITurn), 1f);
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

            // Собираем вражеские фишки в направлении
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
                    // Нашли свою фишку - все между ними захвачены
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
        
        txtScoreA.text = $"Player A: {scoreA}";
        txtScoreB.text = $"Player B: {scoreB}";
    }

    private void CheckGameEnd()
    {
        // Проверка: есть ли пустые клетки
        bool hasEmptyTiles = board.Values.Any(t => t.IsEmpty);

        if (!hasEmptyTiles)
        {
            EndGame();
        }
    }

    private void EndGame()
    {
        gameEnded = true;
        
        int scoreA = board.Values.Count(t => t.Owner == Player.PlayerA);
        int scoreB = board.Values.Count(t => t.Owner == Player.PlayerB);

        string winner = scoreA > scoreB ? "Player A Wins!" : scoreB > scoreA ? "Player B Wins!" : "Draw!";
        txtVictoryMessage.text = $"{winner}\nScore: {scoreA} - {scoreB}";
        
        victoryPanel.SetActive(true);
    }

    private void AITurn()
    {
        // Простой AI: случайный валидный ход
        List<HexTile> emptyTiles = board.Values.Where(t => t.IsEmpty).ToList();
        
        if (emptyTiles.Count == 0)
        {
            EndGame();
            return;
        }

        HexTile randomTile = emptyTiles[Random.Range(0, emptyTiles.Count)];
        AnimalType randomAnimal = (AnimalType)Random.Range(1, 4);

        PlacePiece(randomTile, Player.PlayerB, randomAnimal);
    }

    private void RestartGame()
    {
        gameEnded = false;
        currentPlayer = Player.PlayerA;
        victoryPanel.SetActive(false);
        ShowModeSelection();
    }
}