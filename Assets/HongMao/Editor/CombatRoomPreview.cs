using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HongMao.Editor
{
    public static class CombatRoomPreview
    {
        public static void Render()
        {
            EditorSceneManager.OpenScene(CombatRoomAuthoring.ScenePath);
            Camera camera = Object.FindFirstObjectByType<Camera>();
            Capture(camera, new Vector3(0, 3, -10), 12, 2400, 800, "/tmp/hongmao-room-overview.png");
            Capture(camera, new Vector3(-3, 1.5f, -10), 6.5f, 1600, 900, "/tmp/hongmao-room-spawn.png");
        }

        static void Capture(Camera camera, Vector3 position, float size, int width, int height, string path)
        {
            camera.transform.position = position;
            camera.orthographicSize = size;
            var target = new RenderTexture(width, height, 24);
            var previous = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(width,height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,width,height),0,0);
            image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = previous;
            Object.DestroyImmediate(image);
            target.Release();
            Object.DestroyImmediate(target);
        }
    }
}
