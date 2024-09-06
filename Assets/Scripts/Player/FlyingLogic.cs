using Cysharp.Threading.Tasks;
using PathCreation;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlyingLogic : MonoBehaviour
{
    [SerializeField]
    private PathCreator pathCreatorRef;

    [SerializeField]
    private ParticleSystem lasersEffect;

    [SerializeField]
    private AudioClip lasersSound;

    [SerializeField]
    private int rotationSpeed = 15;

    private GameObject shipCamera;
    private bool readyForFlight;
    private float speed;
    private float travelledPath;
    private Quaternion rotationTarget;
    private Vector2 mouseDelta;
    private FlyingMode ShipFlyingMode;
    private EndOfPathInstruction end;
    private readonly CancellationTokenSource cancelationTokenSource = new CancellationTokenSource();
    public event EventHandler ArrivedAtStart;
    public event EventHandler OnRoute;
    private enum FlyingMode
    {
        idle,
        toPoint,
        onSpline
    };

    private void Start()
    {
        shipCamera = Camera.main.transform.parent.gameObject;
        rotationTarget = shipCamera.transform.localRotation;
    }

    // On update fly towards current destination
    private void Update()
    {
        var step = speed * Time.deltaTime;

        switch (ShipFlyingMode)
        {
            case FlyingMode.idle:
                if (readyForFlight) 
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, pathCreatorRef.path.GetRotationAtDistance(0, end), 1);
                }
                break;

            case FlyingMode.toPoint:
                if (pathCreatorRef.path == null) break;
                var targetPoint = pathCreatorRef.path.GetPointAtDistance(0, end);
                var calcRotation = (targetPoint - transform.position) == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(targetPoint - transform.position);
                transform.SetPositionAndRotation(Vector3.MoveTowards(transform.position, targetPoint, step),
                    Quaternion.RotateTowards(transform.rotation, calcRotation, 1));
                break;

            case FlyingMode.onSpline:
                travelledPath += step;
                transform.SetPositionAndRotation(pathCreatorRef.path.GetPointAtDistance(travelledPath, end),
                    pathCreatorRef.path.GetRotationAtDistance(travelledPath, end));
                break;
        }
    }

    private void LateUpdate()
    {
        shipCamera.transform.localRotation = Quaternion.Slerp(shipCamera.transform.localRotation, rotationTarget, Time.deltaTime * 10);
    }

    //ShipNavigator will tell us if we should get ready and fly towards the start point
    public void SetReady(bool shouldLoop, int time)
    {
        end = shouldLoop ? EndOfPathInstruction.Loop : EndOfPathInstruction.Stop;
        speed = pathCreatorRef.path.length / time;
        FlyTowardsStart().Forget();
    }

    // Fly towards start point
    private async UniTaskVoid FlyTowardsStart()
    {
        ShipFlyingMode = FlyingMode.toPoint;
        await UniTask.WaitUntil(() => transform.position == pathCreatorRef.path.GetPointAtDistance(0, end),
            cancellationToken: cancelationTokenSource.Token).SuppressCancellationThrow();
        ShipFlyingMode = FlyingMode.idle;
        readyForFlight = true;
        ArrivedAtStart?.Invoke(this, EventArgs.Empty);
    }

    // Controls
    // On pressing Start Journey (Space by default) we check that the ship is ready and start flying on spline
    public void OnStartJourney()
    {
        if (ShipFlyingMode == FlyingMode.idle && readyForFlight)
        {
            ShipFlyingMode = FlyingMode.onSpline;
            OnRoute?.Invoke(this, EventArgs.Empty);
        }
    }

    // Mouse look
    public void Look(InputAction.CallbackContext context)
    {
        if (shipCamera == null) return;
        mouseDelta = context.ReadValue<Vector2>();
        rotationTarget *= Quaternion.AngleAxis(mouseDelta.x * Time.deltaTime * rotationSpeed, Vector3.forward);
    }

    // When detecting asteroids - automatically shoot them down
    private void OnTriggerEnter(Collider other)
    {
        ShootLasers(other.gameObject).Forget();
    }

    // Shoot lasers
    private async UniTaskVoid ShootLasers(GameObject obstacle)
    {
        if (obstacle == null) return;
        lasersEffect.Play();
        AudioSource.PlayClipAtPoint(lasersSound, transform.position, 0.05f);
        await UniTask.Delay(250);
        obstacle.SendMessageUpwards("ReceiveDamage", null, SendMessageOptions.DontRequireReceiver);
    }

    // Quit the game
    public void OnQuit()
    {
        Application.Quit();
    }
    private void OnDestroy()
    {
        cancelationTokenSource.Cancel();
    }
}