using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Assertions;
using TMPro;

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
        map = new int[numRows, numCols];
        covered = new bool[numRows, numCols];
        flags = new bool[numRows, numCols];

        PlaceBombs();
        PutNumbers();
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

    int CountSurroundingBombs(int row, int col)
    {
        int startRow = row == 0 ? 0 : row - 1;
        int endRow = row == numRows - 1 ? numRows - 1 : row + 1;

        int startCol = col == 0 ? 0 : col - 1;
        int endCol = col == numCols - 1 ? numCols - 1 : col + 1;
    
        int count = 0;

        for (int r = startRow; r <= endRow; r++)
        {
            for (int c = startCol; c <= endCol; c++)
            {
                if (map[r, c] == Bomb) count++; 
            }
        }

        return count;
    }

    void CreateTilemap()
    {
        Vector3Int currPos = new(-numCols / 2, -numRows / 2);
    
        for (int r = 0; r < numRows; r++)
        {
            int tileIndex = r % 2;

            currPos.x = -numCols / 2;
            for (int c = 0; c < numCols; c++)
            {
                bottomLayer.SetTile(currPos, bottomTiles[(++tileIndex) % 2]);
                if (map[r, c] == Bomb) bombLayer.SetTile(currPos, bombTile); 

                ++currPos.x;
            }

            ++currPos.y;
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

                    Vector3Int cellPos = new(c - numCols / 2, r - numRows / 2);
                    Vector3 offset = Vector2.right * UnitsPerTile / 2 + Vector2.up * UnitsPerTile / 2;

                    number.GetComponent<RectTransform>().anchoredPosition = bottomLayer.CellToWorld(cellPos) + offset;
                } 
            }
        }
    }
}
