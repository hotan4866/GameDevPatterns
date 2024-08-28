using System;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;
using System.Text;
using System.Collections;

public class SimpleDataChannelReceiver : MonoBehaviour
{
    private RTCPeerConnection connection;
    private RTCDataChannel dataChannel;

    public string connectIp = "192.168.0.36";
    public Int32 connectPort = 8080;

    private WebSocket ws;
    private string clientId;

    private bool hasReceivedOffer = false;
    private SessionDescription receivedOfferSessionDescTemp; 


    private void Start()
    {
        InitClient(connectIp, 8080);
    }

    private void Update()
    {
        if (hasReceivedOffer)
        {
            hasReceivedOffer = !hasReceivedOffer;
            StartCoroutine(CreateAnswer());
        }
    }

    private IEnumerator CreateAnswer()
    {
        RTCSessionDescription offerSessionDesc = new RTCSessionDescription();
        offerSessionDesc.type = RTCSdpType.Offer;
        offerSessionDesc.sdp = receivedOfferSessionDescTemp.Sdp;

        var remoteDescOp = connection.SetRemoteDescription(ref offerSessionDesc);
        yield return remoteDescOp;

        var answer = connection.CreateAnswer();
        yield return answer;

        var answerDesc = answer.Desc;
        var localDescOp = connection.SetLocalDescription(ref answerDesc);
        yield return localDescOp;

        var snswerSessionDesc = new SessionDescription()
        { 
            SessionType = answerDesc.type.ToString(),
            Sdp = answerDesc.sdp,
        };

        ws.Send("ANSWER!" + snswerSessionDesc.ConvertToJSON());
    }

    private void OnDestroy()
    {
        dataChannel.Close();
        connection.Close();
    }

    // 클라이언트 초기화
    private void InitClient(string serverIp, int serverPort)
    {
        int port = serverPort == 0 ? 8080 : serverPort;
        clientId = gameObject.name;

        ws = new WebSocket($"ws://{serverIp}:{port}/{nameof(SimpleDataChannelService)}");

        ws.OnMessage += (sender, e) => { 
            var requestArray = e.Data.Split("!");
            var requestType = requestArray[0];
            var requestData = requestArray[1];

            switch (requestType)
            {
                case "OFFER":
                    Debug.Log(clientId + " - OFFER : " + requestData);
                    receivedOfferSessionDescTemp = SessionDescription.FromJSON(requestData);
                    hasReceivedOffer = true;
                    break;
                case "CANDIDATE":
                    Debug.Log(clientId + " - CANDIDATE : " + requestData);

                    // 생성 candidate data
                    var candidateInit = CandidateInit.FromJSON(requestData);
                    RTCIceCandidateInit init = new RTCIceCandidateInit();
                    init.sdpMid = candidateInit.SdpMid;
                    init.sdpMLineIndex = candidateInit.SdpMLineIndex;
                    init.candidate = candidateInit.Candidate;
                    RTCIceCandidate candidate = new RTCIceCandidate(init);

                    // ADD candidate
                    connection.AddIceCandidate(candidate);
                    break;
                default:
                    Debug.Log(clientId + " - error : " + e.Data);
                    break;
            }

        }; 

        // 
        ws.Connect();

        connection = new RTCPeerConnection();
        connection.OnIceCandidate = candidate =>
        {
            Debug.Log("CANDIDATE = " + candidate.Candidate);

            var candidateInit = new CandidateInit()
            {
                SdpMid = candidate.SdpMid,
                SdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                Candidate = candidate.Candidate
            };
            ws.Send("CANDIDATE!" + candidateInit.ConvertToJSON());

        };

        // 상태 
        connection.OnIceConnectionChange = state =>
        {
            Debug.Log("상태 : " + state);
        };

        // 채널
        connection.OnDataChannel = channel =>
        {
            dataChannel = channel;
            dataChannel.OnMessage = bytes =>
            {
                var message = Encoding.UTF8.GetString(bytes);
                Debug.Log("Receiver received: " + message);
            };
        };
    }
}
