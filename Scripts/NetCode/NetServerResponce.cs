using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NetServerResponce
{
    public static void ParseCode(int code, string data)
    {
        switch (code)
        {
            case 20: 
                NetClient.PlayerID = int.Parse(data);
                Debug.Log("Received innerID: " + NetClient.PlayerID);
                break;
            case 21:
                NetClient.Seed = int.Parse(data);
                Debug.Log("Received seed: " + NetClient.Seed);
                break;
            case 22:
                Debug.Log("IsAllowed: " + int.Parse(data));
                MainMenu.instance.StartCoroutine(MainMenu.LoadNextLevel());
                break;
            case 23:
                NetClient.PlayersReady = true;
                break;
            case 24:
                InGameUI.gameUI.Fin.gameObject.SetActive(true);
                Debug.Log("You won!");
                break;
        }
    }
}
