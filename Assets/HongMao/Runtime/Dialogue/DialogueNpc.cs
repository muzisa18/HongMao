using UnityEngine;

namespace HongMao
{
    [DisallowMultipleComponent]
    public sealed class DialogueNpc : MonoBehaviour
    {
        [SerializeField] DialogueDefinition dialogue;
        [SerializeField] SpriteRenderer visual;
        [SerializeField] Sprite[] idleFrames;
        [SerializeField, Min(1f)] float framesPerSecond = 12f;
        [SerializeField, Min(0.1f)] float interactionRadius = 2.2f;
        float m_AnimationTime;

        public DialogueDefinition Dialogue => dialogue;
        public Vector3 PromptPosition => transform.position + Vector3.up * 2.3f;

        // The root marks the NPC's feet. Compare to the player's feet so lower
        // steps cannot trigger a conversation through the platform above them.
        public bool IsInRange(Vector2 playerFeet)
        {
            Vector2 offset = playerFeet - (Vector2)transform.position;
            return Mathf.Abs(offset.y) < 0.65f && Mathf.Abs(offset.x) <= interactionRadius;
        }

        public void Configure(DialogueDefinition definition, SpriteRenderer renderer, Sprite[] frames)
        {
            dialogue = definition;
            visual = renderer;
            idleFrames = frames;
        }

        void Update()
        {
            if (visual == null || idleFrames == null || idleFrames.Length == 0) return;
            m_AnimationTime += Time.deltaTime;
            visual.sprite = idleFrames[(int)(m_AnimationTime * framesPerSecond) % idleFrames.Length];
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.8f, 0.45f);
            Gizmos.DrawWireCube(transform.position, new Vector3(interactionRadius * 2f, 1.3f, 0f));
        }
    }
}
