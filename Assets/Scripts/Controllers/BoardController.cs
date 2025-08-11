using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };

    private bool m_isBusy;
    public bool IsBusy
    {
        get => m_isBusy;
        set => m_isBusy = value;
    }

    private Board m_board;

    private BottomRow m_bottomRow;

    private GameManager m_gameManager;

    private bool m_isDragging;

    private Camera m_cam;

    private Collider2D m_hitCollider;

    private GameSettings m_gameSettings;

    private List<Cell> m_potentialMatch;

    private float m_timeAfterFill;

    private bool m_hintIsShown;

    private bool m_gameOver;

    public void StartGame(GameManager gameManager, GameSettings gameSettings, BottomRow bottomRow)
    {
        m_gameManager = gameManager;

        m_bottomRow = bottomRow;

        m_gameSettings = gameSettings;

        m_gameManager.StateChangedAction += OnGameStateChange;

        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);

        Fill();
    }

    private void Fill()
    {
        m_board.Fill();
        //FindMatchesAndCollapse();
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                m_isBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                m_isBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                m_gameOver = true;
                StopHints();
                break;
        }
    }


    public void Update()
    {
        if (m_gameOver) return;
        if (m_isBusy) return;
        if (m_gameManager.CurrentGameMode == GameManager.eLevelMode.AUTOLOSE ||
            m_gameManager.CurrentGameMode == GameManager.eLevelMode.AUTOWIN)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                m_isDragging = true;
                m_hitCollider = hit.collider;
            }
        }

        if (m_hitCollider != null)
        {
            Cell cell = m_hitCollider.GetComponent<Cell>();
            m_bottomRow.SwapCells(cell);
            ResetRayCast();
        }

        if (Input.GetMouseButtonUp(0))
        {
            ResetRayCast();
        }

        /*if (Input.GetMouseButton(0) && m_isDragging)
        {
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                if (m_hitCollider != null && m_hitCollider != hit.collider)
                {
                    //StopHints();

                    Cell c1 = m_hitCollider.GetComponent<Cell>();
                    Cell c2 = hit.collider.GetComponent<Cell>();
                    if (AreItemsNeighbor(c1, c2))
                    {
                        IsBusy = true;
                        SetSortingLayer(c1, c2);
                        m_board.Swap(c1, c2, () =>
                        {
                            //FindMatchesAndCollapse(c1, c2); 
                            //Need to change gameplay here
                        });

                        ResetRayCast();
                    }
                }
            }
            else
            {
                ResetRayCast();
            }
        }*/
    }

    private void ResetRayCast()
    {
        m_isDragging = false;
        m_hitCollider = null;
    }

    private void FindMatchesAndCollapse(Cell cell1, Cell cell2)
    {
        if (cell1.Item is BonusItem)
        {
            cell1.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else if (cell2.Item is BonusItem)
        {
            cell2.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else
        {
            List<Cell> cells1 = GetMatches(cell1);
            List<Cell> cells2 = GetMatches(cell2);

            List<Cell> matches = new List<Cell>();
            matches.AddRange(cells1);
            matches.AddRange(cells2);
            matches = matches.Distinct().ToList();

            if (matches.Count < m_gameSettings.MatchesMin)
            {
                m_board.Swap(cell1, cell2, () =>
                {
                    IsBusy = false;
                });
            }
            else
            {
                OnMoveEvent();

                CollapseMatches(matches, cell2);
            }
        }
    }

    private void FindMatchesAndCollapse()
    {
        List<Cell> matches = m_board.FindFirstMatch();

        if (matches.Count > 0)
        {
            CollapseMatches(matches, null);
        }
        else
        {
            m_potentialMatch = m_board.GetPotentialMatches();
            if (m_potentialMatch.Count > 0)
            {
                IsBusy = false;

                m_timeAfterFill = 0f;
            }
            else
            {
                //StartCoroutine(RefillBoardCoroutine());
                StartCoroutine(ShuffleBoardCoroutine());
            }
        }
    }

    private List<Cell> GetMatches(Cell cell)
    {
        List<Cell> listHor = m_board.GetHorizontalMatches(cell);
        if (listHor.Count < m_gameSettings.MatchesMin)
        {
            listHor.Clear();
        }

        List<Cell> listVert = m_board.GetVerticalMatches(cell);
        if (listVert.Count < m_gameSettings.MatchesMin)
        {
            listVert.Clear();
        }

        return listHor.Concat(listVert).Distinct().ToList();
    }

    private void CollapseMatches(List<Cell> matches, Cell cellEnd)
    {
        for (int i = 0; i < matches.Count; i++)
        {
            matches[i].ExplodeItem();
        }

        if(matches.Count > m_gameSettings.MatchesMin)
        {
            m_board.ConvertNormalToBonus(matches, cellEnd);
        }

        StartCoroutine(ShiftDownItemsCoroutine());
    }

    private IEnumerator ShiftDownItemsCoroutine()
    {
        m_board.ShiftDownItems();

        yield return new WaitForSeconds(0.2f);

        m_board.FillGapsWithNewItems();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator RefillBoardCoroutine()
    {
        m_board.ExplodeAllItems();

        yield return new WaitForSeconds(0.2f);

        m_board.Fill();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator ShuffleBoardCoroutine()
    {
        m_board.Shuffle();

        yield return new WaitForSeconds(0.3f);

        FindMatchesAndCollapse();
    }


    private void SetSortingLayer(Cell cell1, Cell cell2)
    {
        if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
        if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
    }

    private bool AreItemsNeighbor(Cell cell1, Cell cell2)
    {
        return cell1.IsNeighbour(cell2);
    }

    internal void Clear()
    {
        m_board.Clear();
    }

    private void ShowHint()
    {
        m_hintIsShown = true;
        foreach (var cell in m_potentialMatch)
        {
            cell.AnimateItemForHint();
        }
    }

    private void StopHints()
    {
        m_hintIsShown = false;
        foreach (var cell in m_potentialMatch)
        {
            cell.StopHintAnimation();
        }

        m_potentialMatch.Clear();
    }

    public void StartAutoWin()
    {
        StartCoroutine(AutoPlayRoutine());
    }

    private IEnumerator AutoPlayRoutine()
    {
        while (true)
        {
            List<Cell> availableCells = new List<Cell>();
            for (int x = 0; x < m_gameSettings.BoardSizeX; x++)
            {
                for (int y = 0; y < m_gameSettings.BoardSizeY; y++)
                {
                    Cell cell = m_board.GetCell(x, y);
                    if (cell != null && cell.Item != null)
                        availableCells.Add(cell);
                }
            }
            availableCells.RemoveAll(c => c == null || c.Item == null);
            if (availableCells.Count == 0)
                break;

            int randIndex = UnityEngine.Random.Range(0, availableCells.Count);
            var chosenCell = availableCells[randIndex];
            if (chosenCell == null || chosenCell.Item == null)
                continue;

            List<Cell> sameTypeCells = availableCells
                .Where(c => c != chosenCell && c != null && c.Item != null && c.IsSameType(chosenCell))
                .ToList();
            m_bottomRow.SwapCells(chosenCell);

            foreach (var cell in sameTypeCells)
            {
                if (cell == null || cell.Item == null)
                {
                    continue;
                }

                m_bottomRow.SwapCells(cell);
                yield return new WaitForSeconds(1f);
            }
            yield return new WaitForSeconds(0.5f);
        }


        m_gameManager.SetState(GameManager.eStateGame.GAME_WON);
    }

    public void StartAutoLoss()
    {
        StartCoroutine(AutoLossCoroutine());
    }

    private IEnumerator AutoLossCoroutine()
    {
        while (true)
        {
            List<Cell> availableCells = new List<Cell>();
            for (int x = 0; x < m_gameSettings.BoardSizeX; x++)
            {
                for (int y = 0; y < m_gameSettings.BoardSizeY; y++)
                {
                    Cell cell = m_board.GetCell(x, y);
                    if (cell != null && cell.Item != null)
                        availableCells.Add(cell);
                }
            }

            if (availableCells.Count < 2) 
                break;

            var firstCell = availableCells[UnityEngine.Random.Range(0, availableCells.Count)];

            var differentCells = availableCells
                .Where(c => c != firstCell && !c.IsSameType(firstCell))
                .ToList();
            if (differentCells.Count == 0)
                break;

            var secondCell = differentCells[UnityEngine.Random.Range(0, differentCells.Count)];

            m_bottomRow.SwapCells(firstCell);
            yield return new WaitForSeconds(0.5f);
            m_bottomRow.SwapCells(secondCell);

            yield return new WaitForSeconds(0.5f);
        }
    }


}
