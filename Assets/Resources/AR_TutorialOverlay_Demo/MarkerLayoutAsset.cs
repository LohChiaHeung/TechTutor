using UnityEngine;

[CreateAssetMenu(menuName = "AR/Marker Layout")]
public class MarkerLayoutAsset : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string id = "box";
        public string type = "box";  // "box" or "pin"
        public Vector2 pos01 = new(0.5f, 0.5f);   // normalized center
        public Vector2 size01 = new(0.3f, 0.2f);  // normalized size (for "box")
        public string label = "Area";
    }

    public Entry[] entries;
}
