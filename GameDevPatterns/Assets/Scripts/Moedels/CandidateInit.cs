using System;
using UnityEngine;

[Serializable]
public class CandidateInit : IJsonObject<CandidateInit>
{
    public string Candidate;
    public string SdpMid;
    public int SdpMLineIndex;


    // �ٽ� �ǵ�����
    public static CandidateInit FromJSON(string jsonString)
    { 
        return JsonUtility.FromJson<CandidateInit>(jsonString);
    }

    //json ���� ����
    public string ConvertToJSON()
    {
        return JsonUtility.ToJson(this);
    }
}
