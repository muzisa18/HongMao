using UnityEngine;

namespace HongMao
{
    [CreateAssetMenu(menuName = "HongMao/Dialogue", fileName = "Dialogue")]
    public sealed class DialogueDefinition : ScriptableObject
    {
        public string speakerName = "引路人";
        [TextArea(3, 8)] public string[] lines;

        public bool HasContent => lines != null && lines.Length > 0;
    }
}
