using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JourneyData
{
    public List<Vector3> Coordinates { get; set; }

    public int RandomCoordinatesAmount { get; set; }

    public int RandomMinRange { get; set; }

    public int RandomMaxRange { get; set; }

    public bool Loop { get; set; }

    public int TimeToComplete { get; set; }
}
