﻿namespace GBX.NET.Inputs;

public readonly record struct Gas(TimeInt32 Time, int Value) : IInputReal
{
    
}