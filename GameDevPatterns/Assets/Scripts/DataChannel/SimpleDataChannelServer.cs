using System.Net;
using System.Net.Sockets;
using UnityEngine;
using WebSocketSharp.Server;

public class SimpleDataChannelServer : MonoBehaviour
{
    public string serverIpv4Address;
    public int serverPort = 8080;

    private WebSocketServer wssv;

    private void Awake()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName()); // 내 호스트

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                serverIpv4Address = ip.ToString();
                //Debug.Log(ip.ToString());
                break;
            }
        }

        // 웹 소캣 생성
        wssv = new WebSocketServer($"ws://{serverIpv4Address}:{serverPort}");

        // 웹 서비스 내용 추가 
        wssv.AddWebSocketService<SimpleDataChannelService>($"/{nameof(SimpleDataChannelService)}");

        // 웹 서버 구동
        wssv.Start();
    }

    private void OnDestroy()
    {
        wssv.Stop();
    }
}
