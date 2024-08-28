using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SignalingMessageType
{ 
    OFFER,     // 제안
    ANSWER,    // 답변
    CANDIDATE, // 후보자(대기자)
    OTHER      // 다른거
}
