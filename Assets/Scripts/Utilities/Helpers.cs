using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

/// <summary>
/// A static class for general helpful methods
/// </summary>
public static class Helpers
{
    public static T[] ToArray<T>(IReadOnlyList<T> readOnlyList)
    {
        List<T> list = new();

        foreach (T element in readOnlyList)
        {
            list.Add(element);
        }

        return list.ToArray<T>();
    }

    public static ClientRpcParams GenerateRpcParamsToClient(this ulong playerClientId)
    {
        ulong[] allClientIds = Helpers.ToArray(NetworkManager.Singleton.ConnectedClientsIds);

        return new ClientRpcParams()
        {
            Send = new ClientRpcSendParams { TargetClientIds = allClientIds.Where((ulong clientId) => clientId == playerClientId).ToArray() }
        };
    }
}
