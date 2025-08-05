using Unity.Netcode;
using UnityEngine;

public class NetworkVariableIntWrapper : INetworkSerializable
{
    private int value;

    public NetworkVariableIntWrapper(int initialValue)
    {
        value = initialValue;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref value);
    }

    public int GetValue()
    {
        return value;
    }
}

