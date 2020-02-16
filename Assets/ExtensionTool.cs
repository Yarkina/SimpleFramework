using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public static class ExtensionTool
{
    public static Tvalue GetValue<Tkey, Tvalue>(this Dictionary<Tkey, Tvalue> dict, Tkey key)
    {
        Tvalue value = default(Tvalue);
        dict.TryGetValue(key, out value);
        return value;
    }
}