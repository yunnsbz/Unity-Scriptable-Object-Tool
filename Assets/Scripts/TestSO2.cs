using UnityEngine;

[CreateAssetMenu(fileName = "Test2Config", menuName = "Scriptable Objects/New Test2 Config")]
public class TestSO2 : ScriptableObject
{
    public int[] numberArray;
    public GameObject[] gameObjectArray;
}
