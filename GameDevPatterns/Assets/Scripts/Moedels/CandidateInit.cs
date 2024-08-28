using System;
using UnityEngine;

[Serializable]
public class CandidateInit : IJsonObject<CandidateInit>
{
    public string Candidate;
    public string SdpMid;
    public int SdpMLineIndex;


    // 다시 되돌리기
    public static CandidateInit FromJSON(string jsonString)
    { 
        return JsonUtility.FromJson<CandidateInit>(jsonString);
    }

    //json 으로 변경
    public string ConvertToJSON()
    {
        return JsonUtility.ToJson(this);
    }
}
