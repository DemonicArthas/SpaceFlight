using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUD_Logic : MonoBehaviour
{
    [SerializeField]
    private ShipNavigator navigatorRef;

    [SerializeField]
    private GameObject container;

    private void Start()
    {
        Cursor.visible = true;
    }
    public void GeneratePath(bool random)
    {
        navigatorRef.GeneratePath(random);
        Cursor.visible = false;
        container.SetActive(false);
    }
}
