using UnityEngine;
using WebSocketSharp.Server;
using WebSocketSharp;

public class SimpleDataChannelService : WebSocketBehavior
{


    protected override void OnMessage(MessageEventArgs e)
    {
        //Debug.Log(ID + " - ������ ä�� �������� �� �޼��� " + e.Data);

        // �޼��� ��� �ٸ� Ŭ���̾�Ʈ�� ������
        foreach (var id in Sessions.ActiveIDs)
        {
            if (id != ID)
            {
                Sessions.SendTo(e.Data, id);
            }
        }
    }

    //���� �غ� �Ϸ�ÿ� ��� 
    protected override void OnOpen()
    {
        Debug.Log("���� ������ ������ ä�� ���� ����!");
    }
}
