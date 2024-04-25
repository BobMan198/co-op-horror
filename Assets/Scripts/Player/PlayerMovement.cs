using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Dissonance.Integrations.Unity_NFGO;
using UnityEngine.UI;
using System.Collections;
using System.Security.Cryptography.X509Certificates;
using Dissonance;

public class PlayerMovement : NetworkBehaviour
{
    public enum PlayerState
    {
        idle,
        walk,
        reverseWalk,
        sprint,
        jump
    }

    [Header("Set In Editor")]
    public LayerMask layersToIgnore;
    public CharacterController controller;
    public Vector3 currentVelocity; // This result is the finalized value of velocityToApply, used for GhostVelocity value
    public Transform mainCamera;
    public Camera playerCamera;
    public Camera spectatorCamera;
    public AudioListener audioListener;
    public GameObject playerItemHolder;
    public GameObject nfgoPlayer;

    public CanvasGroup fadeBlack;
    private bool fadeBlackBool;

    [SerializeField] private Image staminaProgressUI = null;
    [SerializeField] private CanvasGroup sliderCanvasGroup = null;
    public GameObject playerUI;
    public GameObject playerStaminaUI;

    //public NetworkVariable<float> playerHealth = new NetworkVariable<float>();
    public float playerHealth = 100;
    public float maxPlayerHealth = 100;

    private GameObject lobbyCamera;

    private GameObject lobbyUICanvas;

    [Header("Debugging properties")]
    [Tooltip("Red line is current velocity, blue is the new direction")]
    public bool showDebugGizmos = false;
    //The velocity applied at the end of every physics frame
    public Vector3 velocityToApply;
    public Vector3 currentInput;

    [SerializeField]
    private bool grounded;

    [SerializeField]
    private bool crouching;

    public bool isSprinting;

    [SerializeField]
    private bool canAutoBhop;

    public bool playerIsWalking = false;


    [Header("Player Stats")]
    public float maxStamina = 100f;
    public float staminaDecreaseRate = 20f;
    public float staminaIncreaseRate = 15f;
    public float currentStamina;
    private bool jumping;
    [SerializeField]
    private bool infiniteStamina;
    private bool noClip;

    [SerializeField]
    private NetworkVariable<PlayerState> networkPlayerState = new NetworkVariable<PlayerState>();

    public Animator playerAnimator;
    public Animator clientPlayerAnimator;
    public SkinnedMeshRenderer playerModel;
    public SkinnedMeshRenderer clientPlayerModel;

    private void Awake()
    {
        velocityToApply = Vector3.zero;
        currentVelocity = velocityToApply;
    }

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        controller = GetComponent<CharacterController>();
        currentStamina = maxStamina;
        Cursor.lockState = CursorLockMode.Locked;
        lobbyCamera = GameObject.FindGameObjectWithTag("MainCamera");
        lobbyCamera.SetActive(false);
    }

    // All input checking going in Update, so no Input queries are missed
    private void Update()
    {
        HandleMouseLook();

        CheckNoClip();
        if (noClip)
        {
            NoClipMove();
            return;
        }

        SetGrounded();
        CheckCrouch();
        CheckSprint();
        ApplyGravity();
        CheckJump();

        CheckStamina();
        CheckHealth();
        currentInput = GetWorldSpaceInputVector();
        currentVelocity = velocityToApply;
        controller.Move(velocityToApply * Time.deltaTime);
        HandleLobbyUI();
        ClientVisuals();
        //HandleFlashlightServerRpc();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        GameRunner.PlayerMovementList.Add(this);

        playerAnimator.enabled = true;

        var playerModelColliders = playerAnimator.GetComponentsInChildren<Collider>();
        foreach (var collider in playerModelColliders)
        {
            collider.enabled = false;
        }

        //if (!IsLocalPlayer)
        //{
        //    audioListener.enabled = false;
        //    playerItemHolder.SetActive(false);
        //    controller.enabled = false;
        //}

        if (!IsOwner)
        {
            controller.enabled = false;
            audioListener.enabled = false;
            playerUI.SetActive(false);
            playerItemHolder.SetActive(false);
            this.enabled = false;
            clientPlayerModel.enabled = false;
        }
        else
        {
            controller.enabled = false;
            this.transform.position = new Vector3(0, 0, 2);
            //playerUI.enabled = true;
            controller.enabled = true;
            playerModel.enabled = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        GameRunner.PlayerMovementList.Remove(this);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // If we hit a wall, make us lose speed so we can't continuously jump into a wall and gain speed
        if (controller.collisionFlags == CollisionFlags.Sides && hit.normal.y < 0.7f)
        {
            velocityToApply = ClipVelocity(hit.normal);
        }
    }

    private void FixedUpdate()
    {
        Vector3 wishDir = currentInput.normalized;
        float wishSpeed = currentInput.magnitude;

        if(isSprinting)
        {
            wishSpeed *= 1.5f;
            UpdatePlayerStateServerRpc(PlayerState.sprint);
        }

        if (grounded)
        {
            if (IsPlayerWalkingBackwards())
            {
                wishSpeed *= PlayerConstants.BackWardsMoveSpeedScale;
            }
            ApplyFriction();
            ApplyGroundAcceleration(wishDir, wishSpeed, PlayerConstants.NormalSurfaceFriction);
        }
        else
        {
            ApplyAirAcceleration(wishDir, wishSpeed);
        }

        ClampVelocity(PlayerConstants.MaxVelocity);
    }

    private void SetGrounded()
    {

        // If we weren't grounded last frame, but we are now...
        // Reset the gravity to the base that keeps pushing the player into the ground
        // Otherwise, the controller's grounding doesn't check properly
        if (!grounded && controller.isGrounded)
        {
            velocityToApply.y = -PlayerConstants.Gravity * Time.deltaTime;
        }

        grounded = controller.isGrounded;

        // If we are falling into a portal, make sure we don't clip with the ground
        if ((velocityToApply.y < -1 || velocityToApply.y > 1) )
        {
            grounded = false;
        }

        // When flying up, reset y velocity if you hit the ceiling
        if (controller.collisionFlags == CollisionFlags.CollidedAbove && velocityToApply.y > 0)
        {
            velocityToApply.y = 0;
        }
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        transform.Rotate(Vector3.up * mouseX);

        Vector3 rotation = mainCamera.transform.rotation.eulerAngles;
        rotation.x -= mouseY;
        mainCamera.transform.rotation = Quaternion.Euler(rotation);
    }

    #region Crouch
    private void CheckCrouch()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            crouching = true;
        }
        else
        {
            // If we are already crouching, check if we need to stay crouching (something is above the player)
            if (crouching)
            {
                crouching = !CanUncrouch();
            }
            else
            {
                crouching = false;
            }
        }

        // Resize the player bounding box
        ResizeCollider();

        // Move the camera to the correct offset
    }

    private bool CanUncrouch()
    {
        // Get the vertical distance covered when uncrouching
        float castDistance = (PlayerConstants.StandingPlayerHeight - PlayerConstants.CrouchingPlayerHeight) + 0.01f;
        RaycastHit hit;
        Ray ray = new Ray(transform.position + new Vector3(0, PlayerConstants.CrouchingPlayerHeight / 2, 0), Vector3.up);
        if (Physics.Raycast(ray, out hit, castDistance, layersToIgnore))
        {
            return false;
        }
        return true;
    }

    private void ResizeCollider()
    {
        // Change the size of the collider
        float endHeight = crouching ? PlayerConstants.CrouchingPlayerHeight : PlayerConstants.StandingPlayerHeight;
        float velocity = 0;
        float startingHeight = controller.height;
        float height = Mathf.SmoothDamp(controller.height, endHeight, ref velocity, Time.deltaTime);

        if (height > startingHeight && grounded)
        {
            Vector3 newPosition = transform.position;
            newPosition.y += height - startingHeight;
            transform.position = newPosition;
        }

        controller.height = height;
    }

    private void CheckSprint()
    {
        isSprinting = Input.GetKey(KeyCode.LeftShift) && !crouching && currentStamina > 0;
    }

    void CheckStamina()
    {
        if(infiniteStamina){
            currentStamina = maxStamina;
            staminaProgressUI.fillAmount = currentStamina / maxStamina;
            return;
        }

        if(isSprinting)
        {
            currentStamina -= staminaDecreaseRate * Time.deltaTime;
        }
        if(jumping){
            currentStamina -= 15;
            jumping = false;
        }

        if(!isSprinting && !jumping && currentStamina < maxStamina){
            currentStamina += staminaIncreaseRate * Time.deltaTime;
        }
        
        staminaProgressUI.fillAmount = currentStamina / maxStamina;
    }
    #endregion

    private void ApplyGravity()
    {
        if (!grounded && velocityToApply.y > -PlayerConstants.MaxFallSpeed)
        {
            velocityToApply.y -= PlayerConstants.Gravity * Time.deltaTime;
        }
    }

    private void CheckJump()
    {
        bool requestJump = false;
        if(canAutoBhop){
            requestJump = Input.GetKey(KeyCode.Space);
        }else{
            requestJump = Input.GetKeyDown(KeyCode.Space);
        }

        if (grounded && requestJump && currentStamina >= 15)
        {
            RaycastHit hit;
            Vector3 startPos = transform.position - new Vector3(0, controller.height / 2, 0);
            if (Physics.Raycast(startPos, Vector3.down, out hit, 0.301f, layersToIgnore, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.7f)
            {
                Vector3 clippedVelocity = ClipVelocity(hit.normal);
                clippedVelocity.y = 0;

                Vector3 temp = velocityToApply;
                temp.y = 0;
                if (Vector3.Dot(clippedVelocity, temp) > 0 && clippedVelocity.magnitude >= temp.magnitude)
                {
                    velocityToApply = clippedVelocity;
                }
            }

            velocityToApply.y = 0;
            velocityToApply.y += crouching ? PlayerConstants.CrouchingJumpPower : PlayerConstants.JumpPower;
            grounded = false;

            jumping = true;
        }
    }

    #region Input
    private Vector3 GetWorldSpaceInputVector()
    {
        float moveSpeed = crouching ? PlayerConstants.CrouchingMoveSpeed : PlayerConstants.MoveSpeed;

        Vector3 inputVelocity = GetInputVelocity(moveSpeed);
        if (inputVelocity.magnitude > moveSpeed)
        {
            inputVelocity *= moveSpeed / inputVelocity.magnitude;
        }

        //Get the velocity vector in world space coordinates, by rotating around the camera's y-axis
        return Quaternion.AngleAxis(mainCamera.transform.rotation.eulerAngles.y, Vector3.up) * inputVelocity;
    }

    private Vector3 GetInputVelocity(float moveSpeed)
    {
        float horizontalSpeed = 0;
        float verticalSpeed = 0;

        if (Input.GetKey(KeyCode.A))
        {
            horizontalSpeed -= moveSpeed;
            playerIsWalking = true;
            UpdatePlayerStateServerRpc(PlayerState.walk);
        }

        if (Input.GetKey(KeyCode.D))
        {
            horizontalSpeed += moveSpeed;
            playerIsWalking = true;
            UpdatePlayerStateServerRpc(PlayerState.walk);
        }

        if (Input.GetKey(KeyCode.S))
        {
            verticalSpeed -= moveSpeed;
            playerIsWalking = true;
            UpdatePlayerStateServerRpc(PlayerState.reverseWalk);
        }

        if (Input.GetKey(KeyCode.W))
        {
            verticalSpeed += moveSpeed;
            playerIsWalking = true;
            UpdatePlayerStateServerRpc(PlayerState.walk);
        }

        if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.W))
        {
            playerIsWalking = false;
            UpdatePlayerStateServerRpc(PlayerState.idle);
        }
        else if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.Space) && !Input.GetKey(KeyCode.LeftShift))
        {
            UpdatePlayerStateServerRpc(PlayerState.idle);
        }

        if(Input.GetKey(KeyCode.Space))
        {
            UpdatePlayerStateServerRpc(PlayerState.jump);
        }
        else
        {
            playerAnimator.SetFloat("Jump", 0);
        }

        if(Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.W))
        {
            UpdatePlayerStateServerRpc(PlayerState.sprint);
        }

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.D))
        {
            UpdatePlayerStateServerRpc(PlayerState.sprint);
        }

        if(Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.A))
        {
            UpdatePlayerStateServerRpc(PlayerState.sprint);
        }

        return new Vector3(horizontalSpeed, 0, verticalSpeed);
    }

    private bool IsPlayerWalkingBackwards()
    {
        Vector3 inputDirection = GetInputVelocity(PlayerConstants.MoveSpeed);

        return inputDirection.z < 0;
    }
    #endregion

    //wishDir: the direction the player wishes to go in the newest frame
    //wishSpeed: the speed the player wishes to go this frame
    private void ApplyGroundAcceleration(Vector3 wishDir, float wishSpeed, float surfaceFriction)
    {
        var currentSpeed = Vector3.Dot(velocityToApply, wishDir); //Vector projection of the current velocity onto the new direction
        var speedToAdd = wishSpeed - currentSpeed;

        var acceleration = PlayerConstants.GroundAcceleration * Time.deltaTime; //acceleration to apply in the newest direction

        if (speedToAdd <= 0)
        {
            return;
        }

        var accelspeed = Mathf.Min(acceleration * wishSpeed * surfaceFriction, speedToAdd);
        velocityToApply += accelspeed * wishDir; //add acceleration in the new direction
    }

    //wishDir: the direction the player  wishes to goin the newest frame
    //wishSpeed: the speed the player wishes to go this frame
    private void ApplyAirAcceleration(Vector3 wishDir, float wishSpeed)
    {
        var wishSpd = Mathf.Min(wishSpeed, PlayerConstants.AirAccelerationCap);
        Vector3 xzVelocity = velocityToApply;
        xzVelocity.y = 0;
        var currentSpeed = Vector3.Dot(xzVelocity, wishDir);
        var speedToAdd = wishSpd - currentSpeed;

        if (speedToAdd <= 0)
        {
            return;
        }

        var accelspeed = Mathf.Min(speedToAdd, PlayerConstants.AirAcceleration * wishSpeed * Time.deltaTime);
        var velocityTransformation = accelspeed * wishDir;

        velocityToApply += velocityTransformation;
    }

    private void ApplyFriction()
    {
        var speed = velocityToApply.magnitude;

        // Don't apply friction if the player isn't moving
        // Clear speed if it's too low to prevent accidental movement
        // Also makes the player's friction feel more snappy
        if (speed < PlayerConstants.MinimumSpeedCutoff)
        {
            Vector3 noVelocity = velocityToApply; // don't reset y velocity
            noVelocity.x = 0;
            noVelocity.z = 0;
            velocityToApply = noVelocity;
            return;
        }

        // Bleed off some speed, but if we have less than the bleed
        // threshold, bleed the threshold amount.
        var control = (speed < PlayerConstants.StopSpeed) ? PlayerConstants.StopSpeed : speed;

        // Add the amount to the loss amount.
        var lossInSpeed = control * PlayerConstants.Friction * Time.deltaTime;
        var newSpeed = Mathf.Max(speed - lossInSpeed, 0);

        if (newSpeed != speed)
        {
            velocityToApply.x *= newSpeed / speed; //Scale velocity based on friction
            velocityToApply.z *= newSpeed / speed; //Scale velocity based on friction
        }
    }

    // This function keeps the player from exceeding a maximum velocity
    private void ClampVelocity(float maxLength)
    {
        velocityToApply = Vector3.ClampMagnitude(velocityToApply, maxLength);
    }

    private Vector3 ClipVelocity(Vector3 normal)
    {
        Vector3 toReturn = velocityToApply;

        // Determine how far along plane to slide based on incoming direction.
        float backoff = Vector3.Dot(velocityToApply, normal);

        var change = normal * backoff;
        toReturn -= change;

        // iterate once to make sure we aren't still moving through the plane
        float adjust = Vector3.Dot(toReturn, normal);
        if (adjust < 0)
        {
            toReturn -= (normal * adjust);
        }

        return toReturn;
    }

    public void CheckNoClip()
    {
        if(Input.GetKeyDown(KeyCode.V))
        {
            noClip = !noClip;
            controller.enabled = !noClip;
        }
    }

    private void NoClipMove()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.position += mainCamera.transform.forward * Time.deltaTime * PlayerConstants.NoClipMoveSpeed;
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position += -mainCamera.transform.forward * Time.deltaTime * PlayerConstants.NoClipMoveSpeed;
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += mainCamera.transform.right * Time.deltaTime * PlayerConstants.NoClipMoveSpeed;
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.position += -mainCamera.transform.right * Time.deltaTime * PlayerConstants.NoClipMoveSpeed;
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.position += mainCamera.transform.up * Time.deltaTime * PlayerConstants.NoClipMoveSpeed;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            transform.position += -mainCamera.transform.up * Time.deltaTime * PlayerConstants.NoClipMoveSpeed;
        }
    }
    private void CheckHealth()
    {
        var dissonance = FindObjectOfType<DissonanceComms>();

        if (CompareTag("DeadPlayer") || playerHealth <= 0)
        {
            dissonance.IsMuted = true;
            gameObject.layer = default;
            gameObject.tag = "DeadPlayer";
            fadeBlack.gameObject.SetActive(true);
            //playerCamera.enabled = false;
            spectatorCamera.gameObject.SetActive(true);
            spectatorCamera.transform.SetParent(null);
            controller.enabled = false;
            audioListener.enabled = false;
            //playerUI.SetActive(false);
            playerStaminaUI.SetActive(false);
            playerItemHolder.SetActive(false);
            GetComponent<ItemPickup>().DropObject2ServerRpc();
            StartCoroutine(FadeImage(true));
            var playerModelColliders = GetComponentsInChildren<Collider>();
            foreach (var collider in playerModelColliders)
            {
                collider.enabled = true;
            }
            //this.enabled = false;
        }
    }

    [ServerRpc]
    public void KillPlayerServerRpc()
    {
        playerHealth = 0;
    }

    IEnumerator FadeImage(bool fadeAway)
    {
        // fade from transparent to opaque
        yield return new WaitForSeconds(2);
        if (fadeAway)
        {
            fadeBlack.alpha -= Time.deltaTime;
            if (fadeBlack.alpha <= 0)
            {
                playerCamera.gameObject.SetActive(false);
                spectatorCamera.GetComponent<SpectatorCamera>().playersToSpectate.Remove(this.transform);
                this.enabled = false;
            }
            yield return null;
        }
    }

    private void HandleLobbyUI()
    {
        lobbyUICanvas = GameObject.FindGameObjectWithTag("LobbyUI");
        if (lobbyUICanvas == null)
        {
            return;
        }

        if (lobbyUICanvas.activeInHierarchy == true)
        {
            lobbyUICanvas.gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    public void ChangeTagClientRpc()
    {
        gameObject.layer = default;
        gameObject.tag = "DeadPlayer";
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdatePlayerStateServerRpc(PlayerState newState)
    {
        networkPlayerState.Value = newState;
    }
    private void ClientVisuals()
    {
        if(networkPlayerState.Value == PlayerState.walk)
        {
            playerAnimator.SetFloat("Walk", 1);
            clientPlayerAnimator.SetFloat("Walk", 1);
        }
        else if (networkPlayerState.Value == PlayerState.reverseWalk)
        {
            playerAnimator.SetFloat("Walk", -1);
            clientPlayerAnimator.SetFloat("Walk", -1);
        }
        else if (networkPlayerState.Value == PlayerState.idle)
        {
            playerAnimator.SetFloat("Walk", 0);
            clientPlayerAnimator.SetFloat("Walk", 0);
        }
        else if (networkPlayerState.Value == PlayerState.sprint)
        {
            playerAnimator.SetFloat("Walk", 2);
            clientPlayerAnimator.SetFloat("Walk", 2);
        }
        else if (networkPlayerState.Value == PlayerState.jump)
        {
            playerAnimator.SetFloat("Jump", 1);
        }
    }
}