

using System;
using System.Diagnostics;

public static class PerformanceChecker
{
    private static float averageScriptTime;

    public static float AverageScriptTime
    {
        get { return averageScriptTime; }
        private set { averageScriptTime = value; }
    }

    public static void CheckMethod(Action methodToCheck, int cycles)
    {
        Stopwatch stopwatch = new Stopwatch();
        float[] checkValues = new float[cycles];


        for (int i = 0; i < cycles; i++)
        {
            stopwatch.Start();
            methodToCheck.Invoke();
            stopwatch.Stop();
            checkValues[i] = stopwatch.ElapsedMilliseconds;
            AverageScriptTime += checkValues[i];
            stopwatch.Reset();
        }

        AverageScriptTime /= cycles;

        UnityEngine.Debug.Log(AverageScriptTime);
    }
}