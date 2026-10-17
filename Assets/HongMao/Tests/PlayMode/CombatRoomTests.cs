#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HongMao.Tests
{
    public sealed class CombatRoomTests
    {
        const string ScenePath = "Assets/HongMao/Scenes/BattleTestRoom.unity";

        [UnityTest]
        public IEnumerator Room_RestartReloadsNewRoom()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("BattleTestRoom");
            yield return null;
            Object.FindFirstObjectByType<PrototypeInstaller>().Flow.Restart();
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("BattleTestRoom"));
            Assert.That(Object.FindFirstObjectByType<PrototypeInstaller>().Player, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<PlatformEffector2D>(FindObjectsSortMode.None), Has.Length.EqualTo(18));
        }

        [UnityTest]
        public IEnumerator Room_LoadsAndPlayerCanJumpThroughAndLandOnFirstPlatform()
        {
            Time.timeScale = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var installer = Object.FindFirstObjectByType<PrototypeInstaller>();
            Assert.That(installer.Player, Is.Not.Null);
            Assert.That(installer.Enemy, Is.Not.Null);
            installer.Enemy.gameObject.SetActive(false);
            var player = installer.Player;
            var motor = player.GetComponent<PlayerMotor2D>();
            motor.SetInputEnabled(false);
            var body = player.GetComponent<Rigidbody2D>();
            body.position = new Vector2(10,-1.78f);
            body.linearVelocity = new Vector2(0,8.4f);
            float highest = body.position.y;
            for (int i = 0; i < 65; i++)
            {
                yield return new WaitForFixedUpdate();
                highest = Mathf.Max(highest, body.position.y);
            }
            Assert.That(highest, Is.GreaterThan(-.35f), "Player must pass through the underside.");
            Assert.That(player.GetComponent<Collider2D>().bounds.min.y, Is.EqualTo(-1.55f).Within(.08f), "Player must land on platform top.");
            Assert.That(motor.IsGrounded, Is.True);
            Assert.That(Object.FindObjectsByType<PlayerCombatController>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<PlatformEffector2D>(FindObjectsSortMode.None), Has.Length.EqualTo(18));
        }

        [UnityTest]
        public IEnumerator Room_CameraFollowsAndHitShakeDoesNotSnapBackToSpawn()
        {
            Time.timeScale = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var installer = Object.FindFirstObjectByType<PrototypeInstaller>();
            installer.Enemy.gameObject.SetActive(false);
            installer.Player.GetComponent<Rigidbody2D>().position = new(26,5);
            yield return new WaitForSecondsRealtime(.7f);
            var camera = Camera.main;
            Assert.That(camera.transform.position.x, Is.GreaterThan(15));
            Object.FindFirstObjectByType<PrototypeFeedback>().Shake(.1f,.2f);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(camera.transform.position.x, Is.GreaterThan(15));
            Assert.That(camera.transform.localPosition, Is.EqualTo(new Vector3(0,0,-10)));
        }
    }
}
#endif
