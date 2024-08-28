using System;

public class SignalingMessage
{
    public readonly SignalingMessageType Type;
    public readonly string Message;

    public SignalingMessage(string messageString)
    {
        var messageArray = messageString.Split("!");

        if (messageArray.Length < 2) // 잘못된
        {
            Type = SignalingMessageType.OTHER;
            Message = messageString;
        }
        else if (Enum.TryParse(messageArray[0], out SignalingMessageType resultType)) // 올바는 요구
        {
            Type = resultType;
            Message = messageArray[1];
        }
    }
}
