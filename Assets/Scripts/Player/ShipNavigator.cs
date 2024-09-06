using Cysharp.Threading.Tasks;
using PathCreation;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class ShipNavigator : MonoBehaviour
{
    [SerializeField]
    private FlyingLogic flyingLogicRef;

    [SerializeField]
    private PathCreator pathCreatorRef;

    [SerializeField]
    private GameObject beaconPrefab;

    [SerializeField]
    private GameObject splineArrowsPrefab;

    [SerializeField]
    private TMP_Text navigatorText;

    [SerializeField]
    private string journeyUrl;

    [SerializeField]
    private string receivingDataText;

    [SerializeField]
    private string flyingToStartText;

    [SerializeField]
    private string readyToStartText;

    [SerializeField]
    private int normalsAngle;

    private readonly CancellationTokenSource cancelationTokenSource = new CancellationTokenSource();

    private void Start()
    {
        UpdateNavigatorMessage("Choose route style:");
        flyingLogicRef.ArrivedAtStart += ShipArrived;
        flyingLogicRef.OnRoute += ShipOnRoute;
    }

    // When call from UI received, generate path
    public async void GeneratePath(bool random)
    {
            JourneyData journeyData = await GetJSONFile();
            if (journeyData.Coordinates.Count > 1 && random == false)
            {
                ConstructSplinePath(journeyData.Coordinates, journeyData.Loop, journeyData.TimeToComplete);
                return;
            }
        GenerateRandomPath(journeyData.RandomCoordinatesAmount, journeyData.RandomMinRange, journeyData.RandomMaxRange,
            journeyData.Loop, journeyData.TimeToComplete);

    }

    // Generate a new random path
    private void GenerateRandomPath(int coordinatesAmount, int locMinRange, int locMaxRange, bool shouldLoop, int completeTime)
    {
        var randomDestinations = new List<Vector3>();
        randomDestinations.Add(new Vector3(transform.position.x, 0, transform.position.z));
        for (int i = 1; i - 1 < coordinatesAmount; i++)
        {
            var newLocation = GenerateRandomLocation(randomDestinations[i - 1], locMinRange, locMaxRange);
            if (i > 3)
            {
                // Check for intersections
                for (int n = 1; n < i - 1; n++)
                {
                    var intersect = IntersectionCheck(randomDestinations[n - 1], randomDestinations[n], 
                        randomDestinations[i - 1], newLocation);
                    var tries = 0;
                    while (tries < 10000 && intersect == true)
                    {
                        newLocation = GenerateRandomLocation(randomDestinations[i - 1], locMinRange, locMaxRange);
                        intersect = IntersectionCheck(randomDestinations[n - 1], randomDestinations[n], 
                            randomDestinations[i - 1], newLocation);
                        ++tries;
                    }
                }
            }
            randomDestinations.Add(newLocation);
        }
        ConstructSplinePath(randomDestinations, shouldLoop, completeTime);
    }

    private Vector3 GenerateRandomLocation(Vector3 previousLocation, int minRange, int maxRange)
    {
        Vector3 newLocation;
        newLocation = previousLocation + new Vector3(Random.Range(minRange, maxRange) * ((Random.value > 0.5f) ? 1 : -1),
            0,
            Random.Range(minRange, maxRange) * ((Random.value > 0.5f) ? 1 : -1));

        return newLocation;
    }

    // Intersection check
    // Taken from https://www.habrador.com/tutorials/math/5-line-line-intersection/
    // MATH, amirite?
    private bool IntersectionCheck(Vector3 line1Start, Vector3 line1End, Vector3 line2Start, Vector3 line2End)
    {
        bool isIntersecting = false;

        //3d -> 2d
        Vector2 p1 = new Vector2(line1Start.x, line1Start.z);
        Vector2 p2 = new Vector2(line1End.x, line1End.z);

        Vector2 p3 = new Vector2(line2Start.x, line2Start.z);
        Vector2 p4 = new Vector2(line2End.x, line2End.z);

        float denominator = (p4.y - p3.y) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.y - p1.y);
        float u_a = ((p4.x - p3.x) * (p1.y - p3.y) - (p4.y - p3.y) * (p1.x - p3.x)) / denominator;
        float u_b = ((p2.x - p1.x) * (p1.y - p3.y) - (p2.y - p1.y) * (p1.x - p3.x)) / denominator;

        //Is intersecting if u_a and u_b are between 0 and 1
        if (u_a >= 0 && u_a <= 1 && u_b >= 0 && u_b <= 1)
        {
            isIntersecting = true;
        }

        return isIntersecting;
    }

    // Load coordinates data from file
    private async UniTask<JourneyData> GetJSONFile()
    {
        UpdateNavigatorMessage(receivingDataText);
        string jsonTxt = "";
        var (IsCanceled, Result) = (await UnityWebRequest.Get(journeyUrl)
            .SendWebRequest()
            .ToUniTask(cancellationToken: cancelationTokenSource.Token)
            .SuppressCancellationThrow());
        if (!IsCanceled)
        {
            jsonTxt = Result.downloadHandler.text;
        }
        return Newtonsoft.Json.JsonConvert.DeserializeObject<JourneyData>(jsonTxt);
    }

    // Create a new bezier path from the waypoints and start flying to the beginning
    private void ConstructSplinePath(List<Vector3> coordinates, bool shouldLoop, int time)
    {
        var bezierSpline = new BezierPath(coordinates, shouldLoop, PathSpace.xyz);
        pathCreatorRef.bezierPath = bezierSpline;
        bezierSpline.GlobalNormalsAngle = normalsAngle;
        flyingLogicRef.SetReady(shouldLoop, time);
        CreateBeacons(coordinates);
        LaunchWaypointEffect(pathCreatorRef);
        UpdateNavigatorMessage(flyingToStartText);
    }

    // Launch the waypoint arrow
    private void LaunchWaypointEffect(PathCreator pathCreatorRef)
    {
        GameObject newGO = Instantiate(splineArrowsPrefab);
        var newArrows = newGO.GetComponent<SplineArrows>();
        newArrows.ReceiveInfo((pathCreatorRef.path.length / 5), pathCreatorRef);
    }

    // Create guiding beacons at coordinates
    private void CreateBeacons(List<Vector3> beaconLocations)
    {
        if (beaconPrefab != null)
        {
            foreach (Vector3 vector in beaconLocations)
            {
                InstBeacon(vector);
            }
        }
        else
        {
            print("Set Beacon prefab to create beacons/waypoints");
        }
    }

    // Create a single beacon
    private void InstBeacon(Vector3 location)
    {
        Instantiate(beaconPrefab, location, Quaternion.identity, pathCreatorRef.transform); 
    }

    // Update HUD text
    private void ShipArrived(object sender, EventArgs e)
    {
        UpdateNavigatorMessage(readyToStartText);
    }

    private void ShipOnRoute(object sender, EventArgs e)
    {
        UpdateNavigatorMessage("");
    }

    private void UpdateNavigatorMessage(string message)
    {
        navigatorText.text = message;
    }

    private void OnDestroy()
    {
        cancelationTokenSource.Cancel();
        flyingLogicRef.ArrivedAtStart -= ShipArrived;
        flyingLogicRef.OnRoute -= ShipOnRoute;
    }
}