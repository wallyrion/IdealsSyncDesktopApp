﻿using System;

namespace BlazorHybridApp.Components;

public class ServerFile
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}