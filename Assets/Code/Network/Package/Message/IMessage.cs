﻿namespace MiniGame.Network
{
    public interface IMessage
    {
        int MsgId { get; }
        int RespId { get; }
    }

    public interface IClientMessageEncoder
    {
        byte[] Encode(IMessage message);
    }

    public interface IClientMessageDecoder
    {
        IMessage Decode(byte[] bytes);
    }
}