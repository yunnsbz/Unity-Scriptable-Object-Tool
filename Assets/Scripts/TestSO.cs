using UnityEngine;

[CreateAssetMenu(fileName = "TestConfig", menuName = "Scriptable Objects/New Test Config")]
public class TestSO : ScriptableObject
{
    public int number = 0;
    public float value = 0.0f;
    public string text = "Hello World!";
    public bool flag = false;
    public Vector3 position = Vector3.zero;
    public Quaternion rotation = Quaternion.identity;
    public Color color = Color.white;
    public GameObject gameObject = null;
    public Transform transform = null;
    public Material material = null;
}
