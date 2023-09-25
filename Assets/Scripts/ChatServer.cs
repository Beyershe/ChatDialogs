using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEditor.Experimental.GraphView;

public class ChatServer : NetworkBehaviour
{
    public ChatUi chatUI;
    const ulong SYSTEM_ID = ulong.MaxValue;
    ulong[] dmClientIds = new ulong[2];

    void Start()
    {
        chatUI.printEnteredText = false;
        chatUI.MessageEntered += OnChatUiMessageEntered;

        if(IsServer)
        {
            NetworkManager.OnClientConnectedCallback += ServerOnClientConnected;
            NetworkManager.OnClientDisconnectCallback += ServerOnClientDisconnected;
            if(IsHost)
            {
                DisplayMessageLocally(SYSTEM_ID, $"You are the host and player {NetworkManager.LocalClientId}");
            }
            else
            {
                DisplayMessageLocally(SYSTEM_ID, "You are the server");
            }
            
        }
        else
        {
            DisplayMessageLocally(SYSTEM_ID, $"You are player {NetworkManager.LocalClientId}");
        }

    }

    private void ServerOnClientConnected(ulong clientId)
    {
        ServerSendDirectMessage($" I ({NetworkManager.LocalClientId}) see you ({clientId}) have connected. Welcome!", NetworkManager.LocalClientId, clientId);
        DisplayMessageLocally(clientId, $"{clientId} has connected");

    }

    private void ServerOnClientDisconnected(ulong clientId)
    {
        DisplayMessageLocally(clientId, $"{clientId} has disconnected");
    }

    private void DisplayMessageLocally(ulong from, string message)
    {
        string fromStr = $"Player {from}";
        Color textColor = chatUI.defaultTextColor;

        if(from == NetworkManager.LocalClientId)
        {
            fromStr = "You";
            textColor = Color.blue;
        }else if(from == SYSTEM_ID)
        {
            fromStr = "SYS";
            textColor = Color.red;
        }
        chatUI.addEntry(fromStr, message, textColor);
    }

    private void OnChatUiMessageEntered(string message)
    {
        SendChatMessageServerRpc(message);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendChatMessageServerRpc(string message, ServerRpcParams serverRpcParams = default)
    {
        if (message.StartsWith("@"))
        {
            string[] parts = message.Split(" ");
            string clientIdStr = parts[0].Replace("@", " ");
            ulong toClientId = ulong.Parse(clientIdStr);
            
            ServerSendDirectMessage(message, serverRpcParams.Receive.SenderClientId, toClientId);
        }
        else
        {
            ReceiveChatMesageClientRpc(message, serverRpcParams.Receive.SenderClientId);
        }

    }

    [ClientRpc]
    public void ReceiveChatMesageClientRpc(string message, ulong from, ClientRpcParams clientRpcParams = default)
    {
        DisplayMessageLocally(from, message);
    }

    private void ServerSendDirectMessage(string message, ulong from, ulong to)
    {

        dmClientIds[0] = from;
        dmClientIds[1] = to;
        ClientRpcParams rpcParams = default;
        rpcParams.Send.TargetClientIds = dmClientIds;

        ReceiveChatMesageClientRpc($"<whisper> {message}", from, rpcParams);
    }

}
