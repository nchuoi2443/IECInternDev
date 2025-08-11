using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action<eStateGame> StateChangedAction = delegate { };

    public enum eLevelMode
    {
        TIMER,
        MOVES,
        AUTOLOSE,
        AUTOWIN,
    }

    public enum eStateGame
    {
        SETUP,
        MAIN_MENU,
        GAME_STARTED,
        PAUSE,
        GAME_OVER,
        GAME_WON,
    }

    private eStateGame m_state;
    public eStateGame State
    {
        get { return m_state; }
        private set
        {
            m_state = value;

            StateChangedAction(m_state);
        }
    }

    private eLevelMode m_currentGameMode;
    public eLevelMode CurrentGameMode
    {
        get { return m_currentGameMode; }
        set { m_currentGameMode = value; }
    }

    private GameSettings m_gameSettings;

    private BottomRow m_bottomRow;

    private UIMainManager m_uiMenu;

    private LevelCondition m_levelCondition;

    public BoardController BoardController;

    private void Awake()
    {
        State = eStateGame.SETUP;

        m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);

        m_uiMenu = FindObjectOfType<UIMainManager>();
        m_uiMenu.Setup(this);
    }

    void Start()
    {
        State = eStateGame.MAIN_MENU;
    }

    // Update is called once per frame
    void Update()
    {
        if (BoardController != null) BoardController.Update();
    }


    internal void SetState(eStateGame state)
    {
        State = state;

        if(State == eStateGame.PAUSE)
        {
            DOTween.PauseAll();
        }
        else
        {
            DOTween.PlayAll();
        }
    }

    public void LoadLevel(eLevelMode mode)
    {
        m_bottomRow = new GameObject("BottomRow").AddComponent<BottomRow>();
        m_bottomRow.SetUp(m_bottomRow.transform, m_gameSettings);

        BoardController = new GameObject("BoardController").AddComponent<BoardController>();
        BoardController.StartGame(this, m_gameSettings, m_bottomRow);

        if (mode == eLevelMode.MOVES)
        {
            m_currentGameMode = eLevelMode.MOVES;
            m_levelCondition = this.gameObject.AddComponent<LevelMoves>();
            m_levelCondition.Setup(m_gameSettings.LevelMoves, m_uiMenu.GetLevelConditionView(), BoardController);
        }
        else if (mode == eLevelMode.TIMER)
        {
            m_currentGameMode = eLevelMode.TIMER;
            m_levelCondition = this.gameObject.AddComponent<LevelTime>();
            m_levelCondition.Setup(m_gameSettings.LevelTime, m_uiMenu.GetLevelConditionView(), this);
        } else if (mode == eLevelMode.AUTOWIN)
        {
            m_currentGameMode = eLevelMode.AUTOWIN;
            m_levelCondition = this.gameObject.AddComponent<LevelAutoWin>();
            m_levelCondition.Setup(m_gameSettings.LevelAutoWin, m_uiMenu.GetLevelConditionView(), this);
            BoardController.StartAutoWin();
        } else if (mode == eLevelMode.AUTOLOSE)
        {
            m_currentGameMode = eLevelMode.AUTOLOSE;
            m_levelCondition = this.gameObject.AddComponent<LevelAutoLoss>();
            m_levelCondition.Setup(m_gameSettings.LevelAutoLoss, m_uiMenu.GetLevelConditionView(), this);
            BoardController.StartAutoLoss();
        }

        m_levelCondition.ConditionCompleteEvent += GameOver;

        State = eStateGame.GAME_STARTED;
    }

    public void GameOver()
    {
        StartCoroutine(WaitBoardController());
    }

    internal void ClearLevel()
    {
        if (BoardController)
        {
            BoardController.Clear();
            Destroy(BoardController.gameObject);
            BoardController = null;
        }
    }

    private IEnumerator WaitBoardController()
    {
        while (BoardController.IsBusy)
        {
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(1f);

        State = eStateGame.GAME_OVER;

        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent -= GameOver;

            Destroy(m_levelCondition);
            m_levelCondition = null;
        }
    }
}
