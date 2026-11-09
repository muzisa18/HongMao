#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HongMao.Tests
{
    public sealed class PlayerLocomotionSpriteTests
    {
        const string ArtPath = "Assets/HongMao/Art/鸿毛精灵图/";
        const string IdlePath = ArtPath + "MA01_待機.png";
        const string LeftPath = ArtPath + "MA01_左移動.png";
        const string RightPath = ArtPath + "MA01_右移動 .png";
        const string LeftJumpPath = ArtPath + "MA01_左跳躍.png";
        const string RightJumpPath = ArtPath + "MA01_右跳躍.png";
        const string LeftFallPath = ArtPath + "MA01_左下落.png";
        const string RightFallPath = ArtPath + "MA01_右下落.png";
        const string RedMaskIdlePath = ArtPath + "关羽脸谱_待機.png";
        SpriteRenderer m_Renderer;
        Transform m_Visual;
        PlayerMotor2D m_Motor;
        Rigidbody2D m_Body;
        PlayerCombatController m_Combat;
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
            yield return SceneManager.LoadSceneAsync("PrototypeCombat");
            yield return null;
            var installer = Object.FindFirstObjectByType<PrototypeInstaller>();
            installer.Enemy.enabled = false;
            m_Motor = installer.Player.GetComponent<PlayerMotor2D>();
            m_Body = installer.Player.GetComponent<Rigidbody2D>();
            m_Combat = installer.Player;
            m_Visual = installer.Player.transform.Find("Visual");
            m_Renderer = m_Visual.Find("Sprite Animation").GetComponent<SpriteRenderer>();
            yield return new WaitForSeconds(.25f);
            Assert.That(m_Motor.IsGrounded, Is.True);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (m_Keyboard != null) InputSystem.RemoveDevice(m_Keyboard);
            if (m_Mouse != null) InputSystem.RemoveDevice(m_Mouse);
            InputSystem.settings.editorInputBehaviorInPlayMode = m_EditorInputBehavior;
            InputSystem.settings.backgroundBehavior = m_BackgroundBehavior;
            Time.timeScale = 1f;
            yield return null;
        }

        void Keys(params Key[] keys) => InputSystem.QueueStateEvent(m_Keyboard, new KeyboardState(keys));

        void AssertSheet(string path) => Assert.That(AssetDatabase.GetAssetPath(m_Renderer.sprite), Is.EqualTo(path));

        [UnityTest]
        public IEnumerator Originals_ImportAllFramesAndIdleLoops()
        {
            foreach (var item in new[]
            {
                (IdlePath, 8, 8f), (LeftPath, 10, 9f), (RightPath, 10, 9f),
                (LeftJumpPath, 3, 9f), (RightJumpPath, 3, 9f),
                (LeftFallPath, 3, 9f), (RightFallPath, 3, 9f), (RedMaskIdlePath, 8, 3f)
            })
            {
                Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(item.Item1).OfType<Sprite>().ToArray();
                Assert.That(frames, Has.Length.EqualTo(item.Item2));
                foreach (Sprite frame in frames)
                {
                    Assert.That(frame.rect.size, Is.EqualTo(new Vector2(64, 64)));
                    Assert.That(frame.pixelsPerUnit, Is.EqualTo(30f));
                    Assert.That(frame.pivot, Is.EqualTo(new Vector2(32, item.Item3)));
                }
            }

            var shown = new HashSet<Sprite>();
            float deadline = Time.time + 1f;
            while (Time.time < deadline)
            {
                yield return null;
                AssertSheet(IdlePath);
                shown.Add(m_Renderer.sprite);
            }
            Assert.That(shown.Count, Is.GreaterThan(1), "Idle must animate rather than hold its first frame.");
        }

        [UnityTest]
        public IEnumerator Movement_UsesBothOriginalDirectionsAndOriginalAir_KeepingOtherActions()
        {
            Keys(Key.A);
            yield return new WaitForSeconds(.18f);
            AssertSheet(LeftPath);
            Assert.That(m_Visual.localScale.x, Is.LessThan(0));
            Assert.That(m_Renderer.flipX, Is.True, "Cancel the parent mirror for the already-left-facing drawing.");
            Assert.That(m_Visual.localScale.x * (m_Renderer.flipX ? -1f : 1f), Is.GreaterThan(0));
            Sprite firstLeftFrame = m_Renderer.sprite;
            yield return new WaitForSeconds(.15f);
            Assert.That(m_Renderer.sprite, Is.Not.EqualTo(firstLeftFrame));

            Keys();
            yield return new WaitForSeconds(.12f);
            AssertSheet(IdlePath);
            Assert.That(m_Renderer.flipX, Is.False);
            Keys(Key.D);
            yield return new WaitForSeconds(.18f);
            AssertSheet(RightPath);
            Assert.That(m_Renderer.flipX, Is.False);
            Assert.That(m_Visual.localScale.x, Is.GreaterThan(0));

            Keys(Key.Space);
            yield return new WaitForSeconds(.08f);
            Assert.That(m_Motor.IsGrounded, Is.False);
            AssertSheet(RightJumpPath);
            Keys();
            float deadline = Time.time + 2f;
            bool sawFall = false;
            while (!m_Motor.IsGrounded && Time.time < deadline)
            {
                yield return null;
                if (AssetDatabase.GetAssetPath(m_Renderer.sprite) == RightFallPath) sawFall = true;
            }
            yield return null;
            Assert.That(m_Motor.IsGrounded, Is.True);
            Assert.That(sawFall, Is.True, "Descent must switch to the original falling sheet.");
            AssertSheet(IdlePath);

            Keys(Key.LeftShift);
            yield return new WaitForSeconds(.08f);
            Assert.That(m_Motor.IsDodging, Is.True);
            Assert.That(AssetDatabase.GetAssetPath(m_Renderer.sprite), Does.EndWith("PlayerGroundOneSheet.png"));
            Keys();
            yield return new WaitForSeconds(.4f);
            AssertSheet(IdlePath);
        }

        [UnityTest]
        public IEnumerator Airborne_PlaysJumpOnce_ChangesDirectionWithoutRestart_AndLoopsFall()
        {
            Vector2 groundedPosition = m_Body.position;
            // Hold ascent in a test-only zero-gravity body to observe the jump's final frame.
            m_Body.gravityScale = 0f;
            Keys(Key.A, Key.Space);
            yield return new WaitForSeconds(.35f);
            Assert.That(m_Motor.IsGrounded, Is.False);
            AssertSheet(LeftJumpPath);
            Assert.That(m_Renderer.sprite.name, Does.EndWith("_02"));
            Assert.That(m_Renderer.flipX, Is.True);

            Keys(Key.D);
            yield return new WaitForSeconds(.05f);
            AssertSheet(RightJumpPath);
            Assert.That(m_Renderer.sprite.name, Does.EndWith("_02"), "Changing direction must not restart the jump pose.");
            Assert.That(m_Renderer.flipX, Is.False);

            Keys();
            m_Body.linearVelocity = new Vector2(0f, -1f);
            yield return null;
            yield return null;
            AssertSheet(RightFallPath);
            Assert.That(m_Renderer.sprite.name, Does.EndWith("_00"), "Descent starts its own frame sequence.");
            var fallingFrames = new HashSet<Sprite>();
            float deadline = Time.time + .65f;
            while (Time.time < deadline)
            {
                yield return null;
                AssertSheet(RightFallPath);
                fallingFrames.Add(m_Renderer.sprite);
            }
            Assert.That(fallingFrames.Count, Is.EqualTo(3));

            Keys(Key.A);
            yield return new WaitForSeconds(.05f);
            AssertSheet(LeftFallPath);
            Assert.That(m_Renderer.flipX, Is.True);
            Assert.That(m_Visual.localScale.x, Is.LessThan(0));
            Keys();
            m_Body.position = groundedPosition;
            m_Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.08f);
            Assert.That(m_Motor.IsGrounded, Is.True);
            AssertSheet(IdlePath);
            Assert.That(m_Renderer.flipX, Is.False);

            Keys(Key.Space);
            yield return new WaitForSeconds(.04f);
            AssertSheet(LeftJumpPath);
            Assert.That(m_Renderer.sprite.name, Does.EndWith("_00"), "A new jump must reset the previous airborne timer.");
            Keys();
        }

        [UnityTest]
        public IEnumerator DropWithoutJump_UsesOriginalFall_AndAirAttackKeepsPriority()
        {
            Keys(Key.D);
            yield return new WaitForSeconds(.08f);
            Keys();
            m_Body.position += Vector2.up * 5f;
            m_Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.08f);
            Assert.That(m_Motor.IsGrounded, Is.False);
            AssertSheet(RightFallPath);

            var visual = m_Combat.GetComponent<PrototypeCharacterVisual>();
            visual.PlayAttack(AttackKind.GroundOne, .1f, .1f, .1f);
            yield return null;
            yield return null;
            Assert.That(AssetDatabase.GetAssetPath(m_Renderer.sprite), Does.EndWith("PlayerGroundOneSheet.png"));
            visual.StopAttackAnimation();
            yield return null;
            yield return null;
            AssertSheet(RightFallPath);
        }

        [UnityTest]
        public IEnumerator RedMaskIdle_UsesOriginalPalette_MovementAndJumpRecover_AndExpiryRestoresNormalIdle()
        {
            for (int i = 0; i < CombatResourceModel.MaxColorSlots; i++)
            {
                Assert.That(m_Combat.Resources.TryAddCharge(3), Is.True);
                Assert.That(m_Combat.Resources.TryCommitAbsorb(ColorKind.Red), Is.True);
            }
            Keys(Key.R);
            yield return null;
            yield return null;
            Keys();
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(m_Combat.Resources.IsMaskActive, Is.True);
            AssertSheet(RedMaskIdlePath);
            Assert.That(m_Renderer.color, Is.EqualTo(Color.white), "Preserve the original green costume and red mask palette.");
            Sprite first = m_Renderer.sprite;
            yield return new WaitForSeconds(.15f);
            Assert.That(m_Renderer.sprite, Is.Not.EqualTo(first));

            Keys(Key.A);
            yield return new WaitForSeconds(.12f);
            AssertSheet(LeftPath);
            Assert.That(m_Renderer.color, Is.Not.EqualTo(Color.white), "Unavailable mask movement art keeps the existing mask feedback.");
            Keys();
            yield return new WaitForSeconds(.12f);
            AssertSheet(RedMaskIdlePath);
            Assert.That(m_Renderer.color, Is.EqualTo(Color.white));
            Assert.That(m_Renderer.flipX, Is.False);
            Assert.That(m_Visual.localScale.x, Is.LessThan(0));

            Keys(Key.Space);
            yield return new WaitForSeconds(.08f);
            AssertSheet(LeftJumpPath);
            Keys();
            float deadline = Time.time + 2f;
            while (!m_Motor.IsGrounded && Time.time < deadline) yield return null;
            yield return null;
            Assert.That(m_Motor.IsGrounded, Is.True);
            AssertSheet(RedMaskIdlePath);
            Assert.That(m_Renderer.color, Is.EqualTo(Color.white));

            // Check the same-frame palette update when an action replaces the full-color idle.
            InputSystem.QueueStateEvent(m_Mouse, new MouseState().WithButton(MouseButton.Right));
            yield return null;
            yield return null;
            Assert.That(m_Combat.IsActionBusy, Is.True);
            Assert.That(m_Renderer.color, Is.Not.EqualTo(Color.white));
            InputSystem.QueueStateEvent(m_Mouse, new MouseState());
            yield return new WaitForSeconds(.9f);
            AssertSheet(RedMaskIdlePath);
            Assert.That(m_Renderer.color, Is.EqualTo(Color.white));

            m_Combat.Resources.TickMask(100f);
            yield return null;
            yield return null;
            Assert.That(m_Combat.Resources.IsMaskActive, Is.False);
            AssertSheet(IdlePath);
            Assert.That(m_Renderer.color, Is.EqualTo(Color.white));
        }

        [UnityTest]
        public IEnumerator LeftMovement_ThenAllThreeAttacks_UsesExistingArtAndReturnsToOriginalIdle()
        {
            Keys(Key.A);
            yield return new WaitForSeconds(.15f);
            AssertSheet(LeftPath);
            Keys();
            yield return new WaitForSeconds(.08f);

            foreach (string sheet in new[] { "PlayerGroundOneSheet.png", "PlayerGroundTwoSheet.png", "PlayerGroundThreeSheet.png" })
            {
                InputSystem.QueueStateEvent(m_Mouse, new MouseState().WithButton(MouseButton.Left));
                yield return new WaitForSeconds(.04f);
                Assert.That(AssetDatabase.GetAssetPath(m_Renderer.sprite), Does.EndWith(sheet));
                Assert.That(m_Visual.localScale.x, Is.LessThan(0));
                Assert.That(m_Renderer.flipX, Is.False, "Attacks must use the existing facing mirror after left movement.");
                InputSystem.QueueStateEvent(m_Mouse, new MouseState());
                yield return new WaitForSeconds(.75f);
                AssertSheet(IdlePath);
            }
        }
    }
}
#endif
