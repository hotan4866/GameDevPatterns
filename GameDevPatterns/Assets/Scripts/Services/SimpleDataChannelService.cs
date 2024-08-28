using UnityEngine;
using WebSocketSharp.Server;
using WebSocketSharp;

public class SimpleDataChannelService : WebSocketBehavior
{


    protected override void OnMessage(MessageEventArgs e)
    {
        //Debug.Log(ID + " - 데이터 채널 서버에서 온 메세지 " + e.Data);

        // 메세지 모든 다른 클라이언트에 보내기
        foreach (var id in Sessions.ActiveIDs)
        {
            if (id != ID)
            {
                Sessions.SendTo(e.Data, id);
            }
        }
    }

    //서비스 준비 완료시에 출력 
    protected override void OnOpen()
    {
        Debug.Log("서버 간단한 데이터 채널 서비스 시작!");
    }
}
