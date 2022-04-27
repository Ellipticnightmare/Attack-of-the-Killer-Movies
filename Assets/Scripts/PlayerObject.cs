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
        SwapManager.singleton.ToggleCam();
        UpdateMyUI();
    }
    public void disableControl()
    {
        SwapManager.singleton.ToggleCam();
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
    private void Start()
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
            moveDirection = cameraObject.forward * vertical;
            moveDirection += cameraObject.right * horizontal;
            moveDirection.Normalize();
            moveDirection.y = 0;

            float speed = movementSpeed;
            moveDirection *= speed;

            Vector3 projectedVelocity = Vector3.ProjectOnPlane(moveDirection, normalVector);
            rigidbody.velocity = projectedVelocity;

            AnimatorHandler.UpdateAnimatorValues(moveAmount, 0);

            if (AnimatorHandler.canRotate)
                HandleRotation(delta);

            inputActions.PlayerMovement.Swap.performed += ctx => SwapManager.singleton.StartSwap(this);
        }
    }
    #region InputHandling
    public float horizontal, vertical, moveAmount, mouseX, mouseY;
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
                MoveInput(delta);
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
        Crippled,
        Dead
    };
    #endregion
}