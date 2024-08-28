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
        var host = Dns.GetHostEntry(Dns.GetHostName()); // �� ȣ��Ʈ

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                serverIpv4Address = ip.ToString();
                //Debug.Log(ip.ToString());
                break;
            }
        }

        // �� ��Ĺ ����
        wssv = new WebSocketServer($"ws://{serverIpv4Address}:{serverPort}");

        // �� ���� ���� �߰� 
        wssv.AddWebSocketService<SimpleDataChannelService>($"/{nameof(SimpleDataChannelService)}");

        // �� ���� ����
        wssv.Start();
    }

    private void OnDestroy()
    {
        wssv.Stop();
    }
}
