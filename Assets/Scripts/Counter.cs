using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Counter
{
    public float currentTime;
    public float endTime;
    public bool hasfinished = false;
    public Counter(float end)
    {
        endTime = end;
        ResetCounter();
    }
    public void ResetCounter()
    {
        currentTime = 0f;
        hasfinished = false;
    }
    public void ResetCounter(float newEndTime)
    {
        endTime = newEndTime;
        currentTime = 0f;
        hasfinished = false;
    }
    public void UpdateCounter(float timeToAdd)
    {
        currentTime += timeToAdd;
        if (currentTime >= endTime)
        {
            hasfinished = true;
            currentTime = endTime;
        }
    }
    public float GetPercentageDone()
    {
        return currentTime / endTime;
    }
}
