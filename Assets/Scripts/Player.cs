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

    //public CharacterController controller;
    public Transform shootOrigin;
    public float shootDistance = 100f;
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

        Vector2 _inputDirection = Vector2.zero;
        if (inputs[0])
        {
            _inputDirection.y += 1;
        }
        if (inputs[1])
        {
            _inputDirection.y -= 1;
        }
        if (inputs[2])
        {
            _inputDirection.x -= 1;
        }
        if (inputs[3])
        {
            _inputDirection.x += 1;
        }

        Move(_inputDirection);
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
        if (Physics.Raycast(transform.position, _moveDirection, out RaycastHit _hit, Vector3.Distance(Vector3.zero, _moveDirection), discludePlayer, QueryTriggerInteraction.Ignore))
        {
            //float _maxDistanceAbleToMove = Vector3.Distance(Vector3.zero, _moveDirection) / _hit.distance;
            //Debug.Log(_maxDistanceAbleToMove);

            _moveDirection = Vector3.zero;
            //_moveDirection = new Vector3(_moveDirection.x / _maxDistanceAbleToMove,
            //                                _moveDirection.y / _maxDistanceAbleToMove,
            //                                    _moveDirection.z / _maxDistanceAbleToMove);
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

        //movementRequestIds.Remove(_requestId);
        //if (movementRequestIds.Count > 0)
        //{
        //}
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

        ServerSend.PlayerPosition(id, this, false); // bool for teleport or lerped movement
    }

    private void CollisionCheck()
    {
        // Check with body collider
        //Collider[] overlaps = new Collider[4];
        //int num = Physics.OverlapSphereNonAlloc(transform.TransformPoint(bodyCollider.center), bodyCollider.radius, overlaps, discludePlayer, QueryTriggerInteraction.UseGlobal);
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


        //// Check with head collider
        //overlaps = new Collider[4];
        //num = Physics.OverlapSphereNonAlloc(transform.TransformPoint(headCollider.center), headCollider.radius, overlaps, discludePlayer, QueryTriggerInteraction.UseGlobal);

        //for (int i = 0; i < num; i++)
        //{
        //    Transform t = overlaps[i].transform;
        //    Vector3 dir;
        //    float dist;

        //    if (Physics.ComputePenetration(headCollider, transform.position, transform.rotation, overlaps[i], t.position, t.rotation, out dir, out dist))
        //    {
        //        Vector3 penetrationVector = dir * dist;
        //        transform.position = transform.position + penetrationVector;
        //    }
        //}
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
        //Debug.DrawRay(_raycastPosWithOffset, -Vector3.up * _smallestHitDistance, Color.blue);
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
            //Debug.Log("Distance to roof hit: " + _smallestHitDistance);
        }

        _newDistanceToCeiling -= height / 2;
        //Debug.Log("New Distance To Ceiling: " + _newDistanceToCeiling);
        Debug.DrawRay(transform.position, Vector3.up * _newDistanceToCeiling, Color.green);

        return _newDistanceToCeiling;
    }

    private bool IsGrounded()
    {
        //Debug.DrawRay(transform.position, -Vector3.up * (distToGround + 0.1f), Color.blue);
        //bool _isGrounded = Physics.Raycast(transform.position, -Vector3.up, distToGround + 0.1f);

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
        //movementRequestIds.Add(_requestId);
        inputs = _inputs;
        //Debug.Log(inputs);
        transform.rotation = _rotation;
        headXRotation = _headXRotation;


        Vector2 _inputDirection = Vector2.zero;
        if (inputs[0])
        {
            _inputDirection.y += 1;
        }
        if (inputs[1])
        {
            _inputDirection.y -= 1;
        }
        if (inputs[2])
        {
            _inputDirection.x -= 1;
        }
        if (inputs[3])
        {
            _inputDirection.x += 1;
        }

        Move(_inputDirection, _requestId);
    }

    public void Shoot(Vector3 _viewDirection)
    {
        if (health <= 0)
        {
            return;
        }

        ServerSend.PlayerShot(this, _viewDirection);

        if (Physics.Raycast(shootOrigin.position, _viewDirection, out RaycastHit _hit, shootDistance))
        {
            if (_hit.collider.CompareTag("Player"))
            {
                // If hit own player
                if (_hit.collider.gameObject.GetComponent<Player>() == this)
                {
                    return;
                }
                // If the player hit is dead
                if (_hit.collider.GetComponent<Player>().health <= 0)
                {
                    return;
                }

                _hit.collider.GetComponent<Player>().TakeDamage(50f);
                ServerSend.PlayerHitInfo(id, _hit.point, 50f);

                if (_hit.collider.GetComponent<Player>().health <= 0)
                {
                    kills++;
                    ServerSend.PlayerKilled(username, _hit.collider.GetComponent<Player>().username);
                    ServerSend.PlayerDeathsAndKills(this);
                }
            }
            else if (_hit.collider.CompareTag("Enemy"))
            {
                if (_hit.collider.GetComponent<Enemy>().health <= 0)
                {
                    return;
                }

                _hit.collider.GetComponent<Enemy>().TakeDamage(50f);
                ServerSend.PlayerHitInfo(id, _hit.point, 50f);

                if (_hit.collider.GetComponent<Enemy>().health <= 0)
                {
                    kills++;
                    ServerSend.PlayerKilled(username, "Bot");
                    ServerSend.PlayerDeathsAndKills(this);
                }
            }
        }
    }

    public void ThrowItem(Vector3 _viewDirection)
    {
        if (health <= 0)
        {
            return;
        }

        if (itemAmount > 0)
        {
            itemAmount--;
            NetworkManager.instance.InstantiateProjectile(shootOrigin).Initialize(_viewDirection, throwForce, id);
        }
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
            //controller.enabled = false;

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
        //controller.enabled = true;
        ServerSend.PlayerRespawned(this);
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




//    public int id;
//    public string username;
//    public CharacterController controller;
//    public Transform shootOrigin;
//    public float shootDistance = 100f;
//    public float gravity = -9.81f;
//    public float moveSpeed = 5f;
//    public float jumpSpeed = 5f;
//    public float throwForce = 600f;
//    public float health;
//    public float maxHealth = 100f;
//    public int itemAmount = 0;
//    public int maxItemAmount = 3;

//    public float yVelocity = 0;

//    private bool[] inputs;

//    //public List<int> movementRequestIds = new List<int>();

//    private void Start()
//    {
//        gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime;
//        moveSpeed *= Time.fixedDeltaTime;
//        jumpSpeed *= Time.fixedDeltaTime;

//        ServerSend.LocalPlayerMovementVars(id, this);
//    }

//    public void Initialize(int _id, string _username)
//    {
//        id = _id;
//        username = _username;
//        health = maxHealth;

//        inputs = new bool[5];
//    }

//    public void FixedUpdate()
//    {
//        if (health <= 0f)
//        {
//            return;
//        }

//        Vector2 _inputDirection = Vector2.zero;
//        if (inputs[0])
//        {
//            _inputDirection.y += 1;
//        }
//        if (inputs[1])
//        {
//            _inputDirection.y -= 1;
//        }
//        if (inputs[2])
//        {
//            _inputDirection.x -= 1;
//        }
//        if (inputs[3])
//        {
//            _inputDirection.x += 1;
//        }

//        Move(_inputDirection);
//        ServerSend.PlayerRotation(this);
//        ServerSend.PlayerPosition(id, this, false); // bool for teleport or lerped movement
//    }

//    private void Move(Vector2 _inputDirection, int _requestId)
//    {
//        Vector3 _moveDirection = transform.right * _inputDirection.x + transform.forward * _inputDirection.y;
//        _moveDirection *= moveSpeed;

//        if (controller.isGrounded)
//        {
//            yVelocity = 0;
//            if (inputs[4])
//            {
//                yVelocity = jumpSpeed;
//            }
//        }
//        yVelocity += gravity;

//        _moveDirection.y = yVelocity;
//        controller.Move(_moveDirection);

//        ServerSend.PlayerPositionRespond(this, false, _requestId); // bool for teleport or lerped movement

//        //movementRequestIds.Remove(_requestId);
//        //if (movementRequestIds.Count > 0)
//        //{
//        //}
//    }

//    private void Move(Vector2 _inputDirection)
//    {
//        Vector3 _moveDirection = transform.right * _inputDirection.x + transform.forward * _inputDirection.y;
//        _moveDirection *= moveSpeed;

//        if (controller.isGrounded)
//        {
//            yVelocity = 0;
//            if (inputs[4])
//            {
//                yVelocity = jumpSpeed;
//            }
//        }
//        yVelocity += gravity;

//        _moveDirection.y = yVelocity;
//        controller.Move(_moveDirection);

//        ServerSend.PlayerPosition(id, this, false); // bool for teleport or lerped movement

//        //movementRequestIds.Remove(_requestId);
//        //if (movementRequestIds.Count > 0)
//        //{
//        //}
//    }

//    public void SetInput(int _requestId, bool[] _inputs, Quaternion _rotation)
//    {
//        //movementRequestIds.Add(_requestId);
//        inputs = _inputs;
//        //Debug.Log(inputs);
//        transform.rotation = _rotation;


//        Vector2 _inputDirection = Vector2.zero;
//        if (inputs[0])
//        {
//            _inputDirection.y += 1;
//        }
//        if (inputs[1])
//        {
//            _inputDirection.y -= 1;
//        }
//        if (inputs[2])
//        {
//            _inputDirection.x -= 1;
//        }
//        if (inputs[3])
//        {
//            _inputDirection.x += 1;
//        }

//        Move(_inputDirection, _requestId);
//    }

//    public void Shoot(Vector3 _viewDirection)
//    {
//        if (health <= 0)
//        {
//            return;
//        }

//        ServerSend.PlayerShot(this, _viewDirection);

//        if (Physics.Raycast(shootOrigin.position, _viewDirection, out RaycastHit _hit, shootDistance))
//        {
//            if (_hit.collider.CompareTag("Player"))
//            {
//                _hit.collider.GetComponent<Player>().TakeDamage(50f);
//                ServerSend.PlayerHitInfo(id, _hit.point, 50f);
//            }
//            else if (_hit.collider.CompareTag("Enemy"))
//            {
//                _hit.collider.GetComponent<Enemy>().TakeDamage(50f);
//                ServerSend.PlayerHitInfo(id, _hit.point, 50f);
//            }
//        }
//    }

//    public void ThrowItem(Vector3 _viewDirection)
//    {
//        if (health <= 0)
//        {
//            return;
//        }

//        if (itemAmount > 0)
//        {
//            itemAmount--;
//            NetworkManager.instance.InstantiateProjectile(shootOrigin).Initialize(_viewDirection, throwForce, id);
//        }
//    }

//    public void TakeDamage(float _damage)
//    {
//        if (health <= 0)
//        {
//            return;
//        }

//        health -= _damage;
//        if (health <= 0)
//        {
//            health = 0;
//            controller.enabled = false;

//            MapProperties _currentMapProperties = GameObject.FindWithTag("Map").GetComponent<MapProperties>();
//            transform.position = _currentMapProperties.spawnPositions[Random.Range(0, _currentMapProperties.spawnPositions.Length)].position;

//            ServerSend.PlayerPosition(this, true);
//            StartCoroutine(Respawn());
//        }

//        ServerSend.PlayerHealth(this);
//    }

//    private IEnumerator Respawn()
//    {
//        yield return new WaitForSeconds(2);

//        health = maxHealth;
//        controller.enabled = true;
//        ServerSend.PlayerRespawned(this);
//    }

//    public bool AttemptPickupItem()
//    {
//        if (itemAmount >= maxItemAmount)
//        {
//            return false;
//        }

//        itemAmount++;
//        return true;
//    }
//}
