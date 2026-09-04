using UnityEngine;

namespace HongMao
{
    public static class PrototypeVisuals
    {
        static Texture2D s_WhiteTexture;
        static Sprite s_WhiteSprite;

        public static Sprite WhiteSprite
        {
            get
            {
                if (s_WhiteSprite != null) return s_WhiteSprite;
                s_WhiteTexture = new Texture2D(1, 1) { name = "HongMao_RuntimeWhite" };
                s_WhiteTexture.SetPixel(0, 0, Color.white);
                s_WhiteTexture.Apply();
                s_WhiteSprite = Sprite.Create(s_WhiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                s_WhiteSprite.name = "HongMao_RuntimeSquare";
                return s_WhiteSprite;
            }
        }

        public static SpriteRenderer AddBoxRenderer(GameObject target, Color color, int order = 0)
        {
            SpriteRenderer renderer = target.AddComponent<SpriteRenderer>();
            renderer.sprite = WhiteSprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        public static void FlashBox(Vector2 center, Vector2 size, Color color, float duration)
        {
            GameObject flash = new("HitboxFlash");
            flash.transform.position = center;
            flash.transform.localScale = size;
            AddBoxRenderer(flash, color, 8);
            FlashLifetime life = flash.AddComponent<FlashLifetime>();
            life.Duration = duration;
        }
    }

    public sealed class FlashLifetime : MonoBehaviour
    {
        public float Duration { get; set; }

        void Update()
        {
            Duration -= Time.unscaledDeltaTime;
            if (Duration <= 0f) Destroy(gameObject);
        }
    }
}
