using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.IO.Compression;
using UnityEngine.SceneManagement;
using System.Security.Policy;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public class NetClient : MonoBehaviour
{
    public static NetClient netClient = null;

    [SerializeField] private string PlayerName = "Default";
    public GameObject PlayerPrefab;
    public GameObject PropPrefab;
    public bool IsConnected => (client != null)? true:false;

    private static int BUFFERSIZE = 1024;
    private static string address = "127.0.0.1";
    private static Socket client;
    private static byte[] incMsgBuffer = new byte[BUFFERSIZE];
    private static Queue<byte[]> outMsgBuffer = new Queue<byte[]>();
    private static StringBuilder builder = new StringBuilder();
    private Coroutine serverPing;

    public static int PlayerID = -1;
    public static int Seed = 0;
    public static bool PlayersReady;

    void Start()
    {
        if(netClient == null)
        {
            netClient = this;
            DontDestroyOnLoad(this);
        }
        else
            Destroy(this);
        try
        {
            client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            client.SendTimeout = 10;
            StartCoroutine(SocketWriter());
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }

    private void Update()
    {
        
        while (client != null  && client.Available != 0)
        {
            ReadStream();
        }
        if (SceneManager.GetActiveScene().name == "MainMenu" && serverPing == null && client != null)
        {
            serverPing = StartCoroutine(PingServer());
        }
        else if(SceneManager.GetActiveScene().name != "MainMenu" || client == null)
        {
            if(serverPing != null)
            StopCoroutine(serverPing);
            serverPing = null;
        }
    }

    private IEnumerator PingServer()
    {
            
        while (true)
        {
            SendServerCode(0);
            yield return new WaitForSeconds(60);
        }
    }

    private IEnumerator SocketWriter()
    {
        IPEndPoint iipPoint = new IPEndPoint(IPAddress.Parse(address), 8005);
        while (true)
        {
            if(outMsgBuffer.Count != 0)
            {
                byte[] data = outMsgBuffer.Dequeue();
                
                client.SendTo(data, data.Length, 0, iipPoint);
                //Debug.Log("Sended : " + );
            }
            yield return null;
        }
    }

    public static void Disconnect()
    {
        client.Close();
    }

    public static void SendServerCode(int code)
    {
        byte[] Cbyte = { 0 };
        IPEndPoint iipPoint = new IPEndPoint(IPAddress.Parse(address), 8005);
        byte []buff = Encoding.ASCII.GetBytes(code.ToString());
        try
        {
            client.SendTo(buff, buff.Length, 0, iipPoint);
        }
        catch(Exception ex)
        {
            Debug.Log(ex);
        }
    }

    public void ReadStream()
    {
        byte[] Cbyte = { 0 };
        int bytes = 0;
        try
        {
            builder.Clear();
            // получаем сообщение
            Array.Clear(incMsgBuffer, 0, incMsgBuffer.Length);
            if (client == null) return;
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint senderRemote = (EndPoint)sender;
            bytes = client.ReceiveFrom(incMsgBuffer, ref senderRemote);
            if (bytes < 50)
            {
                string responce = builder.Append(Encoding.ASCII.GetString(incMsgBuffer,0,bytes)).ToString();
                int code = int.Parse(responce.Substring(0, 3));
                NetServerResponce.ParseCode(code, responce.Substring(4, responce.IndexOf('\0') - 4));
                return;
            }
            IFormatter formatter = new BinaryFormatter();
            incMsgBuffer = Zip(incMsgBuffer, CompressionMode.Decompress);
            NetForms rawForm = (NetForms)formatter.Deserialize(new MemoryStream(incMsgBuffer));
            NetForms.ProceedForm(rawForm);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Received bytes: " + bytes + ". "+ ex);
        }
    }

    public static int SendForm(NetForms form)
    {

        if (client == null) return -1;
        try
        {
            using (var memStream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(memStream, form);
                outMsgBuffer.Enqueue(Zip(memStream.GetBuffer(), CompressionMode.Compress));
                return 0;
            }       
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            return 1;
        }
    }

    #region Compressing
    public static byte[] Zip(byte[] target , CompressionMode mode)
    {
        using (var msi = new MemoryStream(target))
        using (var mso = new MemoryStream())
        {
            using (var ZipStream = new GZipStream((mode == CompressionMode.Compress)?mso:msi, mode))
            {
                if (mode == CompressionMode.Compress)
                    CopyTo(msi, ZipStream);
                else
                    CopyTo(ZipStream, mso);
            }
            return mso.ToArray();
        }
    }

    private static void CopyTo(Stream src, Stream dst)
    {
        byte[] bytes = new byte[BUFFERSIZE];
        int Amount;
        while ((Amount = src.Read(bytes, 0, bytes.Length)) != 0)
            dst.Write(bytes, 0, Amount);
    }
    #endregion
}
