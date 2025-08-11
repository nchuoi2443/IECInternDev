using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Analytics;
using static NormalItem;
using static UnityEditor.Progress;

public class BottomRow : MonoBehaviour
{
    private int bottomCellX;

    private int bottomCellY;

    private int m_currentLastItem;

    private int m_remainingItems;

    private Transform m_root;

    private List<Cell> m_cell;

    private bool[] m_cellStatus;

    private GameManager m_gameManager;

    private List<ListNormalItem> m_listNormalItem;

    private Dictionary<Item, Cell> m_itemOriginCell = new Dictionary<Item, Cell>();

    private int m_itemsInBoad;

    public bool IsBusy { get; private set; }
    private class ListNormalItem
    {
        public List<NormalItem> normalItems;

    }
    public void SetUp(Transform transform, GameSettings gameSettings)
    {
        IsBusy = false;
        m_root = transform;
        m_currentLastItem = -1;

        this.bottomCellX = gameSettings.BottomRowSizeX;
        this.bottomCellY = gameSettings.BottomRowSizeY;

        this.m_remainingItems = gameSettings.BoardSizeX * gameSettings.BoardSizeY;

        m_cell = new List<Cell>();
        for (int i = 0; i < bottomCellX; i++)
        {
            m_cell.Add(new Cell());
        }

        m_cellStatus = new bool[bottomCellX];
        m_gameManager = FindObjectOfType<GameManager>();
        m_itemsInBoad = gameSettings.BoardSizeX * gameSettings.BoardSizeY;

        CreateBottomCell();

        for (int i = 0; i < bottomCellX; i++)
        {
            if (i > 0)
                m_cell[i].NeighbourLeft = m_cell[i - 1];
            else
                m_cell[i].NeighbourLeft = null;

            if (i < bottomCellX - 1)
                m_cell[i].NeighbourRight = m_cell[i + 1];
            else
                m_cell[i].NeighbourRight = null;
        }
    }

    private void CreateBottomCell()
    {
        Vector3 origin = new Vector3(-bottomCellX * 0.5f + 0.5f, -bottomCellY * 0.5f - 3f, 0f);

        GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        for (int x = 0; x < bottomCellX; x++)
        {
            GameObject go = GameObject.Instantiate(prefabBG);
            go.transform.position = origin + new Vector3(x, 0f);
            go.transform.SetParent(m_root);

            Cell cell = go.GetComponent<Cell>();
            cell.Setup(x, bottomCellY - 1);
            m_cellStatus[x] = false;

            m_cell[x] = cell;

        }

    }

    private void Swap(Cell cell1, Cell cell2, Action callback)
    {
        Item item = cell1.Item;
        cell1.Free();
        //Item item2 = cell2.Item;
        //cell1.Assign(item2);
        cell2.Free();
        cell2.Assign(item);

        //item.View.DOMove(cell2.transform.position, 0.3f);
        item.View.DOMove(cell2.transform.position, 0.3f).OnComplete(() => { if (callback != null) callback(); });
    }

    public void SwapCells(Cell cell1)
    {
        if (cell1.Item == null) return;

        if (m_gameManager.CurrentGameMode == GameManager.eLevelMode.MOVES)
        {
            if (m_cell.Contains(cell1)) return;
        }

        if (m_gameManager.CurrentGameMode == GameManager.eLevelMode.TIMER)
        {
            
            if (m_cell.Contains(cell1))
            {
                ReturnItemToOrigin(cell1.Item);
                return;
            } else
            {
                m_itemOriginCell[cell1.Item] = cell1;
            }
        }

        if (m_currentLastItem >= m_cell.Count - 1) return;
        m_currentLastItem++;

        Swap(cell1, m_cell[m_currentLastItem],() =>
        {
            FindMatchAndCollapse(m_cell[m_currentLastItem]);
        });

        
    }

    public void ReturnItemToOrigin(Item item)
    {
        if (m_itemOriginCell.TryGetValue(item, out Cell originCell))
        {
            m_gameManager.BoardController.IsBusy = true;
            foreach (var cell in m_cell)
            {
                if (cell.Item == item)
                {
                    cell.Free();
                }
            }
            if (originCell.Item != null && originCell.Item != item)
            {
                originCell.Free();
            }

            originCell.Assign(item);
            item.SetCell(originCell);
            item.View.DOMove(originCell.transform.position, 0.3f).OnComplete(() =>
            {
                m_gameManager.BoardController.IsBusy = false;
            });
            m_currentLastItem--;
        }
        ShiftLeftItems();
    }

    private void FindMatchAndCollapse(Cell cell2)
    {
        List<Cell> matches = GetHorizontalMatchesWithNull(cell2);

        if (matches.Count < 3)
        {
            if (m_currentLastItem >= m_cell.Count - 1 && (m_gameManager.CurrentGameMode == GameManager.eLevelMode.MOVES
                || m_gameManager.CurrentGameMode == GameManager.eLevelMode.AUTOLOSE || 
                m_gameManager.CurrentGameMode == GameManager.eLevelMode.AUTOWIN))
            {
                m_gameManager.SetState(GameManager.eStateGame.GAME_OVER);
            }
            else return;
        }
        else
        {
            for (int i = 0; i < matches.Count; i++)
            {
                m_currentLastItem--;
                m_remainingItems--;
                matches[i].ExplodeItem();
            }
            ShiftLeftItems();
            if (m_remainingItems <= 0)
            {
                m_gameManager.SetState(GameManager.eStateGame.GAME_WON);
            }
        }
    }

    internal void ShiftLeftItems()
    {
        int target = 0;
        for (int i = 0; i < bottomCellX; i++)
        {
            Cell cell = m_cell[i];
            if (!cell.IsEmpty)
            {
                if (i != target)
                {
                    Item item = cell.Item;
                    cell.Free();
                    m_cell[target].Assign(item);
                    item.View.DOMove(m_cell[target].transform.position, 0.3f);
                }
                target++;
            }
        }
        for (int i = target; i < bottomCellX; i++)
        {
            m_cell[i].Free();
        }
    }

    public List<Cell> GetHorizontalMatchesWithNull(Cell cell)
    {
        List<Cell> matches = new List<Cell>();

        if (cell.Item == null)
            return matches;

        matches.Add(cell);

        // Check to the right
        Cell current = cell;
        while (true)
        {
            Cell right = current.NeighbourRight;
            if (right == null) break;
            if (right.Item == null)
            {
                current = right;
                continue;
            }
            if (right.IsSameType(cell))
            {
                matches.Add(right);
            }
            current = right;
        }

        // Check to the left
        current = cell;
        while (true)
        {
            Cell left = current.NeighbourLeft;
            if (left == null) break;
            if (left.Item == null)
            {
                current = left;
                continue;
            }
            if (left.IsSameType(cell))
            {
                matches.Add(left);
            }
            current = left;
        }

        return matches;
    }


}