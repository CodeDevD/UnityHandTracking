using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateInfo : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}

    void LateUpdate()
    {
        if (Time.frameCount % 3 == 0)
        {
            transform.rotation = Quaternion.Euler(transform.parent.rotation.eulerAngles.x, Camera.main.transform.rotation.eulerAngles.y, transform.parent.rotation.eulerAngles.z);
        }
    }
}
