﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utilis 
{
    // Fractal Brownian Motion
    public static float fBM(float x, float y, int octaves, float persistance)
    {
        
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;
        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(x * frequency, y  * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistance;
            frequency *= 2;
        }
        return total / maxValue;
    }

    // We create a function to make our seamless procedurally generated texture push its values to the extreme. So instead of having something greyish, we push the values closer to the extreme.
    public static float Map (float value, float originalMin, float originalMax, float targetMin, float targetMax)
    {
        return (value - originalMin) * (targetMax - targetMin) / (originalMax - originalMin) + targetMin;
    }

    // Fisher-Yates Shuffle. It swaps the current value that you are looking at with a new random one. 
    public static System.Random r = new System.Random();
    public static void Shuffle<T> (this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = r.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
