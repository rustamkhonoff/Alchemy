using System;
using System.Collections.Generic;
using Alchemy.Editor.Elements;
using Alchemy.Inspector;
using TMPro.EditorUtilities;
using UnityEngine;

public class TestList : MonoBehaviour
{
    [ListViewSettings]
    public List<Test> Values = new();

    [ListViewSettings, SerializeReference]
    public List<ITest> Tests = new();

    [Serializable]
    public class Test
    {
        public int A = 11;
        public string B = "Hello";
    }

    public interface ITest { }

    [Serializable]
    public class MyClassA : ITest
    {
        public int A;
    }

    [Serializable]
    public class MyClassB : ITest
    {
        public string B;
    }
}