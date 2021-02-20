using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerHandle
{
    public static void WelcomeReceived(int _fromClient, Packet _packet)
    {
        int _clientIdCheck = _packet.ReadInt();
        string _username = _packet.ReadString();

        Debug.Log($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected succesfully and is now player {_fromClient}.");
        if (_fromClient != _clientIdCheck)
        {
            Debug.Log($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
        }
        Server.clients[_fromClient].SendIntoGame(_username);
    }

    public static void RequestServer(int _fromClient, Packet _packet)
    {
        int _clientId = _packet.ReadInt();

        ServerSend.ServerRespondToClient(_clientId);

        //Debug.Log($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected succesfully and is now player {_fromClient}.");
        //if (_fromClient != _clientIdCheck)
        //{
        //    Debug.Log($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
        //}
        //Server.clients[_fromClient].SendIntoGame(_username);
    }

    public static void PlayerMovement(int _fromClient, Packet _packet)
    {
        int _requestId = _packet.ReadInt();
        bool[] _inputs = new bool[_packet.ReadInt()];
        for (int i = 0; i < _inputs.Length; i++)
        {
            _inputs[i] = _packet.ReadBool();
        }
        Quaternion _rotation = _packet.ReadQuaternion();
        float _headXRotation = _packet.ReadFloat();

        Server.clients[_fromClient].player.SetInput(_requestId, _inputs, _rotation, _headXRotation);
    }

    public static void PlayerTryPickUpWeapon(int _fromClient, Packet _packet)
    {
        Vector3 _direction = _packet.ReadVector3();

        Server.clients[_fromClient].player.weaponsController.TryPickUpWeapon(_direction);
    }

    public static void PlayerTryDropWeapon(int _fromClient, Packet _packet)
    {
        int _weaponId = _packet.ReadInt();

        int _usedWeapon = _packet.ReadInt();
        Vector3 _direction = _packet.ReadVector3();

        bool _includeStartPosAndRot = _packet.ReadBool();
        if (_includeStartPosAndRot)
        {
            Vector3 _throwStartPos = _packet.ReadVector3();
            Vector3 _throwStartRot = _packet.ReadVector3();

            Server.clients[_fromClient].player.weaponsController.TryDropWeapon(_weaponId, _usedWeapon, _throwStartPos, _throwStartRot, _direction);
        }
        else
        {
            Server.clients[_fromClient].player.weaponsController.TryDropWeapon(_weaponId, _usedWeapon, _direction);
        }
    }

    public static void PlayerShoot(int _fromClient, Packet _packet)
    {
        Vector3 _playerOrigin = _packet.ReadVector3();
        Vector3 _shootDirection = _packet.ReadVector3();
        int _fireModeInt = _packet.ReadInt();

        Server.clients[_fromClient].player.Shoot(_playerOrigin, _shootDirection, _fireModeInt);
    }

    public static void PlayerWeaponUsed(int _fromClient, Packet _packet)
    {
        int _weaponUsed = _packet.ReadInt();

        Server.clients[_fromClient].player.weaponsController.weaponUsed = _weaponUsed;
        Server.clients[_fromClient].player.weaponsController.ChangeWeaponUsed(); 
    }

    public static void PlayerFireMode(int _fromClient, Packet _packet)
    {
        int _fireModeInt = _packet.ReadInt();

        Server.clients[_fromClient].player.weaponsController.weaponsEquiped[Server.clients[_fromClient].player.
            weaponsController.weaponUsed].GetComponent<Weapon>().SetFireMode(_fireModeInt);
    }

    public static void PlayerReload(int _fromClient, Packet _packet)
    {
        int _weapon = _packet.ReadInt();

        Server.clients[_fromClient].player.weaponsController.Reload(_weapon);
    }

    public static void PlayerThrowItem(int _fromClient, Packet _packet)
    {
        Vector3 _throwDirection = _packet.ReadVector3();

        Server.clients[_fromClient].player.ThrowItem(_throwDirection);
    }
}
