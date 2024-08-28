using System;
using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class SimpleMediaStreamReceiver : MonoBehaviour
{
    [SerializeField] private RawImage recriveImage;

    public string connectIp = "192.168.0.36";
    public Int32 connectPort = 8080;

    private RTCPeerConnection connection;

    private WebSocket ws;
    private string clientId;

    private bool hasReceivedOffer = false;
    private SessionDescription receivedOfferSessionDescTemp;

    private string senderIp;
    private int senederPort;

    private void Start()
    {
        InitClient(connectIp, connectPort);
    }

    private void InitClient(string serverIp, int serverPort)
    {
        int port = serverPort == 0 ? 8080 : serverPort;
        clientId = gameObject.name;

        ws = new WebSocket($"ws://{serverIp}:{port}/{nameof(SimpleDataChannelService)}");

        ws.OnMessage += (sender, e) => {

            var signalingMessage = new SignalingMessage(e.Data);

            switch (signalingMessage.Type)
            {
                case SignalingMessageType.OFFER:
                    Debug.Log(clientId + " - OFFER : " + signalingMessage.Message);
                    receivedOfferSessionDescTemp = SessionDescription.FromJSON(signalingMessage.Message);
                    hasReceivedOffer = true;
                    break;
                case SignalingMessageType.CANDIDATE:
                    Debug.Log(clientId + " - CANDIDATE : " + signalingMessage.Message);

                    // 생성 candidate data
                    var candidateInit = CandidateInit.FromJSON(signalingMessage.Message);
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
        connection.OnTrack = e =>
        {

            Debug.Log("트랙 받음");

            if (e.Track is VideoStreamTrack video)
            {
                Debug.Log("비디오 확인");

                video.OnVideoReceived += tex =>
                {
                    Debug.Log("비디오 매핑");
                    recriveImage.texture = tex;
                };
            }
        };

        StartCoroutine(WebRTC.Update());
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

        var answerSessionDesc = new SessionDescription()
        {
            SessionType = answerDesc.type.ToString(),
            Sdp = answerDesc.sdp,
        };

        ws.Send("ANSWER!" + answerSessionDesc.ConvertToJSON());
    }
}
