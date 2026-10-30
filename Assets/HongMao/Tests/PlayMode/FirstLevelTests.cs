#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace HongMao.Tests
{
    public sealed class FirstLevelTests
    {
        const string ScenePath = "Assets/HongMao/Scenes/FirstLevelGraybox.unity";
        FirstLevelController m_Level;
        Rigidbody2D m_Player;
        PlayerMotor2D m_Motor;
        Collider2D m_Collider;
        Keyboard m_Keyboard;
        Mouse m_Mouse;
        InputSettings.EditorInputBehaviorInPlayMode m_EditorInputBehavior;
        InputSettings.BackgroundBehavior m_BackgroundBehavior;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_EditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            m_BackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            m_Keyboard = InputSystem.AddDevice<Keyboard>();
            m_Mouse = InputSystem.AddDevice<Mouse>();
            Time.timeScale = 1f;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            CacheLevel();
            yield return Settle(3.5f);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (m_Keyboard != null) InputSystem.RemoveDevice(m_Keyboard);
            if (m_Mouse != null) InputSystem.RemoveDevice(m_Mouse);
            InputSystem.settings.editorInputBehaviorInPlayMode = m_EditorInputBehavior;
            InputSystem.settings.backgroundBehavior = m_BackgroundBehavior;
            var time = Object.FindFirstObjectByType<CombatTimeController>();
            if (time != null) time.ForceRestore();
            yield return null;
        }

        void CacheLevel()
        {
            m_Level = Object.FindFirstObjectByType<FirstLevelController>();
            Assert.That(m_Level, Is.Not.Null);
            m_Player = m_Level.Player.GetComponent<Rigidbody2D>();
            m_Motor = m_Level.Player.GetComponent<PlayerMotor2D>();
            m_Collider = m_Level.Player.GetComponent<Collider2D>();
        }

        void Keys(params Key[] keys) => InputSystem.QueueStateEvent(m_Keyboard, new KeyboardState(keys));

        void PlacePlayer(Vector2 center)
        {
            Keys();
            m_Player.position = center;
            m_Player.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }

        IEnumerator Settle(float surface, float timeout = 2f)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            do
            {
                yield return new WaitForFixedUpdate();
                if (m_Motor.IsGrounded && Mathf.Abs(m_Collider.bounds.min.y - surface) < .1f) yield break;
            } while (Time.realtimeSinceStartup < deadline && !m_Level.IsFailed);
            Assert.Fail($"Did not land on y={surface}; player={m_Player.position}, feet={m_Collider.bounds.min.y}, grounded={m_Motor.IsGrounded}.");
        }

        IEnumerator MoveTo(float targetX, float timeout = 3f)
        {
            int direction = targetX >= m_Player.position.x ? 1 : -1;
            Keys(direction > 0 ? Key.D : Key.A);
            float deadline = Time.realtimeSinceStartup + timeout;
            while (direction * (targetX - m_Player.position.x) > .06f && Time.realtimeSinceStartup < deadline && !m_Level.IsFailed)
                yield return null;
            Keys();
            yield return null;
            yield return null;
            Assert.That(m_Player.position.x, Is.EqualTo(targetX).Within(.25f), $"Could not walk to x={targetX}.");
        }

        IEnumerator JumpTo(float targetX, float targetSurface)
        {
            Assert.That(m_Motor.IsGrounded, Is.True, "Jump must begin from a real landing.");
            int direction = targetX >= m_Player.position.x ? 1 : -1;
            Key movement = direction > 0 ? Key.D : Key.A;
            bool verticalJump = Mathf.Abs(targetX - m_Player.position.x) < .15f;
            if (verticalJump) Keys(Key.Space);
            else Keys(movement, Key.Space);
            yield return null;
            yield return null;
            if (verticalJump) Keys();
            else Keys(movement);
            bool leftGround = !m_Motor.IsGrounded || m_Player.linearVelocity.y > .1f;
            bool released = verticalJump;
            float deadline = Time.realtimeSinceStartup + 2.5f;
            while (Time.realtimeSinceStartup < deadline && !m_Level.IsFailed)
            {
                yield return null;
                leftGround |= !m_Motor.IsGrounded || m_Player.linearVelocity.y > .1f;
                if (!released && direction * (targetX - m_Player.position.x) <= .04f)
                {
                    Keys();
                    released = true;
                }
                if (leftGround && released && m_Motor.IsGrounded && Mathf.Abs(m_Collider.bounds.min.y - targetSurface) < .1f)
                {
                    Assert.That(m_Player.position.x, Is.EqualTo(targetX).Within(.25f));
                    yield break;
                }
            }
            Keys();
            Assert.Fail($"Space jump to ({targetX}, top {targetSurface}) failed; player={m_Player.position}, feet={m_Collider.bounds.min.y}, leftGround={leftGround}.");
        }

        IEnumerator Click()
        {
            InputSystem.QueueStateEvent(m_Mouse, new MouseState().WithButton(MouseButton.Left));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(m_Mouse, new MouseState());
            yield return new WaitForSecondsRealtime(.65f);
        }

        IEnumerator Capture(string fileName)
        {
            string directory = Environment.GetEnvironmentVariable("HONGMAO_FIRST_LEVEL_PREVIEW_DIR");
            if (string.IsNullOrEmpty(directory)) yield break;
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(.35f);
            string path = Path.Combine(directory, fileName);
            if (File.Exists(path)) File.Delete(path);
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(.35f);
            if (!File.Exists(path))
            {
                // Some graphic batch runners do not submit ScreenCapture frames.
                // This fallback captures camera geometry; IMGUI is not included.
                Camera camera = Camera.main;
                Assert.That(camera, Is.Not.Null);
                RenderTexture previousTarget = camera.targetTexture;
                RenderTexture previousActive = RenderTexture.active;
                var render = new RenderTexture(1280, 720, 24);
                var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                try
                {
                    render.Create();
                    camera.targetTexture = render;
                    camera.Render();
                    RenderTexture.active = render;
                    image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(path, image.EncodeToPNG());
                    Debug.Log("FIRST_LEVEL_CAMERA_ONLY_PREVIEW: " + path);
                }
                finally
                {
                    camera.targetTexture = previousTarget;
                    RenderTexture.active = previousActive;
                    Object.Destroy(image);
                    render.Release();
                    Object.Destroy(render);
                }
            }
            Assert.That(File.Exists(path), Is.True, "Requested gameplay preview was not captured.");
        }

        IEnumerator ReachExit()
        {
            Keys(Key.D);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!m_Level.IsComplete && Time.realtimeSinceStartup < deadline) yield return null;
            Keys();
            Assert.That(m_Level.IsComplete, Is.True, "The authored exit must be reachable by ordinary movement.");
            Assert.That(m_Level.IsFailed, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        IEnumerator RestartWithEnter()
        {
            FirstLevelController previous = m_Level;
            Keys(Key.Enter);
            yield return null;
            yield return null;
            Keys();
            float deadline = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                var candidate = Object.FindFirstObjectByType<FirstLevelController>();
                if (candidate != null && candidate != previous)
                {
                    CacheLevel();
                    yield return Settle(3.5f);
                    yield break;
                }
            }
            Assert.Fail("Enter did not reload the authored first-level scene.");
        }

        [UnityTest]
        public IEnumerator Scene_HasOnePlayableSetAndSafeOpeningDrop()
        {
            Assert.That(Object.FindObjectsByType<PlayerCombatController>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<CombatBotController>(FindObjectsSortMode.None), Has.Length.EqualTo(2));
            Assert.That(Object.FindObjectsByType<PrototypeInstaller>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None), Is.Empty,
                "Sketch annotations are authoring notes, never player-facing world text.");
            var startPlatform = GameObject.Find("World/Geometry/01 Start platform").GetComponent<BoxCollider2D>();
            Assert.That(startPlatform.bounds.size.y, Is.GreaterThan(6), "A raised platform must have substantial vertical mass.");
            var roof = GameObject.Find("World/Geometry/06 Hollow right platform/Roof").GetComponent<BoxCollider2D>();
            Assert.That(roof.bounds.size.y, Is.GreaterThan(1));
            Assert.That(Physics2D.OverlapPoint(new Vector2(43, -2), ~0), Is.Null,
                "The right platform's interior must remain hollow above the second enemy.");
            int tutorialSprites = 0;
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (renderer.sprite != null && UnityEditor.AssetDatabase.GetAssetPath(renderer.sprite).Contains("TutorialPlatformer")) tutorialSprites++;
            Assert.That(tutorialSprites, Is.GreaterThan(100), "The level must reuse the tutorial environment assets.");
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    foreach (Component component in child.GetComponents<Component>())
                        Assert.That(component, Is.Not.Null, "Missing script on " + child.name);
            Assert.That(m_Level.TotalRewards, Is.EqualTo(2));
            yield return Capture("01-spawn.png");
            PlacePlayer(new Vector2(7f, 4.43f));
            yield return Settle(3.5f);
            yield return MoveTo(10.4f);
            yield return Settle(0f);
            Assert.That(m_Level.PlayerBody.Health, Is.EqualTo(m_Level.PlayerBody.MaxHealth));
            Assert.That(m_Level.IsFailed || m_Level.IsComplete, Is.False);
            Assert.That(m_Level.Enemies[1].transform.position.x, Is.EqualTo(43f).Within(.1f), "Distant lower enemy must stay in its encounter.");
        }

        [UnityTest]
        public IEnumerator OrdinaryAttack_DamagesAndKillsDummyWithoutFinishingLevel()
        {
            PlacePlayer(new Vector2(13.6f, .93f));
            yield return Settle(0f);
            CombatantBody target = m_Level.Enemies[0].Body;
            int initialHealth = target.Health;
            yield return Click();
            Assert.That(target.Health, Is.LessThan(initialHealth), "A real left click must execute a damaging ordinary attack.");
            for (int i = 0; i < 4 && !target.IsDead; i++) yield return Click();
            Assert.That(target.IsDead, Is.True);
            Assert.That(m_Level.IsComplete || m_Level.IsFailed, Is.False, "Enemy death does not finish the traversal level.");
            Assert.That(m_Level.Enemies[1].Body.IsDead, Is.False);
            yield return MoveTo(20.4f);
            Assert.That(m_Player.position.x, Is.GreaterThan(20f), "The defeated target must not block continued movement along the main route.");
        }

        [UnityTest]
        public IEnumerator MainRoute_FourSpaceJumpsLandOnEveryRisingPlatform()
        {
            PlacePlayer(new Vector2(20.4f, .93f));
            yield return Settle(0f);
            float[] destinations = { 23.2f, 26.3f, 29.4f, 32.7f };
            for (int i = 0; i < destinations.Length; i++)
            {
                yield return JumpTo(destinations[i], i + 1f);
                Assert.That(m_Motor.IsGrounded, Is.True, $"Platform {i + 1} must provide a stable landing.");
            }
            Assert.That(m_Level.IsFailed, Is.False);
            yield return Capture("02-upper-jumps.png");
            yield return MoveTo(41.5f);
            yield return JumpTo(41.5f, 5.15f);
            yield return null;
            Assert.That(m_Level.CollectedRewards, Is.EqualTo(1), "The upper reward must be reachable from the main route.");
            yield return MoveTo(52.3f);
            yield return Settle(4f);
            yield return JumpTo(56.2f, 5f);
            yield return MoveTo(62.2f);
            yield return JumpTo(65f, 6.1f);
            yield return ReachExit();
        }

        [UnityTest]
        public IEnumerator LowerExploration_SpaceJumpsReachTheHiddenReward()
        {
            PlacePlayer(new Vector2(23.3f, -7.07f));
            yield return Settle(-8f);
            yield return JumpTo(21.2f, -6.9f);
            yield return JumpTo(20.4f, -5.8f);
            yield return JumpTo(17f, -5f);
            yield return MoveTo(15.4f);
            yield return JumpTo(13.5f, -4.2f);
            yield return null;
            Assert.That(m_Level.CollectedRewards, Is.EqualTo(1), "The hidden reward must be physically reachable from the lower corridor.");
            Assert.That(m_Level.IsFailed, Is.False);
            yield return Capture("03-lower-exploration.png");
        }

        [UnityTest]
        public IEnumerator LowerRoute_EntersHollowChamberAndSlidesDownToLadder()
        {
            PlacePlayer(new Vector2(33f, -7.07f));
            yield return Settle(-8f);
            yield return JumpTo(35.5f, -6.9f);
            yield return JumpTo(38.2f, -5.8f);
            CombatBotController guard = m_Level.Enemies[1];
            Rigidbody2D guardBody = guard.GetComponent<Rigidbody2D>();
            for (int i = 0; i < 8 && !guard.Body.IsDead; i++)
            {
                yield return MoveTo(guardBody.position.x - 1.2f);
                // A small overshoot correction can face left; explicitly face the guard.
                Keys(Key.D);
                yield return null;
                yield return null;
                Keys();
                yield return Click();
            }
            Assert.That(guard.Body.IsDead, Is.True, "Ordinary attacks must defeat the optional lower-route guard.");
            Assert.That(m_Level.IsComplete || m_Level.IsFailed, Is.False);
            yield return MoveTo(48f);
            yield return Settle(-5.8f);
            Keys(Key.D);
            var slide = Object.FindFirstObjectByType<FirstLevelSlide>();
            float slideDeadline = Time.realtimeSinceStartup + 3;
            while (!slide.HasBeenUsed && Time.realtimeSinceStartup < slideDeadline) yield return null;
            Assert.That(slide.HasBeenUsed, Is.True, "Leaving the chamber must enter the actual inclined slide.");
            Keys();
            while (slide.IsSliding && Time.realtimeSinceStartup < slideDeadline) yield return null;
            yield return Settle(-9.4f);
            yield return MoveTo(54.6f);
            Assert.That(m_Level.IsFailed, Is.False);
            Assert.That(m_Level.PlayerBody.Health, Is.GreaterThan(0));
            Assert.That(Object.FindFirstObjectByType<FirstLevelLadder>().IsClimbing, Is.False, "Walking to the ladder does not climb without W.");
        }

        [UnityTest]
        public IEnumerator BothRewards_CollectOnlyOnceWhenLandingOnTheirPedestals()
        {
            var pickups = Object.FindObjectsByType<FirstLevelPickup>(FindObjectsSortMode.None);
            Vector2[] centers = { new(41.5f, 6.8f), new(13.5f, -2.6f) };
            float[] surfaces = { 5.15f, -4.2f };
            for (int i = 0; i < centers.Length; i++)
            {
                PlacePlayer(centers[i]);
                yield return Settle(surfaces[i]);
                yield return null;
                Assert.That(m_Level.CollectedRewards, Is.EqualTo(i + 1));
                PlacePlayer(centers[i]);
                yield return Settle(surfaces[i]);
                Assert.That(m_Level.CollectedRewards, Is.EqualTo(i + 1), "Revisiting the same star cannot add another reward.");
            }
            foreach (FirstLevelPickup pickup in pickups)
            {
                Assert.That(pickup.IsCollected, Is.True);
                foreach (SpriteRenderer renderer in pickup.GetComponentsInChildren<SpriteRenderer>()) Assert.That(renderer.enabled, Is.False);
            }
            Assert.That(m_Level.IsComplete, Is.False, "Rewards remain optional and do not trigger level completion.");
        }

        [UnityTest]
        public IEnumerator Ladder_WClimbsToFinalJumpAndExitThenEnterResetsProgress()
        {
            PlacePlayer(new Vector2(41.5f, 6.8f));
            yield return Settle(5.15f);
            Assert.That(m_Level.CollectedRewards, Is.EqualTo(1));
            PlacePlayer(new Vector2(54.6f, -8.47f));
            yield return Settle(-9.4f);
            var ladder = Object.FindFirstObjectByType<FirstLevelLadder>();
            Keys(Key.W);
            yield return new WaitForSecondsRealtime(.9f);
            Assert.That(ladder.IsClimbing && ladder.HasBeenUsed, Is.True);
            Assert.That(m_Motor.enabled || m_Level.Player.enabled, Is.False, "Climbing suspends the normal movement and combat components.");
            yield return Capture("04-recovery-ladder.png");
            float deadline = Time.realtimeSinceStartup + 4f;
            while (ladder.IsClimbing && Time.realtimeSinceStartup < deadline) yield return null;
            Keys();
            yield return Settle(5f);
            Assert.That(ladder.IsClimbing, Is.False);
            Assert.That(m_Player.position.x, Is.EqualTo(56.9f).Within(.15f));
            Assert.That(m_Player.gravityScale, Is.EqualTo(2.2f).Within(.01f));
            Assert.That(m_Motor.enabled && m_Level.Player.enabled, Is.True);
            yield return MoveTo(62.2f);
            yield return JumpTo(65f, 6.1f);
            yield return ReachExit();
            yield return Capture("05-exit-complete.png");
            yield return RestartWithEnter();
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(ScenePath));
            Assert.That(m_Level.IsComplete || m_Level.IsFailed, Is.False);
            Assert.That(m_Level.CollectedRewards, Is.Zero);
            Assert.That(m_Level.PlayerBody.Health, Is.EqualTo(m_Level.PlayerBody.MaxHealth));
        }

        [UnityTest]
        public IEnumerator DeathAndOutOfBounds_LockGameplayAndAllowRestart()
        {
            m_Level.PlayerBody.ReceiveHit(new HitRequest(98765, 9999, 0, Vector2.zero, false, false, null));
            yield return null;
            Assert.That(m_Level.IsFailed, Is.True);
            float x = m_Player.position.x;
            Keys(Key.D, Key.Space);
            InputSystem.QueueStateEvent(m_Mouse, new MouseState().WithButton(MouseButton.Left));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(m_Player.position.x, Is.EqualTo(x).Within(.05f));
            Assert.That(m_Level.Player.IsActionBusy, Is.False);
            InputSystem.QueueStateEvent(m_Mouse, new MouseState());
            yield return RestartWithEnter();
            Assert.That(m_Level.IsFailed, Is.False);
            PlacePlayer(new Vector2(25f, -15f));
            float deadline = Time.realtimeSinceStartup + .5f;
            while (!m_Level.IsFailed && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(m_Level.IsFailed, Is.True, "Leaving the authored map must produce a restartable failure.");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Slope_IsContinuousAndRejectsReverseMovementAndJumping()
        {
            var slide = Object.FindFirstObjectByType<FirstLevelSlide>();
            var collider = slide.GetComponent<PolygonCollider2D>();
            Assert.That(collider, Is.Not.Null);
            Assert.That(slide.GetComponentsInChildren<BoxCollider2D>(), Is.Empty, "The slide must use a sloping collider rather than stair steps.");
            PlacePlayer(new Vector2(48f, -4.87f));
            yield return Settle(-5.8f);
            Keys(Key.D);
            float deadline = Time.realtimeSinceStartup + 2;
            while (!slide.IsSliding && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(slide.IsSliding, Is.True);
            float previousX = m_Player.position.x;
            Keys(Key.A, Key.Space, Key.LeftShift);
            yield return null;
            yield return null;
            Keys(Key.A);
            deadline = Time.realtimeSinceStartup + 3;
            while (slide.IsSliding && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(m_Player.position.x, Is.GreaterThanOrEqualTo(previousX - .03f), "Reverse input must not climb the one-way slope.");
                previousX = m_Player.position.x;
            }
            Keys();
            yield return Settle(-9.4f);
            Assert.That(m_Level.Player.GetComponent<PlayerInputReader>().GameplayBlocked, Is.False);
            yield return Capture("06-slide-landing.png");
            for (int attempt = 0; attempt < 3; attempt++)
            {
                Keys(Key.A, Key.Space, Key.LeftShift);
                yield return null;
                yield return null;
                Keys(Key.A);
                yield return new WaitForSecondsRealtime(.7f);
                Assert.That(m_Player.position.x, Is.GreaterThan(50.5f), "A new jump/dodge from the bottom must not return to the chamber.");
            }
            Keys();
            deadline = Time.realtimeSinceStartup + 3;
            while (slide.IsSliding && Time.realtimeSinceStartup < deadline) yield return null;
            yield return Settle(-9.4f);
            Assert.That(m_Level.IsFailed, Is.False);
        }
    }
}
#endif
