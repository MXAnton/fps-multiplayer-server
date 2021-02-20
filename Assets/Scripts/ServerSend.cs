using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSend
{
    private static void SendTCPData(int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.clients[_toClient].tcp.SendData(_packet);
    }

    private static void SendUDPData(int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.clients[_toClient].udp.SendData(_packet);
    }

    private static void SendTCPDataToAll(Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].tcp.SendData(_packet);
        }
    }
    private static void SendTCPDataToAll(int _exceptClient, Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != _exceptClient)
            {
                Server.clients[i].tcp.SendData(_packet);
            }
        }
    }

    private static void SendUDPDataToAll(Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].udp.SendData(_packet);
        }
    }
    private static void SendUDPDataToAll(int _exceptClient, Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != _exceptClient)
            {
                Server.clients[i].udp.SendData(_packet);
            }
        }
    }

    #region Packets
    public static void Welcome(int _toClient, string _msg)
    {
        using (Packet _packet = new Packet((int)ServerPackets.welcome))
        {
            _packet.Write(_msg);
            _packet.Write(_toClient);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void ServerRespondToClient(int _toClient)
    {
        using (Packet _packet = new Packet((int)ServerPackets.serverRespondToClient))
        {
            _packet.Write(_toClient);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void SpawnPlayer(int _toClient, Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.username);
            _packet.Write(_player.transform.position);
            _packet.Write(_player.transform.rotation);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void LocalPlayerMovementVars(int _toClient, Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.localPlayerMovementVars))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.gravity);
            _packet.Write(_player.moveSpeed);
            _packet.Write(_player.runSpeedMultiplier);
            _packet.Write(_player.jumpSpeed);

            SendTCPData(_toClient, _packet);
        }
    }

    //Send position to all clients
    public static void PlayerPosition(Player _player, bool _doTeleport)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerPosition))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.transform.position);
            _packet.Write(_doTeleport);

            _packet.Write(_player.moveSpeed * _player.runSpeedMultiplier);

            _packet.Write(_player.headXRotation);

            SendUDPDataToAll(_packet);
        }
    }
    // Send client position to all clients but the client himself
    public static void PlayerPosition(int _exceptClient, Player _player, bool _doTeleport)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerPosition))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.transform.position);
            _packet.Write(_doTeleport);

            _packet.Write(_player.moveSpeed * _player.runSpeedMultiplier);

            _packet.Write(_player.headXRotation);

            SendUDPDataToAll(_exceptClient, _packet);
        }
    }
    // Send respond to client's movement predictions
    public static void PlayerPositionRespond(Player _player, bool _doTeleport, int _latestMovementRequestId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerPositionRespond))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.transform.position);
            _packet.Write(_player.yVelocity);
            _packet.Write(_doTeleport);

            _packet.Write(_latestMovementRequestId);

            SendUDPData(_player.id, _packet);
        }
    }

    public static void PlayerRotation(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRotation))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.transform.rotation);

            SendUDPDataToAll(_player.id, _packet);
        }
    }

    public static void PlayerInputs(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerInputs))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.inputs.Length);
            foreach (bool _input in _player.inputs)
            {
                _packet.Write(_input);
            }

            SendUDPDataToAll(_player.id, _packet);
        }
    }

    public static void PlayerDisconnected(int _playerId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerDisconnected))
        {
            _packet.Write(_playerId);

            SendTCPDataToAll(_packet);
        }
    }

    public static void PlayerHealth(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerHealth))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.health);

            SendTCPDataToAll(_packet);
        }
    }

    public static void PlayerRespawned(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRespawned))
        {
            _packet.Write(_player.id);

            SendTCPDataToAll(_packet);
        }
    }

    public static void PlayerShot(Player _player, Vector3 _fireOrigin, Vector3 _viewDirection, int _weaponId, int _ammoInClip, int _extraAmmo)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerShot))
        {
            _packet.Write(_player.id);
            _packet.Write(_fireOrigin);
            _packet.Write(_viewDirection);
            _packet.Write(_weaponId);
            _packet.Write(_ammoInClip);
            _packet.Write(_extraAmmo);

            SendTCPDataToAll(_packet);
        }
    }

    public static void PlayerReloadDone(Player _player, int _weaponId, int _ammoInClip, int _extraAmmo)
    {
        using (Packet _packet = new Packet((int) ServerPackets.playerReloadDone))
        {
            if (_player != null)
            {
                _packet.Write(_player.id);
            }
            else
            {
                _packet.Write(-1);
            }
            _packet.Write(_weaponId);
            _packet.Write(_ammoInClip);
            _packet.Write(_extraAmmo);

            SendTCPDataToAll(_packet);
        }
    }

    public static void PlayerHitInfo(int _toClient, Vector3 _hitPoint, float _damageGiven)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerHitInfo))
        {
            _packet.Write(_hitPoint);
            _packet.Write(_damageGiven);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void PlayerDeathsAndKills(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerDeathsAndKills))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.kills);
            _packet.Write(_player.deaths);

            SendTCPDataToAll(_packet);
        }
    }
    public static void PlayerDeathsAndKills(int _toClient, Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerDeathsAndKills))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.kills);
            _packet.Write(_player.deaths);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void PlayerKilled(string _killerPlayer, string _killedPlayer)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerKilled))
        {
            _packet.Write(_killerPlayer);
            _packet.Write(_killedPlayer);

            SendTCPDataToAll(_packet);
        }
    }

    public static void CreateItemSpawner(int _toClient, int _spawnerId, Vector3 _spawnerPosition, bool _hasItem)
    {
        using (Packet _packet = new Packet((int)ServerPackets.createItemSpawner))
        {
            _packet.Write(_spawnerId);
            _packet.Write(_spawnerPosition);
            _packet.Write(_hasItem);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void ItemSpawned(int _spawnerId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.itemSpawned))
        {
            _packet.Write(_spawnerId);

            SendTCPDataToAll(_packet);
        }
    }

    public static void ItemPickedUp(int _spawnerId, int _byPlayer)
    {
        using (Packet _packet = new Packet((int)ServerPackets.itemPickedUp))
        {
            _packet.Write(_spawnerId);
            _packet.Write(_byPlayer);

            SendTCPDataToAll(_packet);
        }
    }

    public static void SpawnProjectile(Projectile _projectile, int _thrownByPlayer)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnProjectile))
        {
            _packet.Write(_projectile.id);
            _packet.Write(_projectile.transform.position);
            _packet.Write(_thrownByPlayer);

            SendTCPDataToAll(_packet);
        }
    }

    public static void ProjectilePosition(Projectile _projectile)
    {
        using (Packet _packet = new Packet((int)ServerPackets.projectilePosition))
        {
            _packet.Write(_projectile.id);
            _packet.Write(_projectile.transform.position);

            SendUDPDataToAll(_packet);
        }
    }

    public static void ProjectileExploded(Projectile _projectile)
    {
        using (Packet _packet = new Packet((int)ServerPackets.projectileExploded))
        {
            _packet.Write(_projectile.id);
            _packet.Write(_projectile.transform.position);

            SendTCPDataToAll(_packet);
        }
    }

    public static void SpawnEnemy(Enemy _enemy)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnEnemy))
        {
            SendTCPDataToAll(SpawnEnemy_Data(_enemy, _packet));
        }
    }
    public static void SpawnEnemy(int _toClient, Enemy _enemy)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnEnemy))
        {
            SendTCPData(_toClient, SpawnEnemy_Data(_enemy, _packet));
        }
    }

    private static Packet SpawnEnemy_Data(Enemy _enemy, Packet _packet)
    {
        _packet.Write(_enemy.id);
        _packet.Write(_enemy.transform.position);
        return _packet;
    }

    public static void EnemyPosition(Enemy _enemy, bool _doTeleport)
    {
        using (Packet _packet = new Packet((int)ServerPackets.enemyPosition))
        {
            _packet.Write(_enemy.id);
            _packet.Write(_enemy.transform.position);
            _packet.Write(_doTeleport);

            SendUDPDataToAll(_packet);
        }
    }

    public static void EnemyRotation(Enemy _enemy)
    {
        using (Packet _packet = new Packet((int)ServerPackets.enemyRotation))
        {
            _packet.Write(_enemy.id);
            _packet.Write(_enemy.transform.rotation);

            SendUDPDataToAll(_packet);
        }
    }

    public static void EnemyHealth(Enemy _enemy)
    {
        using (Packet _packet = new Packet((int)ServerPackets.enemyHealth))
        {
            _packet.Write(_enemy.id);
            _packet.Write(_enemy.health);

            SendTCPDataToAll(_packet);
        }
    }

    public static void EnemyShot(Enemy _enemy, Vector3 _viewDirection)
    {
        using (Packet _packet = new Packet((int)ServerPackets.enemyShot))
        {
            _packet.Write(_enemy.id);
            _packet.Write(_viewDirection);

            SendTCPDataToAll(_packet);
        }
    }


    public static void SpawnWeapon(Weapon _weapon)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnWeapon))
        {
            SendTCPDataToAll(SpawnWeapon_Data(_weapon, _packet));
        }
    }
    public static void SpawnWeapon(int _toClient, Weapon _weapon)
    {
        using (Packet _packet = new Packet((int) ServerPackets.spawnWeapon))
        {
            SendTCPData(_toClient, SpawnWeapon_Data(_weapon, _packet));
        }
    }

    private static Packet SpawnWeapon_Data(Weapon _weapon, Packet _packet)
    {
        _packet.Write(_weapon.id);
        _packet.Write(_weapon.whichWeapon);
        _packet.Write(_weapon.transform.position);
        _packet.Write(_weapon.currentClipAmmo);
        _packet.Write(_weapon.currentExtraAmmo);
        _packet.Write(_weapon.maxClipAmmo);
        _packet.Write(_weapon.maxExtraAmmo);
        _packet.Write(_weapon.reloadTime);
        _packet.Write(_weapon.autoFireRate);
        _packet.Write(_weapon.burstFireRate);
        _packet.Write(_weapon.semiFireRate);
        _packet.Write(_weapon.fireSpread);
        _packet.Write(_weapon.shootDistance);
        return _packet;
    }

    public static void WeaponPositionAndRotation(int _weaponId, Vector3 _position, Vector3 _rotation)
    {
        using (Packet _packet = new Packet((int)ServerPackets.weaponPositionAndRotation))
        {
            _packet.Write(_weaponId);
            _packet.Write(_position);
            _packet.Write(_rotation);

            SendUDPDataToAll(_packet);
        }
    }

    public static void PlayerPickedWeapon(int _whichPlayer, Weapon _weapon)
    {
        using (Packet _packet = new Packet((int) ServerPackets.playerPickedWeapon))
        {
            _packet.Write(_whichPlayer);
            _packet.Write(_weapon.id);
            _packet.Write(_weapon.weaponType);
            _packet.Write(_weapon.currentClipAmmo);
            _packet.Write(_weapon.currentExtraAmmo);

            SendTCPDataToAll(_packet);
        }
    }

    public static void PlayerDroppedWeapon(int _whichPlayer, Weapon _weapon)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerDroppedWeapon))
        {
            _packet.Write(_whichPlayer);
            _packet.Write(_weapon.id);
            _packet.Write(_weapon.weaponType);

            SendTCPDataToAll(_packet);
        }
    }

    public static void PlayerWeaponUsed(int _whichPlayer, int _weaponUsed)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerWeaponUsed))
        {
            _packet.Write(_whichPlayer);
            _packet.Write(_weaponUsed);

            SendTCPDataToAll(_whichPlayer, _packet);
        }
    }
    #endregion
}
