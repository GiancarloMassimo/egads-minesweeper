using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Assertions;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    const float UnitsPerTile = 1f; 

    [SerializeField] int numRows;
    [SerializeField] int numCols;
    [SerializeField] int numBombs;

    [SerializeField] Tilemap topLayer;
    [SerializeField] Tilemap bottomLayer;
    [SerializeField] Tilemap bombLayer;

    [SerializeField] TileBase[] bottomTiles;
    [SerializeField] TileBase[] topTiles;
    [SerializeField] TileBase bombTile;
    [SerializeField] TileBase flagTile;

    [SerializeField] RectTransform canvasDimensions;
    [SerializeField] GameObject numberPrefab;
    [SerializeField] Color[] textGradient;

    int[,] map;
    bool[,] covered;
    bool[,] flags;

    const int Bomb = -1;

    void Awake()
    {
        InitMap();
        PlaceBombs();
        PutNumbers();
    }

    void InitMap()
    {
        map = new int[numRows, numCols];

        covered = new bool[numRows, numCols];
        for (int r = 0; r < numRows; r++) 
            for (int c = 0; c < numCols; c++) covered[r, c] = true;

        flags = new bool[numRows, numCols];
    }

    void Start()
    {
        Assert.IsTrue(bottomTiles.Length == 2);
        Assert.IsTrue(topTiles.Length == 2);
        Assert.IsTrue(textGradient.Length == 8);

        canvasDimensions.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, numCols * UnitsPerTile);
        canvasDimensions.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, numRows * UnitsPerTile);
        
        CreateTilemap();
        DisplayNumbers();
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame) 
        {
            OnClick(Mouse.current.position.ReadValue());
        }
    }

    void OnClick(Vector3 screenPos)
    {
        screenPos.z = 10f;
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

        Vector3Int cellPos = topLayer.WorldToCell(worldPos);

        if (!topLayer.HasTile(cellPos)) return;
        
        if (bombLayer.HasTile(cellPos))
        {
            LoseGame();
            return;
        }

        ClearTile(cellPos);
    }

    void LoseGame()
    {
        bombLayer.GetComponent<TilemapRenderer>().sortingOrder = topLayer.GetComponent<TilemapRenderer>().sortingOrder + 1;
        print("You Lose!!!");
        Destroy(gameObject);
    }

    void ClearTile(Vector3Int cellPos)
    {   
        const int ProtectMeFromCrashingUnity = 1000;
        int i = 0;

        Stack<Vector3Int> clearPositions = new();
        clearPositions.Push(cellPos);

        while (clearPositions.Count > 0)
        {
            if (++i == ProtectMeFromCrashingUnity) {
                Debug.LogError("this is sus");
                break;
            } 

            Vector3Int clearPos = clearPositions.Pop();
            (int r, int c) = CellPosToRowCol(clearPos);

            if (!covered[r, c]) continue;

            topLayer.SetTile(clearPos, null);
            covered[r, c] = false;

            if (map[r, c] != 0) continue; 

            var nextPositions = GetNeighbors(r, c)
                .Where(neighbor => covered[neighbor.r, neighbor.c])
                .Select(neighbor => RowColToCellPos(neighbor.r, neighbor.c));

            foreach (Vector3Int pos in nextPositions) clearPositions.Push(pos);
        }
    }

    void PlaceBombs()
    {
        Assert.IsTrue(numBombs <= 0.75 * numRows * numCols, 
            "More than 75% of the level is bombs... it might be too hard lol");

        for (int i = 0; i < numBombs; i++)
        {
            int r, c;

            do
            {   
                r = Random.Range(0, numRows);
                c = Random.Range(0, numCols);       
            }
            while(map[r, c] == Bomb);

            map[r, c] = Bomb;
        }
    }

    void PutNumbers()
    {
        for (int r = 0; r < numRows; r++)
        {
            for (int c = 0; c < numCols; c++)
            {
                if (map[r, c] == Bomb) continue; 
                map[r, c] = CountSurroundingBombs(r, c);
            }
        }
    }

    List<(int r, int c)> GetNeighbors(int row, int col)
    {
        int startRow = row == 0 ? 0 : row - 1;
        int endRow = row == numRows - 1 ? numRows - 1 : row + 1;

        int startCol = col == 0 ? 0 : col - 1;
        int endCol = col == numCols - 1 ? numCols - 1 : col + 1;

        List<(int r, int c)> neighbors = new();

        for (int r = startRow; r <= endRow; r++)
        {
            for (int c = startCol; c <= endCol; c++)
            {
                if (r == row && c == col) continue;
                neighbors.Add((r, c)); 
            }
        }

        return neighbors;
    }

    int CountSurroundingBombs(int r, int c)
    {
        return GetNeighbors(r, c)
            .Count(neighbor => map[neighbor.r, neighbor.c] == Bomb);
    }

    void CreateTilemap()
    {    
        for (int r = 0; r < numRows; r++)
        {
            int tileIndex = r % 2;
            for (int c = 0; c < numCols; c++)
            {
                Vector3Int cellPos = RowColToCellPos(r, c);

                bottomLayer.SetTile(cellPos, bottomTiles[tileIndex]);
                topLayer.SetTile(cellPos, topTiles[tileIndex]);

                if (map[r, c] == Bomb) bombLayer.SetTile(cellPos, bombTile); 

                tileIndex = (++tileIndex) % 2;
            }
        }
    }

    void DisplayNumbers()
    {
        for (int r = 0; r < numRows; r++)
        {
            for (int c = 0; c < numCols; c++)
            {
                if (map[r, c] != Bomb && map[r, c] > 0)
                {
                    GameObject number = Instantiate(numberPrefab, canvasDimensions);

                    var text = number.GetComponent<TMP_Text>(); 
                    text.SetText("{0}", map[r, c]);
                    text.color = textGradient[map[r, c] - 1];

                    Vector3Int cellPos = RowColToCellPos(r , c);
                    Vector3 offset = Vector2.right * UnitsPerTile / 2 + Vector2.up * UnitsPerTile / 2;

                    number.GetComponent<RectTransform>().anchoredPosition = bottomLayer.CellToWorld(cellPos) + offset;
                } 
            }
        }
    }

    Vector3Int RowColToCellPos(int r, int c)
    {
        return new(c - numCols / 2, r - numRows / 2);
    }

    (int r, int c) CellPosToRowCol(Vector3Int cellPos)
    {
        return (numRows / 2 + cellPos.y, numCols / 2 + cellPos.x);
    }
}
