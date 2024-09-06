using UnityEngine;
using PathCreation;

public class SplineArrows : MonoBehaviour
{
    private PathCreator pathRef;
    private float arrowsSpeed;
    private float travelledPath;

    public void ReceiveInfo(float speed, PathCreator path)
    {
        arrowsSpeed = speed;
        pathRef = path;
    }

    // Update is called once per frame
    void Update()
    {
        if (pathRef != null)
        {
            var step = arrowsSpeed * Time.deltaTime;
            travelledPath += step;
            transform.SetPositionAndRotation(pathRef.path.GetPointAtDistance(travelledPath, EndOfPathInstruction.Loop),
                                             pathRef.path.GetRotationAtDistance(travelledPath, EndOfPathInstruction.Loop));
        }
    }
}
