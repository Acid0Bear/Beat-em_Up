using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    public static InGameUI @gameUI;

    public Text Fin;

    private void Awake()
    {
        if (gameUI == null)
            gameUI = this;
    }

    public void Jump()
    {
        MainMenu.localPlayer.TryJump();
    }
}
