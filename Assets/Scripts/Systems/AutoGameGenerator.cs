using UnityEngine;
using DragonChessLegends.Core;
using DragonChessLegends.Data;

/// <summary>
/// 自动生成游戏 - 挂在空物体上自动运行
/// </summary>
public class AutoGameGenerator : MonoBehaviour
{
    void Awake()
    {
        GenerateGame();
    }

    void GenerateGame()
    {
        // 1. 创建Camera（如果没有）
        CreateCameraIfNeeded();

        // 2. 创建棋盘
        CreateChessBoard();

        // 3. 创建棋子
        CreatePieces();

        // 4. 创建UI
        CreateUI();

        Debug.Log("✅ 游戏生成完成！点击棋子可查看移动范围");
    }

    void CreateCameraIfNeeded()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        cam.transform.position = new Vector3(0, 0, -10);
        cam.orthographic = true;
        cam.orthographicSize = 5;
        cam.backgroundColor = new Color(0.1f, 0.08f, 0.05f);
    }

    void CreateChessBoard()
    {
        float cellSize = 1f;
        float startX = -4f;
        float startY = -4.5f;

        // 创建9x10格子
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cell.name = $"Cell_{x}_{y}";
                cell.transform.position = new Vector3(startX + x * cellSize, startY + y * cellSize, 0);
                cell.transform.localScale = new Vector3(cellSize * 0.95f, cellSize * 0.95f, 1);

                Renderer rend = cell.GetComponent<Renderer>();
                bool isLight = (x + y) % 2 == 0;
                if (y == 4 || y == 5)
                {
                    rend.material.color = new Color(0.3f, 0.5f, 0.8f, 0.4f);
                }
                else
                {
                    rend.material.color = isLight ? new Color(0.85f, 0.75f, 0.55f) : new Color(0.6f, 0.4f, 0.25f);
                }

                // 添加碰撞器和点击检测
                BoxCollider2D collider = cell.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one;

                CellClickListener listener = cell.AddComponent<CellClickListener>();
                listener.x = x;
                listener.y = y;
            }
        }
    }

    void CreatePieces()
    {
        // 初始化棋盘
        gameObject.AddComponent<BoardManager>();

        float cellSize = 1f;
        float startX = -4f;
        float startY = -4.5f;

        // 红色方（上方）
        PlacePiece(PieceType.General, PieceSubType.Holy, 4, 0, "圣殿骑士", new Color(0.9f, 0.3f, 0.3f), startX, startY, cellSize);
        PlacePiece(PieceType.Chariot, PieceSubType.Iron, 0, 0, "泰坦", new Color(0.8f, 0.8f, 0.8f), startX, startY, cellSize);
        PlacePiece(PieceType.Chariot, PieceSubType.Iron, 8, 0, "泰坦", new Color(0.8f, 0.8f, 0.8f), startX, startY, cellSize);
        PlacePiece(PieceType.Horse, PieceSubType.Nightmare, 1, 0, "梦魇", new Color(0.4f, 0.2f, 0.6f), startX, startY, cellSize);
        PlacePiece(PieceType.Horse, PieceSubType.Nightmare, 7, 0, "梦魇", new Color(0.4f, 0.2f, 0.6f), startX, startY, cellSize);
        PlacePiece(PieceType.Cannon, PieceSubType.FireCannon, 1, 2, "火炮", new Color(0.9f, 0.5f, 0.2f), startX, startY, cellSize);
        PlacePiece(PieceType.Cannon, PieceSubType.FireCannon, 7, 2, "火炮", new Color(0.9f, 0.5f, 0.2f), startX, startY, cellSize);

        // 兵
        for (int i = 0; i < 5; i++)
        {
            PlacePiece(PieceType.Soldier, PieceSubType.Sword, i * 2, 3, "剑士", new Color(0.5f, 0.7f, 0.5f), startX, startY, cellSize);
        }

        // 黑色方（下方）
        PlacePiece(PieceType.General, PieceSubType.Fire, 4, 9, "炎魔", new Color(0.3f, 0.3f, 0.9f), startX, startY, cellSize);
        PlacePiece(PieceType.Chariot, PieceSubType.Thunder, 0, 9, "雷鸣", new Color(0.9f, 0.9f, 0.3f), startX, startY, cellSize);
        PlacePiece(PieceType.Chariot, PieceSubType.Thunder, 8, 9, "雷鸣", new Color(0.9f, 0.9f, 0.3f), startX, startY, cellSize);

        // 卒
        for (int i = 0; i < 5; i++)
        {
            PlacePiece(PieceType.Soldier, PieceSubType.Sword, i * 2, 6, "卒", new Color(0.3f, 0.6f, 0.3f), startX, startY, cellSize);
        }
    }

    void PlacePiece(PieceType type, PieceSubType subType, int x, int y, string name, Color color, float startX, float startY, float cellSize)
    {
        var piece = PieceFactory.CreatePiece(type, subType);
        piece.pieceName = name;
        BoardManager.Instance.PlacePiece(piece, x, y);

        // 创建显示
        GameObject pieceObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        pieceObj.name = $"Piece_{name}_{x}_{y}";
        pieceObj.transform.position = new Vector3(startX + x * cellSize, startY + y * cellSize, -0.1f);
        pieceObj.transform.localScale = new Vector3(cellSize * 0.9f, cellSize * 0.9f, 1);

        Renderer rend = pieceObj.GetComponent<Renderer>();
        rend.material.color = color;

        // 添加碰撞器
        BoxCollider2D collider = pieceObj.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;

        // 保存引用
        PieceDisplay display = pieceObj.AddComponent<PieceDisplay>();
        display.pieceId = piece.id;
    }

    void CreateUI()
    {
        // 创建文字说明
        GameObject textObj = new GameObject("InfoText");
        textObj.transform.position = new Vector3(0, 5.5f, -0.2f);

        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = "龙棋传说 - 点击棋子显示移动范围";
        textMesh.fontSize = 40;
        textMesh.characterSize = 0.1f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;
    }
}

/// <summary>
/// 格子点击事件
/// </summary>
public class CellClickListener : MonoBehaviour
{
    public int x;
    public int y;

    void Start()
    {
        ClearCellHighlights();
    }

    void OnMouseDown()
    {
        Debug.Log($"点击格子: ({x}, {y})");
        ClearCellHighlights();
    }

    void ClearCellHighlights()
    {
        GameObject[] allObjs = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjs)
        {
            if (obj.name == "Highlight")
            {
                Destroy(obj);
            }
        }
    }
}

/// <summary>
/// 棋子显示
/// </summary>
public class PieceDisplay : MonoBehaviour
{
    public string pieceId;

    void OnMouseDown()
    {
        Debug.Log($"点击棋子: {pieceId}");
        ClearCellHighlights();
        ShowValidMoves();
    }

    void ShowValidMoves()
    {
        var piece = BoardManager.Instance.GetAllPieces().Find(p => p.id == pieceId);
        if (piece == null) return;

        var moves = MovementLogic.GetValidMoves(piece, BoardManager.Instance);
        var attacks = MovementLogic.GetValidAttacks(piece, BoardManager.Instance);

        float cellSize = 1f;
        float startX = -4f;
        float startY = -4.5f;

        // 移动位置 - 绿色
        foreach (var move in moves)
        {
            CreateHighlight(move.x, move.y, new Color(0.3f, 0.9f, 0.3f, 0.5f), cellSize, startX, startY);
        }

        // 攻击位置 - 红色
        foreach (var attack in attacks)
        {
            CreateHighlight(attack.x, attack.y, new Color(0.9f, 0.3f, 0.3f, 0.5f), cellSize, startX, startY);
        }
    }

    void CreateHighlight(int x, int y, Color color, float cellSize, float startX, float startY)
    {
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
        highlight.name = "Highlight";
        highlight.transform.position = new Vector3(startX + x * cellSize, startY + y * cellSize, -0.05f);
        highlight.transform.localScale = new Vector3(cellSize * 0.85f, cellSize * 0.85f, 1);

        Renderer rend = highlight.GetComponent<Renderer>();
        rend.material.color = color;
    }

    void ClearCellHighlights()
    {
        GameObject[] allObjs = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjs)
        {
            if (obj.name == "Highlight")
            {
                Destroy(obj);
            }
        }
    }
}
