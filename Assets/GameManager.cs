using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using NativeWebSocket;
using System.Text;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Slider leftSlider;
    [SerializeField] private Slider rightSlider;

    [SerializeField] private Text leftText;
    [SerializeField] private Text rightText;

    [SerializeField] private Text xText;
    [SerializeField] private Text yText;
    [SerializeField] private Text zText;

    private ulong timer = 0;

    private WebSocket webSocket;


    private void Start()
    {
        StartCoroutine(GetLocalRobotIP("http://api.metater.tk:5000/getlocalrobotip"));
    }

    private async void InitWebSocket(string hostname)
    {
        webSocket = new WebSocket($"ws://{hostname}:81");

        webSocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
        };

        webSocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        webSocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
        };

        webSocket.OnMessage += (bytes) =>
        {
            Debug.Log("OnMessage!");
            Debug.Log(Encoding.UTF8.GetString(bytes));

            // getting the message as a string
            // var message = System.Text.Encoding.UTF8.GetString(bytes);
            // Debug.Log("OnMessage! " + message);
        };

        //InvokeRepeating("SendSpeeds", 0.0f, 0.5f);

        // waiting for messages
        await webSocket.Connect();
    }

    private void FixedUpdate()
    {
        if (timer % 5 == 0)
            SendSpeeds();
        timer++;
    }

    private void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
            if (webSocket != null) webSocket.DispatchMessageQueue();
        #endif

        Vector3 orientation = Input.acceleration;
        xText.text = "X: " + orientation.x;
        yText.text = "Y: " + orientation.y;
        zText.text = "Z: " + orientation.z;
    }

    async void SendSpeeds()
    {
        if (webSocket.State == WebSocketState.Open)
        {
            (int, int) speeds = GetSpeeds();
            byte leftSpeed = (byte)speeds.Item1;
            byte rightSpeed = (byte)speeds.Item2;
            //Debug.Log("Left Speed: " + leftSpeed + " Right Speed: " + rightSpeed);
            await webSocket.Send(new byte[] { leftSpeed, rightSpeed });
        }
    }

    private async void OnApplicationQuit()
    {
        await webSocket.Close();
    }

    //public Vector3 orientation;

    private (int, int) GetSpeeds()
    {
        Vector3 orientation = Input.acceleration;

        float power = 90;
        int leftValue = 90, rightValue = 90;

        bool forward = orientation.z < 0;
        const float zThresh = 0.15f;
        const float forwardZRange = 0.95f;
        const float reverseZRange = 0.75f;
        if (Mathf.Abs(orientation.z) >= zThresh)
        {
            float powerMagnitude;
            if (forward)
            {
                powerMagnitude = Mathf.Abs(((-orientation.z) - zThresh) / (forwardZRange - zThresh));
                powerMagnitude = Mathf.Clamp01(powerMagnitude);
                power = Mathf.Lerp(90, 0, powerMagnitude);
            }
            else
            {
                powerMagnitude = Mathf.Abs((orientation.z - zThresh) / (reverseZRange - zThresh));
                powerMagnitude = Mathf.Clamp01(powerMagnitude);
                power = Mathf.Lerp(90, 180, powerMagnitude);
            }
            //Debug.Log("Power Magnitude: " + powerMagnitude);
        }

        bool left = orientation.x < 0;
        const float xThresh = 0.1f;
        const float xRange = 1f;
        if (Mathf.Abs(orientation.x) >= xThresh)
        {
            float turnMagnitude = Mathf.Abs((orientation.x - xThresh) / (xRange - xThresh));
            turnMagnitude = Mathf.Clamp01(turnMagnitude);
            if (left)
            {
                if (forward)
                {
                    leftValue = (int)Mathf.Lerp(power, 90, turnMagnitude);
                    rightValue = (int)power;
                }
                else
                {
                    leftValue = (int)power;
                    rightValue = (int)Mathf.Lerp(power, 90, turnMagnitude);
                }
            }
            else
            {
                if (forward)
                {
                    leftValue = (int)power;
                    rightValue = (int)Mathf.Lerp(power, 90, turnMagnitude);
                }
                else
                {
                    leftValue = (int)Mathf.Lerp(power, 90, turnMagnitude);
                    rightValue = (int)power;
                }
            }
            //Debug.Log("Turn Magnitude: " + turnMagnitude);
        }
        else
        {
            leftValue = (int)power;
            rightValue = (int)power;
        }

        return (leftValue, rightValue);
    }

    private IEnumerator GetLocalRobotIP(string uri)
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
                    InitWebSocket(webRequest.downloadHandler.text);
                    break;
            }
        }
    }
}
