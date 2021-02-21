using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Player : MonoBehaviour
{
    public int id;
    public string username;

    public int kills;
    public int deaths;

    public WeaponsController weaponsController;

    //public CharacterController controller;
    //public Transform shootOrigin;
    //public float shootDistance = 100f;
    public float throwForce = 600f;
    public float health;
    public float maxHealth = 100f;
    public int itemAmount = 0;
    public int maxItemAmount = 3;

    public float headXRotation;

    public float moveSpeed = 2.5f;
    public float runSpeedMultiplier = 2.5f;
    public float jumpSpeed = 5f;
    public float gravity = -9.81f;
    public float yVelocity = 0;

    public float height = 2f;
    public float stepHeight = 0.5f;
    public float stepSearchOffset = 1f;
    public LayerMask groundedDisclude;
    public LayerMask discludePlayer;
    public CapsuleCollider bodyCollider;


    public bool[] inputs;

    private bool hasMovementRequestId = false;
    private int latestMovementRequestId = 0;

    private Vector3 oldSentPosition;
    private float oldHeadXRotation;

    private void Start()
    {
        gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime;
        moveSpeed *= Time.fixedDeltaTime;
        jumpSpeed *= Time.fixedDeltaTime;

        ServerSend.LocalPlayerMovementVars(id, this);
    }

    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
        health = maxHealth;

        inputs = new bool[6];
    }

    public void FixedUpdate()
    {
        if (health <= 0f)
        {
            return;
        }

        if (hasMovementRequestId)
        {
            hasMovementRequestId = false;
            Move(GetInputDirection(inputs[0], inputs[1], inputs[2], inputs[3]), latestMovementRequestId);
        }
        else
        {
            Move(GetInputDirection(inputs[0], inputs[1], inputs[2], inputs[3]));
        }
        ServerSend.PlayerRotation(this);
        ServerSend.PlayerPosition(id, this, false); // bool for teleport or lerped movement
        ServerSend.PlayerInputs(this);



        CollisionCheck();

        if (IsGrounded())
        {
            GetUpFromGround();
        }

        // If under map, take damage
        if (transform.position.y < -10f)
        {
            TakeDamage(maxHealth);
        }
    }

    private void Move(Vector2 _inputDirection, int _requestId)
    {
        Vector3 _moveDirection = transform.right * _inputDirection.x + transform.forward * _inputDirection.y;
        _moveDirection *= moveSpeed;
        if (inputs[5])
        {
            _moveDirection *= runSpeedMultiplier;
        }

        if (IsGrounded())
        {
            yVelocity = 0;
            if (inputs[4])
            {
                yVelocity = jumpSpeed;
            }
        }
        yVelocity += gravity;
        if (yVelocity > 0)
        {
            // If moving upwards, make sure the player doesn't touch roof. If player touch roof, set yVelocity to 0 to make sure that the player doesn't "slide" around in the roof.
            if (GetClearDistanceAbovePlayer() < 0.1f)
            {
                yVelocity = 0;
            }
        }

        _moveDirection.y = yVelocity;


        // If something i in from of the player with less distance than _moveDirection, don't move.
        //if (Physics.Raycast(transform.position, _moveDirection, out RaycastHit _hit, Vector3.Distance(Vector3.zero, _moveDirection), discludePlayer, QueryTriggerInteraction.Ignore))
        //{
        //    _moveDirection = Vector3.zero;
        //}
        if (CollisionInOffset(_moveDirection / bodyCollider.radius))
        {
            //_moveDirection = Vector3.zero;
        }

        Vector3 _newPosition = transform.position + _moveDirection;
        transform.position = _newPosition;


        CollisionCheck();

        if (IsGrounded())
        {
            GetUpFromGround();
        }
        if (IsGroundedInFront(new Vector3(_moveDirection.x, 0, _moveDirection.z)))
        {
            GetUpFromGroundWithOffset(new Vector3(_moveDirection.x, 0, _moveDirection.z));
        }

        ServerSend.PlayerPositionRespond(this, false, _requestId); // bool for teleport or lerped movement
    }

    private void Move(Vector2 _inputDirection)
    {
        Vector3 _moveDirection = transform.right * _inputDirection.x + transform.forward * _inputDirection.y;
        _moveDirection *= moveSpeed;
        if (inputs[5])
        {
            _moveDirection *= runSpeedMultiplier;
        }

        if (IsGrounded())
        {
            yVelocity = 0;
            if (inputs[4])
            {
                yVelocity = jumpSpeed;
            }
        }
        yVelocity += gravity;
        if (yVelocity > 0)
        {
            // If moving upwards, make sure the player doesn't touch roof. If player touch roof, set yVelocity to 0 to make sure that the player doesn't "slide" around in the roof.
            if (GetClearDistanceAbovePlayer() < 0.1f)
            {
                yVelocity = 0;
            }
        }

        _moveDirection.y = yVelocity;


        // If something i in from of the player with less distance than _moveDirection, don't move.
        if (Physics.Raycast(transform.position, _moveDirection, out RaycastHit _hit, Vector3.Distance(Vector3.zero, _moveDirection), discludePlayer, QueryTriggerInteraction.Ignore))
        {
            _moveDirection = Vector3.zero;
        }


        Vector3 _newPosition = transform.position + _moveDirection;
        transform.position = _newPosition;


        CollisionCheck();

        if (IsGrounded())
        {
            GetUpFromGround();
        }
        if (IsGroundedInFront(new Vector3(_moveDirection.x, 0, _moveDirection.z)))
        {
            GetUpFromGroundWithOffset(new Vector3(_moveDirection.x, 0, _moveDirection.z));
        }

        if (Vector3.Distance(oldSentPosition, transform.position) > 0.005f || oldHeadXRotation - headXRotation > 0.1f || headXRotation - oldHeadXRotation > 0.1f)
        {
            oldSentPosition = transform.position;
            oldHeadXRotation = headXRotation;

            ServerSend.PlayerPosition(id, this, false); // bool for teleport or lerped movement
            //Debug.Log("Send player pos to clients");
        }
    }

    private void CollisionCheck()
    {
        // Check with body collider
        Collider[] overlaps = new Collider[10];
        int num = Physics.OverlapSphereNonAlloc(transform.TransformPoint(bodyCollider.center), bodyCollider.radius, overlaps, discludePlayer, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < num; i++)
        {
            if (overlaps[i].gameObject == gameObject)
            {
                return;
            }
            Transform t = overlaps[i].transform;
            Vector3 dir;
            float dist;

            if (Physics.ComputePenetration(bodyCollider, transform.position, transform.rotation, overlaps[i], t.position, t.rotation, out dir, out dist))
            {
                Vector3 penetrationVector = dir * dist;
                transform.position = transform.position + penetrationVector;
            }
        }
    }
    private bool CollisionInOffset(Vector3 _offset)
    {
        // Check with body collider
        Collider[] overlaps = new Collider[10];
        int num = Physics.OverlapSphereNonAlloc(transform.TransformPoint(bodyCollider.center) + _offset, bodyCollider.radius, overlaps, discludePlayer, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < num; i++)
        {
            if (overlaps[i].gameObject != gameObject)
            {
                return true;
            }

            //Transform t = overlaps[i].transform;
            //Vector3 dir;
            //float dist;

            //if (Physics.ComputePenetration(bodyCollider, transform.position, transform.rotation, overlaps[i], t.position, t.rotation, out dir, out dist))
            //{
            //    Vector3 penetrationVector = dir * dist;
            //    transform.position = transform.position + penetrationVector;
            //}
        }

        return false;
    }

    private void GetUpFromGround()
    {
        float _distanceAbovePlayer = GetClearDistanceAbovePlayer();

        // Store all correct raycast hits and store the smallestHitDistance
        float _smallestHitDistance = stepHeight + 0.05f;
        Ray _downRay = new Ray(new Vector3(transform.position.x, transform.position.y - (height / 2 - stepHeight), transform.position.z), -Vector3.up);
        RaycastHit[] _hits = Physics.RaycastAll(_downRay, stepHeight + 0.5f, discludePlayer, QueryTriggerInteraction.Ignore);
        if (_hits != null)
        {
            foreach (RaycastHit _hit in _hits)
            {
                if (_hit.transform.gameObject != gameObject)
                {
                    if (_hit.distance < _smallestHitDistance)
                    {
                        _smallestHitDistance = _hit.distance;
                    }
                }
            }
        }
        // Check if the smallestHitDistance is < stepHeight + 0.05f. If so, calculate the distance to move up
        if (_smallestHitDistance < stepHeight + 0.05f)
        {
            float _distanceToMoveUp = stepHeight + 0.05f - _smallestHitDistance;
            // If distanceToMoveUp > the clear distance above the player, set distanceToMoveUp to distanceAbovePlayer.
            if (_distanceToMoveUp > _distanceAbovePlayer)
            {
                _distanceToMoveUp = _distanceAbovePlayer;
            }

            Vector3 newYPos = new Vector3(transform.position.x, transform.position.y + _distanceToMoveUp, transform.position.z);
            transform.position = newYPos;
        }
        Debug.DrawRay(new Vector3(transform.position.x, transform.position.y - (height / 2 - stepHeight), transform.position.z), -Vector3.up * _smallestHitDistance, Color.yellow);
    }
    private void GetUpFromGroundWithOffset(Vector3 _direction)
    {
        float _distanceAbovePlayer = GetClearDistanceAbovePlayer();

        // Store all correct raycast hits and store the smallestHitDistance
        float _smallestHitDistance = stepHeight + 0.05f;
        Vector3 _raycastPosWithOffset = new Vector3(transform.position.x, transform.position.y - (height / 2 - stepHeight), transform.position.z);
        _raycastPosWithOffset -= _direction * stepSearchOffset;
        Ray _downRayInFront = new Ray(_raycastPosWithOffset, -Vector3.up);
        RaycastHit[] _hits = Physics.RaycastAll(_downRayInFront, stepHeight + 0.5f, discludePlayer, QueryTriggerInteraction.Ignore);
        if (_hits != null)
        {
            foreach (RaycastHit _hit in _hits)
            {
                if (_hit.transform.gameObject != gameObject)
                {
                    if (_hit.distance < _smallestHitDistance)
                    {
                        _smallestHitDistance = _hit.distance;
                    }
                }
            }
        }
        // Check if the smallestHitDistance is < stepHeight + 0.05f. If so, calculate the distance to move up
        if (_smallestHitDistance < stepHeight + 0.05f)
        {
            float _distanceToMoveUp = stepHeight + 0.05f - _smallestHitDistance;
            // If distanceToMoveUp > the clear distance above the player, set distanceToMoveUp to distanceAbovePlayer.
            if (_distanceToMoveUp > _distanceAbovePlayer)
            {
                _distanceToMoveUp = _distanceAbovePlayer;
            }

            Vector3 newYPos = new Vector3(transform.position.x, transform.position.y + _distanceToMoveUp, transform.position.z);
            transform.position = newYPos;
        }
    }

    private float GetClearDistanceAbovePlayer()
    {
        float _newDistanceToCeiling = height * 5;

        Ray _upRay = new Ray(transform.position, Vector3.up);
        RaycastHit[] _hits = Physics.RaycastAll(_upRay, height * 5, discludePlayer, QueryTriggerInteraction.Ignore);
        if (_hits != null)
        {
            float _smallestHitDistance = _newDistanceToCeiling;
            foreach (RaycastHit _hit in _hits)
            {
                if (_hit.transform.gameObject != gameObject)
                {
                    if (_hit.distance < _smallestHitDistance)
                    {
                        _smallestHitDistance = _hit.distance;
                    }
                }
            }
            _newDistanceToCeiling = _smallestHitDistance;
        }

        _newDistanceToCeiling -= height / 2;
        Debug.DrawRay(transform.position, Vector3.up * _newDistanceToCeiling, Color.green);

        return _newDistanceToCeiling;
    }

    private bool IsGrounded()
    {
        bool _isGrounded = false;
        RaycastHit[] _hits;
        _hits = Physics.RaycastAll(new Vector3(transform.position.x, transform.position.y - (height / 2 - stepHeight), transform.position.z), -Vector3.up, stepHeight + 0.1f
                                        , groundedDisclude, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < _hits.Length; i++)
        {
            RaycastHit _hit = _hits[i];
            if (_hit.transform.gameObject != gameObject)
            {
                _isGrounded = true;
            }
        }

        return _isGrounded;
    }

    private bool IsGroundedInFront(Vector3 _direction)
    {
        bool _isGrounded = false;
        Vector3 _raycastPos = new Vector3(transform.position.x, transform.position.y - (height / 2 - stepHeight));
        _raycastPos += _direction * stepSearchOffset;
        RaycastHit[] _hits;
        _hits = Physics.RaycastAll(_raycastPos, -Vector3.up, stepHeight + 0.1f, groundedDisclude, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < _hits.Length; i++)
        {
            RaycastHit _hit = _hits[i];
            if (_hit.transform.gameObject != gameObject)
            {
                _isGrounded = true;
            }
        }

        return _isGrounded;
    }


    public void SetInput(int _requestId, bool[] _inputs, Quaternion _rotation, float _headXRotation)
    {
        inputs = _inputs;
        transform.rotation = _rotation;
        headXRotation = _headXRotation;

        latestMovementRequestId = _requestId;
        hasMovementRequestId = true;
    }
    private Vector2 GetInputDirection(bool _input0, bool _input1, bool _input2, bool _input3)
    {
        Vector2 _inputDirection = Vector2.zero;
        if (_input0)
        {
            _inputDirection.y += 1;
        }
        if (_input1)
        {
            _inputDirection.y -= 1;
        }
        if (_input2)
        {
            _inputDirection.x -= 1;
        }
        if (_input3)
        {
            _inputDirection.x += 1;
        }

        return _inputDirection;
    }

    public void Shoot(Vector3 _playerPosition, Vector3 _viewDirection, int _fireModeInt)
    {
        if (health <= 0)
        {
            return;
        }

        if (weaponsController.weaponsEquiped[weaponsController.weaponUsed] != null)
        {
            if (weaponsController.weaponUsed == 2)
            {
                StartCoroutine(weaponsController.weaponsEquiped[weaponsController.weaponUsed].GetComponent<MeleeController>().NormalHit());
            }
            else
            {
                weaponsController.weaponsEquiped[weaponsController.weaponUsed].GetComponent<Weapon>().Fire(_playerPosition, _viewDirection, _fireModeInt);
            }
        }
    }

    public void ThrowItem(Vector3 _viewDirection)
    {
        //if (health <= 0)
        //{
        //    return;
        //}

        //if (itemAmount > 0)
        //{
        //    itemAmount--;
        //    NetworkManager.instance.InstantiateProjectile(shootOrigin).Initialize(_viewDirection, throwForce, id);
        //}
    }

    public void TakeDamage(float _damage)
    {
        if (health <= 0)
        {
            return;
        }

        health -= _damage;
        if (health <= 0)
        {
            health = 0;


            // search all weapons in weaponsholder, then deactivate weapons
            Weapon[] _weapons = weaponsController.weaponsHolder.transform.GetComponentsInChildren<Weapon>(true);
            foreach (Weapon _weapon in _weapons)
            {
                _weapon.enabled = false;
                _weapon.gameObject.SetActive(false);
            }


            MapProperties _currentMapProperties = GameObject.FindWithTag("Map").GetComponent<MapProperties>();
            transform.position = _currentMapProperties.spawnPositions[Random.Range(0, _currentMapProperties.spawnPositions.Length)].position;

            ServerSend.PlayerPosition(this, true);

            deaths++;
            ServerSend.PlayerDeathsAndKills(this);

            StartCoroutine(Respawn());
        }

        ServerSend.PlayerHealth(this);
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(2);

        health = maxHealth;
        ServerSend.PlayerRespawned(this);

        // search all weapons in weaponsholder, then activate weapons
        Weapon[] _weapons = weaponsController.weaponsHolder.transform.GetComponentsInChildren<Weapon>(true);
        foreach (Weapon _weapon in _weapons)
        {
            _weapon.canFire = true;
            _weapon.reloading = false;
            if (weaponsController.weaponsEquiped[weaponsController.weaponUsed] != null)
            {
                if (_weapon == weaponsController.weaponsEquiped[weaponsController.weaponUsed].GetComponent<Weapon>())
                {
                    _weapon.enabled = true;
                    _weapon.gameObject.SetActive(true);
                }
                else
                {
                    _weapon.enabled = false;
                    _weapon.gameObject.SetActive(false);
                }
            }
            else
            {
                _weapon.enabled = false;
                _weapon.gameObject.SetActive(false);
            }
        }
    }

    public bool AttemptPickupItem()
    {
        if (itemAmount >= maxItemAmount)
        {
            return false;
        }

        itemAmount++;
        return true;
    }
}