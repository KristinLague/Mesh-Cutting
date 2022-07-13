using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundColorChange : MonoBehaviour {

    public Camera camera;
    public Gradient gradient;
    public float timeUntilChange;

    public float currentTime;
    public float currentColorValue;

    private void Start()
	{
        currentTime = 0;
        currentColorValue = 0f;
    }
    private void Update()
	{
        currentTime += Time.deltaTime;
		currentColorValue += 0.01f;

		if(currentTime >= timeUntilChange)
		{
            
            camera.backgroundColor = gradient.Evaluate(currentColorValue);
            currentTime = 0;
        }

		if(currentColorValue > 1)
		{
            currentColorValue = 0;
        }
    }
}
