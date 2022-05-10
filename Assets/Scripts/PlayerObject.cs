using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class PlayerObject : MonoBehaviour
{
    #region taskManagement
    public List<Task> myTaskChecks = new List<Task>();
    public List<TaskUI> myTasks = new List<TaskUI>();
    public List<GameObject> myTaskObjects = new List<GameObject>();
    public GameObject myCanvas, myTaskListHolder;
    public GameObject newTaskObject;
    public void gainedTask(Task newTask)
    {
        myTaskChecks.Add(newTask);
    }
    public void BuildMyTaskList()
    {
        myTaskChecks = myTaskChecks.Distinct().ToList();
        foreach (var item in myTaskChecks.ToList())
        {
            GameObject newTaskUIObj = Instantiate(newTaskObject, myTaskListHolder.transform);
            TaskUI newTask = newTaskUIObj.GetComponent<TaskUI>();
            newTask.myTask = item;
            myTasks.Add(newTask);
            myTaskObjects.Add(newTaskUIObj);
        }
        myTasks = myTasks.Distinct().ToList();
        UpdateMyUI();
    }
    public void UpdateMyUI()
    {
        List<TaskUI> newMyTasks = new List<TaskUI>();
        foreach (var obj in myTaskObjects)
        {
            Destroy(obj);
        }
        foreach (var data in myTasks)
        {
            GameObject newTaskUIObj = Instantiate(newTaskObject, myTaskListHolder.transform);
            TaskUI newVals = newTaskUIObj.GetComponent<TaskUI>();
            newVals.numberCompleted = data.numberCompleted;
            newVals.myPlayer = this;
            newVals.myTask = data.myTask;
            myTaskObjects.Add(newTaskUIObj);
            newMyTasks.Add(newVals);
        }
        myTasks = newMyTasks;
        foreach (var item in myTasks)
        {
            item.GenerateTaskUI();
        }
    }
    #endregion
    #region playerSwitching
    //IMPORTANT
    public bool isFocus;
    //END IMPORTANT
    public Sprite actorFace;
    public RenderTexture showUICam;
    public void enableControl()
    {
        isFocus = true;
        myCanvas.SetActive(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        CameraHandler.singleton.targetTransform = this.transform;
        SwapManager.singleton.mainCam.SetActive(true);
        SwapManager.singleton.topCam.SetActive(false);
        UpdateMyUI();
    }
    public void disableControl()
    {
        SwapManager.singleton.mainCam.SetActive(false);
        SwapManager.singleton.topCam.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
        isFocus = false;
        myCanvas.SetActive(false);
    }
    #endregion
    #region playerGameplay
    Transform cameraObject;
    Vector3 moveDirection;
    [HideInInspector]
    public Transform myTransform;
    [HideInInspector]
    public AnimatorHandler AnimatorHandler;

    public new Rigidbody rigidbody;
    public GameObject normalCamera;
    [Header("Stats")]
    public PlayerState playerState = PlayerState.Healthy;
    [SerializeField]
    float movementSpeed = 5;
    [SerializeField]
    float rotationSpeed = 10;
    public bool isSprinting, isCrouch, isGrounded, isInAir, isInteracting;
    float sprintTimer = 0;
    [Header("Ground & Air Detection Stats")]
    float groundDetectionRayStartPoint = .5f;
    float minimumDistanceNeededToBeginFall = 1f;
    float groundDetectionRayDistance = .2f;
    public LayerMask ignoreForGroundCheck;
    public float inAirTimer;
    public float fallingSpeed = 45;
    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        cameraObject = GameObject.FindGameObjectWithTag("MainCamera").GetComponentInChildren<Camera>().transform;
        AnimatorHandler = GetComponentInChildren<AnimatorHandler>();
        myTransform = transform;
        AnimatorHandler.Initialize();
        cameraHandler = CameraHandler.singleton;
    }
    private void Update()
    {
        if (isFocus)
        {
            float delta = Time.deltaTime;
            TickInput(delta);
            HandleSprintInput(delta);
            HandleFalling(delta, moveDirection);
            moveDirection = cameraObject.forward * vertical;
            moveDirection += cameraObject.right * horizontal;
            moveDirection.Normalize();
            moveDirection.y = 0;

            if ((EnemyManager.instance.difficultyCheck >= 15 && !isCrouch) || isSprinting)
            {
                sprintTimer -= Time.deltaTime;
                if (sprintTimer <= 0)
                {
                    sprintTimer = 1;
                    GameObject newSoundPoint = Instantiate(GameManager.instance.SoundPoint, this.transform);
                    float bonusTime = EnemyManager.instance.difficultyCheck > 5 ? Mathf.Clamp(EnemyManager.instance.difficultyCheck, 5, 10) / 10 : 0;
                    newSoundPoint.GetComponent<SoundPoint>().Initialize(.1f + bonusTime);
                }
            }
            else
                sprintTimer = 0;

            float speed = isCrouch ? movementSpeed * .75f : isSprinting ? movementSpeed * 1.45f : movementSpeed;
            moveDirection *= speed;

            Vector3 projectedVelocity = Vector3.ProjectOnPlane(moveDirection, normalVector);
            rigidbody.velocity = projectedVelocity;

            AnimatorHandler.UpdateAnimatorValues(moveAmount, 0);

            if (AnimatorHandler.canRotate)
                HandleRotation(delta);

            inputActions.PlayerMovement.Swap.performed += ctx => SwapManager.singleton.StartSwap(this);
        }
        else
            AnimatorHandler.UpdateAnimatorValues(0, 0);
    }
    public void takeDamage()
    {
        Debug.Log("AttackedByEnemy");
        switch (playerState)
        {
            case PlayerState.Healthy:
                playerState = PlayerState.Injured;
                break;
            case PlayerState.Injured:
                playerState = PlayerState.Crippled;
                break;
            case PlayerState.Crippled:
                GameManager.instance.PlayerDied(this);
                break;
        }
    }
    #region InputHandling
    public float horizontal, vertical, moveAmount, mouseX, mouseY, runTimer;
    Vector2 movementInput;
    Vector2 cameraInput;
    public PlayerControls inputActions;
    CameraHandler cameraHandler;

    private void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new PlayerControls();
            inputActions.PlayerMovement.Movement.performed += inputActions => movementInput = inputActions.ReadValue<Vector2>();
            inputActions.PlayerMovement.Camera.performed += i => cameraInput = i.ReadValue<Vector2>();
        }
        inputActions.Enable();
    }
    private void FixedUpdate()
    {
        float delta = Time.fixedDeltaTime;

        if (isFocus)
        {
            if (cameraHandler != null)
            {
                cameraHandler.FollowTarget(delta);
                cameraHandler.HandleCameraRotation(delta, mouseX, mouseY);
            }
            else
            {
                cameraHandler = CameraHandler.singleton;
            }
        }
    }
    private void OnDisable() => inputActions.Disable();
    public void TickInput(float delta) => MoveInput(delta);
    void MoveInput(float delta)
    {
        horizontal = movementInput.x;
        vertical = movementInput.y;
        moveAmount = Mathf.Clamp01(Mathf.Abs(horizontal) + Mathf.Abs(vertical));
        mouseX = cameraInput.x;
        mouseY = cameraInput.y;
    }
    void HandleSprintInput(float delta)
    {
        bool b_Input = inputActions.PlayerMovement.Run.phase == UnityEngine.InputSystem.InputActionPhase.Performed;
        if (b_Input && !isCrouch)
        {
            runTimer += delta;
            if (runTimer > .25f)
                isSprinting = true;
        }
        else
        {
            isSprinting = false;
            runTimer = 0;
        }
    }
    void HandleCrouchInput(float delta)
    {
        bool b_Input = inputActions.PlayerMovement.Crouch.phase == UnityEngine.InputSystem.InputActionPhase.Performed;
        if (b_Input)
            isCrouch = true;
        else
            isCrouch = false;
    }
    void HandleFalling(float delta, Vector3 moveDirection)
    {
        isGrounded = false;
        RaycastHit hit;
        Vector3 origin = myTransform.position;
        origin.y += groundDetectionRayStartPoint;

        if (Physics.Raycast(origin, myTransform.forward, out hit, 0.4f))
            moveDirection = Vector3.zero;
        if (isInAir)
        {
            rigidbody.AddForce(-Vector3.up * fallingSpeed);
            rigidbody.AddForce(moveDirection * fallingSpeed / 10);
        }

        Vector3 dir = moveDirection;
        dir.Normalize();
        origin = origin + dir * groundDetectionRayDistance;

        targetPosition = myTransform.position;

        if(Physics.Raycast(origin, -Vector3.up, out hit, minimumDistanceNeededToBeginFall, ignoreForGroundCheck))
        {
            normalVector = hit.normal;
            Vector3 tp = hit.point;
            isGrounded = true;
            targetPosition.y = tp.y;
            if (isInAir)
            {
                if(inAirTimer > .5f)
                {
                    AnimatorHandler.PlayTargetAnimation("Land", true);
                    inAirTimer = 0;
                }
                else
                {
                    AnimatorHandler.PlayTargetAnimation("Locomotion", false);
                    inAirTimer = 0;
                }
                isInAir = false;
            }
        }
        else
        {
            if (isGrounded)
                isGrounded = false;
            if (!isInAir)
            {
                if (!isInteracting)
                    AnimatorHandler.PlayTargetAnimation("Falling", true);
                Vector3 vel = rigidbody.velocity;
                vel.Normalize();
                rigidbody.velocity = vel * (movementSpeed / 2);
                isInAir = true;
            }
        }
        if (isGrounded)
        {
            if (isInteracting || moveAmount > 0)
                myTransform.position = Vector3.Lerp(myTransform.position, targetPosition, Time.deltaTime);
            else
                myTransform.position = targetPosition;
        }
    }
    #endregion
    #region Movement
    Vector3 normalVector;
    Vector3 targetPosition;
    void HandleRotation(float delta)
    {
        Vector3 targetDir = Vector3.zero;
        float moveOverride = moveAmount;
        targetDir = cameraObject.forward * vertical;
        targetDir += cameraObject.right * horizontal;
        targetDir.Normalize();
        targetDir.y = 0;
        if (targetDir == Vector3.zero)
            targetDir = myTransform.forward;
        float rs = rotationSpeed;
        Quaternion tr = Quaternion.LookRotation(targetDir);
        Quaternion targetRotation = Quaternion.Slerp(myTransform.rotation, tr, rs * delta);
        myTransform.rotation = targetRotation;
    }
    #endregion

    public enum PlayerState
    {
        Healthy,
        Injured,
        Crippled
    };
    #endregion
}