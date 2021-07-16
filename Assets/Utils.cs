using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Utils
{
    public static IEnumerator GetRequest(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
                    break;
            }
        }
    }

    public static void SendValues(int leftValue, int rightValue)
    {
        StartCoroutine(GetRequest($"http://api.metater.tk:5000/setrobot?left={leftValue}&right={rightValue}"));
        float leftSpeed = (int)(((leftValue - 90f) / -0.9f));
        float rightSpeed = (int)(((rightValue - 90f) / -0.9f));
        //leftText.text = (leftSpeed / 100).ToString();
        //rightText.text = (rightSpeed / 100).ToString();
        Debug.Log("Left: " + leftValue + " Right: " + rightValue);
    }

    private static void StartCoroutine(IEnumerator enumerator)
    {
        throw new NotImplementedException();
    }
}
