using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TetrisGame : MonoBehaviour
{
    // Grid settings
    const int GridWidth = 10;
    const int GridHeight = 20;
    const float InitialDropInterval = 0.8f;
    const float SoftDropInterval = 0.05f;
    const float MoveRepeatDelay = 0.2f;
    const float MoveRepeatInterval = 0.05f;

    // Tetromino definitions: each piece has 4 rotation states, each state has 4 cell offsets
    static readonly Vector2Int[][][] Tetrominoes = new Vector2Int[][][]
    {
        // I
        new Vector2Int[][] {
            new[] { new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(3,1) },
            new[] { new Vector2Int(2,0), new Vector2Int(2,1), new Vector2Int(2,2), new Vector2Int(2,3) },
            new[] { new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2), new Vector2Int(3,2) },
            new[] { new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(1,3) },
        },
        // O
        new Vector2Int[][] {
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) },
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) },
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) },
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) },
        },
        // T
        new Vector2Int[][] {
            new[] { new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(1,2) },
            new[] { new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(2,1) },
            new[] { new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(1,0) },
            new[] { new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(0,1) },
        },
        // S
        new Vector2Int[][] {
            new[] { new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(2,2) },
            new[] { new Vector2Int(1,2), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(2,0) },
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(2,1) },
            new[] { new Vector2Int(0,2), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,0) },
        },
        // Z
        new Vector2Int[][] {
            new[] { new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(1,1), new Vector2Int(2,1) },
            new[] { new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(2,2) },
            new[] { new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,0), new Vector2Int(2,0) },
            new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,2) },
        },
        // J
        new Vector2Int[][] {
            new[] { new Vector2Int(0,2), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) },
            new[] { new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(2,2) },
            new[] { new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(2,0) },
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2) },
        },
        // L
        new Vector2Int[][] {
            new[] { new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(2,2) },
            new[] { new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(2,0) },
            new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) },
            new[] { new Vector2Int(0,2), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2) },
        },
    };

    static readonly Color[] PieceColors = new Color[]
    {
        new Color(0.0f, 0.9f, 0.9f), // I - Cyan
        new Color(0.9f, 0.9f, 0.0f), // O - Yellow
        new Color(0.7f, 0.0f, 0.9f), // T - Purple
        new Color(0.0f, 0.9f, 0.0f), // S - Green
        new Color(0.9f, 0.0f, 0.0f), // Z - Red
        new Color(0.0f, 0.0f, 0.9f), // J - Blue
        new Color(0.9f, 0.5f, 0.0f), // L - Orange
    };

    // Game state
    int[,] grid; // 0 = empty, 1-7 = piece color index+1
    int currentPiece;
    int currentRotation;
    Vector2Int currentPos;
    int nextPiece;
    int score;
    int level;
    int linesCleared;
    float dropTimer;
    bool gameOver;
    bool started;

    // Input repeat
    float leftHoldTimer;
    float rightHoldTimer;
    bool leftHeld;
    bool rightHeld;

    // Visual
    GameObject rootContainer;
    GameObject canvasGo;
    SpriteRenderer[,] gridRenderers;
    SpriteRenderer[] previewRenderers;
    Sprite pixelSprite;
    Text scoreText;
    Text levelText;
    Text linesText;
    Text gameOverText;

    // Lobby callback
    public System.Action OnReturnToLobby;

    // Layout offsets
    float gridOffsetX;
    float gridOffsetY;

    void Start()
    {
        rootContainer = new GameObject("TetrisGameRoot");
        grid = new int[GridWidth, GridHeight];
        gridRenderers = new SpriteRenderer[GridWidth, GridHeight];
        CreatePixelSprite();
        SetupCamera();
        DrawBorder();
        DrawGrid();
        CreatePreviewArea();
        CreateUI();
        StartNewGame();
    }

    void CreatePixelSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = GridHeight / 2f + 2f;
        // Center on grid + side panel
        gridOffsetX = 0;
        gridOffsetY = 0;
        cam.transform.position = new Vector3(GridWidth / 2f + 2f, GridHeight / 2f - 0.5f, -10);
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
    }

    void DrawBorder()
    {
        Color borderColor = new Color(0.35f, 0.35f, 0.55f);
        for (int x = -1; x <= GridWidth; x++)
        {
            CreateSquare(new Vector2(x, -1), borderColor, "Border");
            CreateSquare(new Vector2(x, GridHeight), borderColor, "Border");
        }
        for (int y = -1; y <= GridHeight; y++)
        {
            CreateSquare(new Vector2(-1, y), borderColor, "Border");
            CreateSquare(new Vector2(GridWidth, y), borderColor, "Border");
        }
    }

    void DrawGrid()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                Color c = (x + y) % 2 == 0
                    ? new Color(0.12f, 0.12f, 0.15f)
                    : new Color(0.14f, 0.14f, 0.17f);
                var go = CreateSquare(new Vector2(x, y), c, "Grid", 0);
                gridRenderers[x, y] = go.GetComponent<SpriteRenderer>();
            }
        }
    }

    void CreatePreviewArea()
    {
        // "NEXT" label area - right side of grid
        float previewX = GridWidth + 2f;
        float previewY = GridHeight - 3f;

        Color panelColor = new Color(0.15f, 0.15f, 0.2f);
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
                CreateSquare(new Vector2(previewX + x, previewY - y), panelColor, "Preview", 0);

        // Preview piece renderers
        previewRenderers = new SpriteRenderer[4];
        for (int i = 0; i < 4; i++)
        {
            var go = CreateSquare(new Vector2(0, 0), Color.clear, "PreviewPiece", 2);
            go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            previewRenderers[i] = go.GetComponent<SpriteRenderer>();
        }
    }

    GameObject CreateSquare(Vector2 pos, Color color, string name, int sortingOrder = 0)
    {
        var go = new GameObject(name);
        go.transform.SetParent(rootContainer.transform);
        go.transform.position = new Vector3(pos.x, pos.y, 0);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = pixelSprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return go;
    }

    void CreateUI()
    {
        canvasGo = new GameObject("TetrisCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Score - top right
        scoreText = CreateUIText(canvasGo.transform, "ScoreText",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -10), new Vector2(300, 50),
            32, Color.white, TextAnchor.UpperRight);

        // Level
        levelText = CreateUIText(canvasGo.transform, "LevelText",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -60), new Vector2(300, 50),
            28, new Color(0.8f, 0.8f, 1f), TextAnchor.UpperRight);

        // Lines
        linesText = CreateUIText(canvasGo.transform, "LinesText",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -100), new Vector2(300, 50),
            28, new Color(0.8f, 0.8f, 1f), TextAnchor.UpperRight);

        // Game over text - center
        gameOverText = CreateUIText(canvasGo.transform, "GameOverText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(600, 150),
            48, new Color(1f, 0.3f, 0.3f), TextAnchor.MiddleCenter);
        gameOverText.text = "";

        // Hint text - top left
        var hintText = CreateUIText(canvasGo.transform, "HintText",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -10), new Vector2(500, 80),
            22, new Color(1f, 1f, 1f, 0.5f), TextAnchor.UpperLeft);
        hintText.text = "Arrow Keys: Move/Rotate  Space: Hard Drop\nESC - Back to Lobby";
    }

    Text CreateUIText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta,
        int fontSize, Color color, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = FontStyle.Bold;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(2, -2);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        return text;
    }

    void StartNewGame()
    {
        // Clear grid
        for (int x = 0; x < GridWidth; x++)
            for (int y = 0; y < GridHeight; y++)
                grid[x, y] = 0;

        score = 0;
        level = 1;
        linesCleared = 0;
        gameOver = false;
        started = true;
        dropTimer = 0f;

        nextPiece = Random.Range(0, 7);
        SpawnNewPiece();
        UpdateUI();
        RefreshGrid();
    }

    void SpawnNewPiece()
    {
        currentPiece = nextPiece;
        nextPiece = Random.Range(0, 7);
        currentRotation = 0;
        currentPos = new Vector2Int(GridWidth / 2 - 2, GridHeight - 3);

        if (!IsValidPosition(currentPiece, currentRotation, currentPos))
        {
            gameOver = true;
            gameOverText.text = "Game Over!\nPress SPACE to restart\nESC - Back to Lobby";
        }

        UpdatePreview();
    }

    void UpdatePreview()
    {
        float previewX = GridWidth + 2f;
        float previewY = GridHeight - 3f;
        var cells = Tetrominoes[nextPiece][0];
        Color color = PieceColors[nextPiece];

        for (int i = 0; i < 4; i++)
        {
            previewRenderers[i].color = color;
            previewRenderers[i].transform.position = new Vector3(
                previewX + cells[i].x,
                previewY - 1 + cells[i].y - 1,
                0);
        }
    }

    bool IsValidPosition(int piece, int rotation, Vector2Int pos)
    {
        var cells = Tetrominoes[piece][rotation];
        foreach (var cell in cells)
        {
            int x = pos.x + cell.x;
            int y = pos.y + cell.y;
            if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
                return false;
            if (grid[x, y] != 0)
                return false;
        }
        return true;
    }

    void LockPiece()
    {
        var cells = Tetrominoes[currentPiece][currentRotation];
        int colorIndex = currentPiece + 1;
        foreach (var cell in cells)
        {
            int x = currentPos.x + cell.x;
            int y = currentPos.y + cell.y;
            if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight)
                grid[x, y] = colorIndex;
        }

        ClearLines();
        SpawnNewPiece();
    }

    void ClearLines()
    {
        int cleared = 0;
        for (int y = 0; y < GridHeight; y++)
        {
            bool full = true;
            for (int x = 0; x < GridWidth; x++)
            {
                if (grid[x, y] == 0)
                {
                    full = false;
                    break;
                }
            }

            if (full)
            {
                cleared++;
                // Shift everything above down
                for (int row = y; row < GridHeight - 1; row++)
                    for (int x = 0; x < GridWidth; x++)
                        grid[x, row] = grid[x, row + 1];
                // Clear top row
                for (int x = 0; x < GridWidth; x++)
                    grid[x, GridHeight - 1] = 0;
                y--; // Re-check same row
            }
        }

        if (cleared > 0)
        {
            int[] scoreTable = { 0, 100, 300, 500, 800 };
            score += scoreTable[cleared] * level;
            linesCleared += cleared;
            level = linesCleared / 10 + 1;
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        scoreText.text = "Score: " + score;
        levelText.text = "Level: " + level;
        linesText.text = "Lines: " + linesCleared;
    }

    void RefreshGrid()
    {
        // Draw locked cells
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                if (grid[x, y] != 0)
                {
                    gridRenderers[x, y].color = PieceColors[grid[x, y] - 1] * 0.8f;
                    gridRenderers[x, y].sortingOrder = 1;
                }
                else
                {
                    Color c = (x + y) % 2 == 0
                        ? new Color(0.12f, 0.12f, 0.15f)
                        : new Color(0.14f, 0.14f, 0.17f);
                    gridRenderers[x, y].color = c;
                    gridRenderers[x, y].sortingOrder = 0;
                }
            }
        }

        // Draw ghost piece (drop preview)
        if (!gameOver)
        {
            Vector2Int ghostPos = currentPos;
            while (IsValidPosition(currentPiece, currentRotation, ghostPos + Vector2Int.down))
                ghostPos += Vector2Int.down;

            if (ghostPos != currentPos)
            {
                var ghostCells = Tetrominoes[currentPiece][currentRotation];
                Color ghostColor = PieceColors[currentPiece] * 0.25f;
                foreach (var cell in ghostCells)
                {
                    int gx = ghostPos.x + cell.x;
                    int gy = ghostPos.y + cell.y;
                    if (gx >= 0 && gx < GridWidth && gy >= 0 && gy < GridHeight && grid[gx, gy] == 0)
                    {
                        gridRenderers[gx, gy].color = ghostColor;
                        gridRenderers[gx, gy].sortingOrder = 1;
                    }
                }
            }

            // Draw current piece
            var cells = Tetrominoes[currentPiece][currentRotation];
            Color pieceColor = PieceColors[currentPiece];
            foreach (var cell in cells)
            {
                int px = currentPos.x + cell.x;
                int py = currentPos.y + cell.y;
                if (px >= 0 && px < GridWidth && py >= 0 && py < GridHeight)
                {
                    gridRenderers[px, py].color = pieceColor;
                    gridRenderers[px, py].sortingOrder = 2;
                }
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cleanup();
            OnReturnToLobby?.Invoke();
            return;
        }

        if (gameOver)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                StartNewGame();
            return;
        }

        if (!started) return;

        HandleInput();

        // Auto drop
        float interval = Input.GetKey(KeyCode.DownArrow) ? SoftDropInterval : GetDropInterval();
        dropTimer += Time.deltaTime;
        if (dropTimer >= interval)
        {
            dropTimer = 0f;
            if (IsValidPosition(currentPiece, currentRotation, currentPos + Vector2Int.down))
            {
                currentPos += Vector2Int.down;
                if (Input.GetKey(KeyCode.DownArrow))
                    score += 1; // Soft drop bonus
            }
            else
            {
                LockPiece();
            }
            RefreshGrid();
        }
    }

    float GetDropInterval()
    {
        // Speed increases with level
        return Mathf.Max(0.05f, InitialDropInterval - (level - 1) * 0.07f);
    }

    void HandleInput()
    {
        // Rotation
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            int newRot = (currentRotation + 1) % 4;
            if (IsValidPosition(currentPiece, newRot, currentPos))
            {
                currentRotation = newRot;
                RefreshGrid();
            }
            else
            {
                // Wall kick: try shifting left/right
                for (int kick = 1; kick <= 2; kick++)
                {
                    if (IsValidPosition(currentPiece, newRot, currentPos + new Vector2Int(kick, 0)))
                    {
                        currentPos += new Vector2Int(kick, 0);
                        currentRotation = newRot;
                        RefreshGrid();
                        break;
                    }
                    if (IsValidPosition(currentPiece, newRot, currentPos + new Vector2Int(-kick, 0)))
                    {
                        currentPos += new Vector2Int(-kick, 0);
                        currentRotation = newRot;
                        RefreshGrid();
                        break;
                    }
                }
            }
        }

        // Horizontal movement with DAS (Delayed Auto Shift)
        HandleHorizontalMove(KeyCode.LeftArrow, Vector2Int.left, ref leftHeld, ref leftHoldTimer);
        HandleHorizontalMove(KeyCode.RightArrow, Vector2Int.right, ref rightHeld, ref rightHoldTimer);

        // Hard drop
        if (Input.GetKeyDown(KeyCode.Space))
        {
            int dropDist = 0;
            while (IsValidPosition(currentPiece, currentRotation, currentPos + Vector2Int.down))
            {
                currentPos += Vector2Int.down;
                dropDist++;
            }
            score += dropDist * 2; // Hard drop bonus
            LockPiece();
            dropTimer = 0f;
            RefreshGrid();
            UpdateUI();
        }
    }

    void HandleHorizontalMove(KeyCode key, Vector2Int dir, ref bool held, ref float holdTimer)
    {
        if (Input.GetKeyDown(key))
        {
            if (IsValidPosition(currentPiece, currentRotation, currentPos + dir))
            {
                currentPos += dir;
                RefreshGrid();
            }
            held = true;
            holdTimer = 0f;
        }
        else if (Input.GetKey(key) && held)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= MoveRepeatDelay)
            {
                float repeatTime = holdTimer - MoveRepeatDelay;
                // Use modulo-based approach for consistent repeat
                if (repeatTime % MoveRepeatInterval < Time.deltaTime)
                {
                    if (IsValidPosition(currentPiece, currentRotation, currentPos + dir))
                    {
                        currentPos += dir;
                        RefreshGrid();
                    }
                }
            }
        }
        if (Input.GetKeyUp(key))
        {
            held = false;
            holdTimer = 0f;
        }
    }

    public void Cleanup()
    {
        if (rootContainer != null) Destroy(rootContainer);
        if (canvasGo != null) Destroy(canvasGo);
        Destroy(gameObject);
    }
}
