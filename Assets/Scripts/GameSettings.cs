using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettings : ScriptableObject
{
    public int BoardSizeX = 5;

    public int BoardSizeY = 5;

    public int BottomRowSizeX = 5;

    public int BottomRowSizeY = 1;

    public int MatchesMin = 3;

    public int LevelMoves = 16;

    public float LevelTime = 30f;

    public float TimeForHint = 5f;

    public float LevelAutoWin = 0f;

    public float LevelAutoLoss = 0f;
}
