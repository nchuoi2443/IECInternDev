using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelAutoLoss : LevelCondition
{
    private GameManager m_mngr;

    public override void Setup(float value, Text txt, BoardController board)
    {
        base.Setup(value, txt);
        m_txt.text = "";

        UpdateText();
    }

    private void Update()
    {

        //OnConditionComplete();
    }
}
