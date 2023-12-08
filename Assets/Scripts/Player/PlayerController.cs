using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Dissonance.Integrations.Unity_NFGO;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float crouchSpeed = 2.5f;
    public float jumpForce = 7f;
    public float crouchHeight = 0.5f;
    public float standingHeight = 1f;
    public float maxStamina = 100f;
    public float staminaDecreaseRate = 20f;
    public float staminaIncreaseRate = 10f;
    public float gravity = 9.81f;
    public Transform playerCamera;
    public AudioListener audioListener;
    public GameObject nfgoPlayer;

    private CharacterController characterController;
    private bool isCrouching;
    private bool isSprinting;
    public float currentStamina;
    private float verticalVelocity;
    private float yVelocity;
    private bool jumping = false;

    [SerializeField] private Image staminaProgressUI = null;
    [SerializeField] private CanvasGroup sliderCanvasGroup = null;
    public GameObject playerUI;
    
    private float staminaRegenInterval = 0.1f;
    private  float staminaRegenTimer = 0;

    private void Start()
    {
        //characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentStamina = maxStamina;
    }

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        //base.OnNetworkSpawn();

        if (IsLocalPlayer)
        {
            NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += SceneManager_OnSynchronizeComplete;
        }

        if (!IsLocalPlayer)
        {
            audioListener.enabled = false;
            characterController.enabled = false;
        }

        if(!IsOwner)
        {
            characterController.enabled = false;
            playerUI.SetActive(false);
            this.enabled = false;
            Debug.Log("Disabling other player controller!");
        }
        else
        {
            characterController.enabled = false;
            this.transform.position = new Vector3(0, 0, 2);
            //playerUI.enabled = true;
            characterController.enabled = true;
            Debug.Log("Spawning Player with correct controller");
        }
    }

    private void SceneManager_OnSynchronizeComplete(ulong clientId)
    {
        nfgoPlayer.gameObject.SetActive(true);
        //throw new System.NotImplementedException();
    }

    private void Update()
    {
        if(!IsOwner)
        {
            characterController.enabled = false;
            this.enabled = false;
            return;
        }

        HandleMovementInput();
        HandleMouseLook();
        HandleCrouch();
        HandleSprint();
        HandleJump();
        RegenerateStamina();

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontal, 0, 0);
        Vector3 velocity = direction * jumpForce;

        if (Input.GetButtonDown("Jump") && currentStamina > 15 && characterController.isGrounded)
        {
            jumping = true;
        }
        else
        {
            jumping = false;
        }
        velocity.y = yVelocity;
       // characterController.Move(velocity * Time.deltaTime);

        if(jumping)
        {
            characterController.Move(velocity * Time.deltaTime);
            jumping = false;
        }
    }
    private void HandleMovementInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
        Vector3 moveAmount; // Declare moveAmount outside the if statement
        float speed; // Declare speed outside the if-else statement

        speed = isSprinting ? sprintSpeed : walkSpeed;

        // Apply gravity when not grounded
        if (!characterController.isGrounded)
        {
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }
        else
        {
            verticalVelocity = -0.5f; // Reset vertical velocity when grounded
        }

        if (!characterController.isGrounded)
        {
            characterController.Move(Vector3.down * gravity * Time.deltaTime);
        }

        moveAmount = playerCamera.TransformDirection(moveDirection) * speed * Time.deltaTime;
        moveAmount.y = verticalVelocity * Time.deltaTime; // Apply vertical velocity
        characterController.Move(moveAmount);

    }

        private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        transform.Rotate(Vector3.up * mouseX);

        Vector3 rotation = playerCamera.rotation.eulerAngles;
        rotation.x -= mouseY;
        playerCamera.rotation = Quaternion.Euler(rotation);
    }

    private void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = !isCrouching;

            if (isCrouching)
            {
                characterController.height = crouchHeight;
                // Adjust the center to avoid getting stuck
                characterController.center = new Vector3(0f, crouchHeight / 2f, 0f);
            }
            else
            {
                characterController.height = standingHeight;
                // Reset the center to the default value
                characterController.center = Vector3.zero;
            }
        }
    }

    private void HandleSprint()
    {

        isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching && currentStamina > 0;

        if(isSprinting)
        {

            currentStamina -= staminaDecreaseRate * Time.deltaTime;
            UpdateStamina(1);
        }

        if(!isSprinting)
        {
            if(currentStamina <= maxStamina - 0.01)
            {
                UpdateStamina(1);
                if(currentStamina >= maxStamina)
                {
                    sliderCanvasGroup.alpha = 0;
                }
            }
        }

        if(currentStamina <= 0)
        {
            sliderCanvasGroup.alpha = 0;
        }
    }


    private void RegenerateStamina()
    {
        staminaRegenTimer += Time.deltaTime;
        if (!isSprinting && staminaRegenTimer > staminaRegenInterval)
        {
            currentStamina += staminaIncreaseRate;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

            staminaRegenTimer = 0;
        }
    }
    private void HandleJump()
    {
        float jumpSpeed = Mathf.Sqrt(2 * jumpForce * Mathf.Abs(Physics.gravity.y));

        if (Input.GetButtonDown("Jump") && currentStamina > 15 && characterController.isGrounded)
        {

            // Apply the initial jump force gradually
            //StartCoroutine(JumpSmoothly());
            verticalVelocity = jumpForce;
            currentStamina -= 15f;
            Debug.Log("Jumping");
            UpdateStamina(1);
        }
    }

    private IEnumerator JumpSmoothly()
    {
        float jumpSpeed = Mathf.Sqrt(2 * jumpForce * Mathf.Abs(Physics.gravity.y));

        // Ensure the character is grounded before jumping
        while (!characterController.isGrounded)
        {
            yield return null;
        }

        // Jump
        verticalVelocity = jumpSpeed;

        // Wait for a short time to allow the character controller to detect grounding
        yield return new WaitForSeconds(0.1f);

        // Ensure the player is grounded after the jump
        while (!characterController.isGrounded)
        {
            // Move the character controller upwards during the jump
            characterController.Move(Vector3.up * jumpSpeed * Time.deltaTime);

            // Apply gravity manually to the character controller
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            characterController.Move(Vector3.down * verticalVelocity * Time.deltaTime);

            yield return null;
        }

        // Reset vertical velocity
        verticalVelocity = 0f;
    }

    void UpdateStamina(int value)
    {
        staminaProgressUI.fillAmount = currentStamina / maxStamina;

        if(value == 0)
        {
            sliderCanvasGroup.alpha = 0;
        }
        else
        {
            sliderCanvasGroup.alpha = 1;
        }
    }
}