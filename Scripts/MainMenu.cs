using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public static MainMenu instance;

    public static CharControls localPlayer;

    private void Start()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(instance);
            instance = this;
            DontDestroyOnLoad(this);
        }
    }


    public void FindLobby()
    {
        NetClient.SendServerCode(20);
    }

    public void ReturnToMainMenu()
    {
        if (NetClient.netClient.IsConnected)
        {
            //NetClient.SendServerCode(29);
            NetClient.Disconnect();
            NetClient.Seed = 0;
            NetClient.PlayerID = -1;
        }
        StartCoroutine(LoadMenu());
    }

    private IEnumerator LoadMenu()
    {
        
        yield return null;
        SceneManager.LoadScene("SplashScreen");
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("MainMenu");

        asyncOperation.allowSceneActivation = false;
        Debug.Log("Pro :" + asyncOperation.progress);

        while (!asyncOperation.isDone)
        {
            //Output the current progress
            //m_Text.text = "Loading progress: " + (asyncOperation.progress * 100) + "%";

            // Check if the load has finished
            if (asyncOperation.progress >= 0.9f)
            {
                asyncOperation.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    public static IEnumerator LoadNextLevel()
    {
        SceneManager.LoadScene("SplashScreen");
        yield return null;
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("RaceScene");

        asyncOperation.allowSceneActivation = false;
        Debug.Log("Pro :" + asyncOperation.progress);

        while (!asyncOperation.isDone)
        {
            //Output the current progress
            //m_Text.text = "Loading progress: " + (asyncOperation.progress * 100) + "%";

            // Check if the load has finished
            if (asyncOperation.progress >= 0.9f)
            {
                asyncOperation.allowSceneActivation = true;
            }
            yield return null;
        }
        yield return new WaitForSeconds(0.5f);
        var locPlayer = NetPlayer.InstantiateNetPlayer(NetClient.netClient.PlayerPrefab, (NetClient.PlayerID * 100) + 1, new Vector3(0, 2.5f, 0));
        localPlayer = locPlayer.charControls;
        var ragColliders = locPlayer.charControls.RagDoll.GetComponentsInChildren<Collider>();
        var Brain = Camera.main.GetComponent<Cinemachine.CinemachineBrain>();
        Brain.ActiveVirtualCamera.Follow = ragColliders[0].transform;
        Brain.ActiveVirtualCamera.LookAt = ragColliders[6].transform;
        if (NetClient.netClient.IsConnected)
            NetClient.SendServerCode(21);
    }
}
