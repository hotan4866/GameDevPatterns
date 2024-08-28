using System;
using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;

public class SimpleDataChannelSender : MonoBehaviour
{
    [SerializeField] private bool sendMessageViaChannel = false;

    public string connectIp = "192.168.0.36";
    public Int32 connectPort = 8080;

    private RTCPeerConnection connection;
    private RTCDataChannel dataChannel;

    private WebSocket ws;
    private string clientId;

    private bool hasReceivedAnswer = false;
    private SessionDescription receivedAnswerSessionDescTemp;

    private void Start()
    {
        InitClent(connectIp, connectPort);
    }

    private void Update()
    {
        if (hasReceivedAnswer)
        {
            hasReceivedAnswer = !hasReceivedAnswer;
            StartCoroutine(SetRemoteDesc());
        }
        if (sendMessageViaChannel)
        {
            sendMessageViaChannel = !sendMessageViaChannel;
            dataChannel.Send("테스트! 테스트 테스트");
        }
    }

    private void OnDestroy()
    {
        dataChannel.Close();
        connection.Close();
    }

    private void InitClent(string serverIp, int serverPort)
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
                case "ANSWER":
                    Debug.Log(clientId + " - ANSWER : " + requestData);
                    receivedAnswerSessionDescTemp = SessionDescription.FromJSON(requestData);
                    hasReceivedAnswer = true;
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

        connection.OnIceConnectionChange = state => {
            Debug.Log(state);
        };

        dataChannel = connection.CreateDataChannel("sendChannel");
        
        dataChannel.OnOpen = () => {
            Debug.Log("Sender opened channdel");
        };

        dataChannel.OnClose = () => {
            Debug.Log("Sender clodsed channdel");
        };

        connection.OnNegotiationNeeded = () => {
            StartCoroutine(CreateOffer());
        };
    }

    private IEnumerator CreateOffer()
    {
        var offer = connection.CreateOffer();
        yield return offer;

        var offerDesc = offer.Desc;
        var localDescOp = connection.SetLocalDescription(ref offerDesc);
        yield return localDescOp;

        var offerSessionDesc = new SessionDescription()
        {
            SessionType = offerDesc.type.ToString(),
            Sdp = offerDesc.sdp,
        };

        ws.Send("OFFER!" + offerSessionDesc.ConvertToJSON());
    }

    private IEnumerator SetRemoteDesc()
    {
        RTCSessionDescription answerSessionDesc = new RTCSessionDescription();
        answerSessionDesc.type = RTCSdpType.Answer;
        answerSessionDesc.sdp = receivedAnswerSessionDescTemp.Sdp;

        var remoteDescOp = connection.SetRemoteDescription(ref answerSessionDesc);
        yield return remoteDescOp;
    }
}
