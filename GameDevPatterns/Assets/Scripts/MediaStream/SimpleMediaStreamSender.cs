using System;
using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class SimpleMediaStreamSender : MonoBehaviour
{
    [SerializeField] private Camera cameraStream;
    [SerializeField] private RawImage sourceImage;


    public string connectIp = "192.168.0.36";
    public Int32 connectPort = 8080;

    private RTCPeerConnection connection;
    private MediaStream videoStream;
    private VideoStreamTrack videoStreamTrack;

    private WebSocket ws;
    private string clientId;

    private bool hasReceivedAnswer = false;
    private SessionDescription receivedAnswerSessionDescTemp;

    private void Start()
    {
        InitClent(connectIp, connectPort);
        //WebCamPlay();

    }

    public Renderer display;
    WebCamTexture camTexture;
    private int currentIndex = 0;

    private void WebCamPlay()
    {
        /*
        // 모든 웹캠 장치 목록을 가져옵니다.
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.Log("No webcam detected.");
            return;
        }

        // 첫 번째 웹캠을 사용하여 WebCamTexture를 생성합니다.
        WebCamTexture webcamTexture = new WebCamTexture(devices[0].name);
        // RawImage 컴포넌트에 웹캠 영상을 할당합니다.
        sourceImage.texture = webcamTexture;
        // 웹캠을 시작합니다.
        webcamTexture.Play();
        */

    }

    private void Update()
    {
        if (hasReceivedAnswer)
        {
            hasReceivedAnswer = !hasReceivedAnswer;
            StartCoroutine(SetRemoteDesc());
        }
    }


    private void InitClent(string serverIp, int serverPort)
    {
        int port = serverPort == 0 ? 8080 : serverPort;
        clientId = gameObject.name;

        ws = new WebSocket($"ws://{serverIp}:{port}/{nameof(SimpleDataChannelService)}");

        ws.OnMessage += (sender, e) =>
        {

            var signalingMessage = new SignalingMessage(e.Data);

            switch (signalingMessage.Type)
            {
                case SignalingMessageType.ANSWER:
                    Debug.Log(clientId + " - ANSWER : " + signalingMessage.Message);
                    receivedAnswerSessionDescTemp = SessionDescription.FromJSON(signalingMessage.Message);
                    hasReceivedAnswer = true;
                    break;
                case SignalingMessageType.CANDIDATE:
                    Debug.Log(clientId + " - CANDIDATE : " + signalingMessage.Message);

                    var candidateInit = CandidateInit.FromJSON(signalingMessage.Message);
                    RTCIceCandidateInit init = new RTCIceCandidateInit();
                    init.sdpMid = candidateInit.SdpMid;
                    init.sdpMLineIndex = candidateInit.SdpMLineIndex;
                    init.candidate = candidateInit.Candidate;

                    RTCIceCandidate candidate = new RTCIceCandidate(init);
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
            var candidateInit = new CandidateInit()
            {
                SdpMid = candidate.SdpMid,
                SdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                Candidate = candidate.Candidate
            };

            ws.Send("CANDIDATE!" + candidateInit.ConvertToJSON());
        };

        connection.OnIceConnectionChange = state =>
        {
            Debug.Log(state);
        };

        connection.OnNegotiationNeeded = () =>
        {
            StartCoroutine(CreateOffer());
        };

        // 모든 웹캠 장치 목록을 가져옵니다.
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.Log("No webcam detected.");
            return;
        }

        // 첫 번째 웹캠을 사용하여 WebCamTexture를 생성합니다.
        WebCamTexture webcamTexture = new WebCamTexture(devices[0].name);
        // RawImage 컴포넌트에 웹캠 영상을 할당합니다.
        sourceImage.texture = webcamTexture;
        // 웹캠을 시작합니다.
        webcamTexture.Play();

        VideoStreamTrack test =  new VideoStreamTrack(webcamTexture);

        //videoStreamTrack = cameraStream.CaptureStreamTrack(1280, 720);
        videoStreamTrack = test;
        //sourceImage.texture = cameraStream.targetTexture;
        connection.AddTrack(videoStreamTrack);

        StartCoroutine(WebRTC.Update());
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

    private void OnDestroy()
    {
        videoStreamTrack.Stop();
        connection.Close();
        ws.Close();
    }
}
