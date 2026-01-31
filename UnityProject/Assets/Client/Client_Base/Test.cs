using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    private GameObject testObj;

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update()
    {
        OnUpdate();
        testObj = new GameObject();
    }

    private void OnUpdate()
    {
        if (testObj == null)
            testObj = CreateObj();
    }

    private GameObject CreateObj()
    {
        return new GameObject();
    }
}