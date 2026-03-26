using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SnakeGame : MonoBehaviour
{
    // Grid settings
    const int GridWidth = 20;
    const int GridHeight = 20;
    const float MoveInterval = 0.12f;

    // Snake state
    List<Vector2Int> snake = new List<Vector2Int>();
    Vector2Int direction = Vector2Int.right;
    Vector2Int nextDirection = Vector2Int.right;
    Vector2Int foodPosition;
    int score = 0;
    float moveTimer = 0f;
    bool gameOver = false;

    // Visual objects
    List<SpriteRenderer> snakeRenderers = new List<SpriteRenderer>();
    GameObject foodObject;
    Text scoreText;
    Text gameOverText;
    Sprite pixelSprite;

    // Root parent for easy cleanup
    GameObject rootContainer;
    GameObject canvasGo;

    // Lobby callback
    public System.Action OnReturnToLobby;

    void Start()
    {
        rootContainer = new GameObject("SnakeGameRoot");
        CreatePixelSprite();
        SetupCamera();
        DrawBorder();
        DrawGridBackground();
        CreateUI();
        InitGame();
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
        cam.orthographicSize = GridHeight / 2f + 1.5f;
        cam.transform.position = new Vector3(GridWidth / 2f - 0.5f, GridHeight / 2f - 0.5f, -10);
        cam.backgroundColor = new Color(0.12f, 0.12f, 0.15f);
    }

    void DrawBorder()
    {
        Color borderColor = new Color(0.35f, 0.35f, 0.55f);
        for (int x = -1; x <= GridWidth; x++)
        {
            CreateSquare(new Vector2(x, -1), borderColor, "Border");
            CreateSquare(new Vector2(x, GridHeight), borderColor, "Border");
        }
        for (int y = 0; y < GridHeight; y++)
        {
            CreateSquare(new Vector2(-1, y), borderColor, "Border");
            CreateSquare(new Vector2(GridWidth, y), borderColor, "Border");
        }
    }

    void DrawGridBackground()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                Color c = (x + y) % 2 == 0
                    ? new Color(0.16f, 0.16f, 0.19f)
                    : new Color(0.18f, 0.18f, 0.21f);
                CreateSquare(new Vector2(x, y), c, "BG", -1);
            }
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
        canvasGo = new GameObject("SnakeCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Score text - top right
        scoreText = CreateUIText(canvasGo.transform, "ScoreText",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -10), new Vector2(300, 60),
            36, Color.white, TextAnchor.UpperRight);
        scoreText.text = "Score: 0";

        // Game over text - center
        gameOverText = CreateUIText(canvasGo.transform, "GameOverText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(600, 150),
            52, new Color(1f, 0.3f, 0.3f), TextAnchor.MiddleCenter);
        gameOverText.text = "";

        // Hint text - top left
        var hintText = CreateUIText(canvasGo.transform, "HintText",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -10), new Vector2(400, 50),
            24, new Color(1f, 1f, 1f, 0.5f), TextAnchor.UpperLeft);
        hintText.text = "ESC - Back to Lobby";
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

    void InitGame()
    {
        foreach (var sr in snakeRenderers)
            if (sr != null) Destroy(sr.gameObject);
        snakeRenderers.Clear();

        if (foodObject != null) Destroy(foodObject);

        snake.Clear();
        score = 0;
        gameOver = false;
        direction = Vector2Int.right;
        nextDirection = Vector2Int.right;
        moveTimer = 0f;

        Vector2Int startPos = new Vector2Int(GridWidth / 2, GridHeight / 2);
        for (int i = 0; i < 3; i++)
            snake.Add(new Vector2Int(startPos.x - i, startPos.y));

        for (int i = 0; i < snake.Count; i++)
            CreateSnakeSegment(i);

        SpawnFood();
        UpdateScoreUI();
        gameOverText.text = "";
    }

    void CreateSnakeSegment(int index)
    {
        Color color = index == 0
            ? new Color(0.2f, 0.9f, 0.2f)
            : new Color(0.1f, 0.7f, 0.1f);
        var go = CreateSquare(new Vector2(snake[index].x, snake[index].y), color, "Snake", 2);
        go.transform.localScale = index == 0
            ? new Vector3(0.92f, 0.92f, 1f)
            : new Vector3(0.85f, 0.85f, 1f);

        var sr = go.GetComponent<SpriteRenderer>();
        if (index < snakeRenderers.Count)
            snakeRenderers.Insert(index, sr);
        else
            snakeRenderers.Add(sr);
    }

    void SpawnFood()
    {
        var freePositions = new List<Vector2Int>();
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                var pos = new Vector2Int(x, y);
                if (!snake.Contains(pos))
                    freePositions.Add(pos);
            }
        }

        if (freePositions.Count == 0) return;

        foodPosition = freePositions[Random.Range(0, freePositions.Count)];

        if (foodObject != null) Destroy(foodObject);
        foodObject = CreateSquare(new Vector2(foodPosition.x, foodPosition.y),
            new Color(1f, 0.25f, 0.25f), "Food", 1);
        foodObject.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
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
                InitGame();
            return;
        }

        HandleInput();

        moveTimer += Time.deltaTime;
        if (moveTimer >= MoveInterval)
        {
            moveTimer = 0f;
            MoveSnake();
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) && direction != Vector2Int.down)
            nextDirection = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow) && direction != Vector2Int.up)
            nextDirection = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) && direction != Vector2Int.right)
            nextDirection = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow) && direction != Vector2Int.left)
            nextDirection = Vector2Int.right;
    }

    void MoveSnake()
    {
        direction = nextDirection;
        Vector2Int newHead = snake[0] + direction;

        if (newHead.x < 0 || newHead.x >= GridWidth || newHead.y < 0 || newHead.y >= GridHeight)
        {
            DoGameOver();
            return;
        }

        for (int i = 0; i < snake.Count - 1; i++)
        {
            if (snake[i] == newHead)
            {
                DoGameOver();
                return;
            }
        }

        bool ateFood = newHead == foodPosition;

        snake.Insert(0, newHead);
        CreateSnakeSegment(0);

        if (ateFood)
        {
            score += 10;
            UpdateScoreUI();
            SpawnFood();
        }
        else
        {
            int lastIndex = snake.Count - 1;
            snake.RemoveAt(lastIndex);
            Destroy(snakeRenderers[lastIndex].gameObject);
            snakeRenderers.RemoveAt(lastIndex);
        }

        UpdateSnakeVisuals();
    }

    void UpdateSnakeVisuals()
    {
        for (int i = 0; i < snake.Count; i++)
        {
            if (snakeRenderers[i] == null) continue;
            snakeRenderers[i].transform.position = new Vector3(snake[i].x, snake[i].y, 0);
            snakeRenderers[i].color = i == 0
                ? new Color(0.2f, 0.9f, 0.2f)
                : new Color(0.1f, 0.65f, 0.1f);
            snakeRenderers[i].transform.localScale = i == 0
                ? new Vector3(0.92f, 0.92f, 1f)
                : new Vector3(0.85f, 0.85f, 1f);
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    void DoGameOver()
    {
        gameOver = true;
        gameOverText.text = "Game Over!\nPress SPACE to restart\nESC - Back to Lobby";
    }

    public void Cleanup()
    {
        if (rootContainer != null) Destroy(rootContainer);
        if (canvasGo != null) Destroy(canvasGo);
        Destroy(gameObject);
    }
}
