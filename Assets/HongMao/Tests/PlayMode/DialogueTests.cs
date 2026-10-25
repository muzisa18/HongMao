#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HongMao.Tests
{
    public sealed class DialogueTests
    {
        DialogueController m_Dialogue;
        PrototypeInstaller m_Installer;
        PlayerInputReader m_Input;
        Rigidbody2D m_Player;
        Keyboard m_Keyboard;
        Mouse m_Mouse;
        InputSettings.EditorInputBehaviorInPlayMode m_EditorInputBehavior;
        InputSettings.BackgroundBehavior m_BackgroundBehavior;


        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("BattleTestRoom");
            yield return null;
            m_Installer = Object.FindFirstObjectByType<PrototypeInstaller>();
            m_Dialogue = Object.FindFirstObjectByType<DialogueController>();
            m_Input = m_Installer.Player.GetComponent<PlayerInputReader>();
            m_Player = m_Installer.Player.GetComponent<Rigidbody2D>();
            // Batch-mode has no focused Game view; route simulated devices to the game.
            m_EditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            m_BackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            m_Keyboard = InputSystem.AddDevice<Keyboard>();
            m_Mouse = InputSystem.AddDevice<Mouse>();
        }

        [UnityTearDown]
        public IEnumerator CleanupScene()
        {
            if (m_Dialogue != null) m_Dialogue.Close();
            if (m_Keyboard != null) InputSystem.RemoveDevice(m_Keyboard);
            if (m_Mouse != null) InputSystem.RemoveDevice(m_Mouse);
            InputSystem.settings.editorInputBehaviorInPlayMode = m_EditorInputBehavior;
            InputSystem.settings.backgroundBehavior = m_BackgroundBehavior;
            yield return null;
        }

        IEnumerator ReachGuide()
        {
            m_Player.position = new Vector2(26.8f, 6.7f);
            m_Player.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            Assert.That(m_Installer.Player.GetComponent<PlayerMotor2D>().IsGrounded, Is.True);
            Assert.That(m_Dialogue.CanStart, Is.True);
        }

        IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(m_Keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(m_Keyboard, new KeyboardState());
            yield return null;
            yield return null;
        }

        IEnumerator CapturePreview(string fileName)
        {
            string directory = System.Environment.GetEnvironmentVariable("HONGMAO_DIALOGUE_PREVIEW_DIR");
            if (string.IsNullOrEmpty(directory)) yield break;
            System.IO.Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(.6f);
            string path = System.IO.Path.Combine(directory, fileName);
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(System.IO.File.Exists(path), Is.True, "Requested preview was not captured.");
        }

        [UnityTest]
        public IEnumerator Guide_IsNonCombatantAndOnlyReachableOnUpperStep()
        {
            Assert.That(m_Dialogue, Is.Not.Null);
            Assert.That(m_Dialogue.Npc.GetComponentsInChildren<Collider2D>(), Is.Empty);
            Assert.That(m_Dialogue.Npc.GetComponentsInChildren<CombatantBody>(), Is.Empty);
            Assert.That(Object.FindObjectsByType<PlayerCombatController>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<CombatBotController>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            yield return Press(Key.E);
            Assert.That(m_Dialogue.IsOpen, Is.False, "Spawn is out of range.");
            m_Player.position = new Vector2(28, -1.75f);
            Physics2D.SyncTransforms();
            Assert.That(m_Dialogue.TryStart(), Is.False, "Must not talk from the ground below.");
            yield return ReachGuide();
            yield return CapturePreview("npc-prompt.png");
            yield return Press(Key.E);
            Assert.That(m_Dialogue.IsOpen, Is.True);
            Assert.That(m_Dialogue.LineIndex, Is.Zero, "Opening E must not skip greeting.");
            Assert.That(m_Dialogue.CurrentLine, Does.Contain("你好"));
            yield return CapturePreview("npc-dialogue.png");
        }

        [UnityTest]
        public IEnumerator Dialogue_BlocksGameplayAndRestoresOnEscapeAndCompletion()
        {
            yield return ReachGuide();
            yield return Press(Key.E);
            Assert.That(Time.timeScale, Is.Zero);
            Vector2 playerPosition = m_Player.position;
            Vector3 enemyPosition = m_Installer.Enemy.transform.position;
            InputSystem.QueueStateEvent(m_Keyboard, new KeyboardState(Key.D, Key.Space, Key.LeftShift, Key.Q, Key.R));
            InputSystem.QueueStateEvent(m_Mouse, new MouseState().WithButton(MouseButton.Left).WithButton(MouseButton.Right));
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(m_Input.Horizontal, Is.Zero);
            Assert.That(m_Input.AttackPressed || m_Input.JumpPressed || m_Input.DodgePressed || m_Input.ParryPressed || m_Input.AbsorbPressed || m_Input.MaskPressed, Is.False);
            Assert.That(m_Installer.Player.IsActionBusy, Is.False);
            Assert.That(m_Player.position, Is.EqualTo(playerPosition));
            Assert.That(m_Installer.Enemy.transform.position, Is.EqualTo(enemyPosition));
            InputSystem.QueueStateEvent(m_Mouse, new MouseState());
            yield return Press(Key.Escape);
            Assert.That(m_Dialogue.IsOpen, Is.False);
            Assert.That(m_Input.GameplayBlocked, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            yield return Press(Key.E);
            Assert.That(m_Dialogue.LineIndex, Is.Zero);
            int count = m_Dialogue.LineCount;
            for (int i = 1; i < count; i++)
            {
                yield return Press(Key.E);
                Assert.That(m_Dialogue.LineIndex, Is.EqualTo(i));
            }
            yield return Press(Key.E);
            Assert.That(m_Dialogue.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            InputSystem.QueueStateEvent(m_Keyboard, new KeyboardState(Key.A));
            yield return new WaitForSeconds(.1f);
            Assert.That(m_Player.position.x, Is.LessThan(playerPosition.x - .1f), "Movement must resume.");
        }

        [UnityTest]
        public IEnumerator Dialogue_HitStopDisableAndRestartCannotLeavePauseOrInputLocked()
        {
            yield return ReachGuide();
            Assert.That(m_Dialogue.TryStart(), Is.True);
            var time = Object.FindFirstObjectByType<CombatTimeController>();
            time.HitStop(.04f);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(Time.timeScale, Is.Zero, "Hit-stop completion must preserve dialogue pause.");
            m_Dialogue.Npc.gameObject.SetActive(false);
            yield return null;
            yield return null;
            Assert.That(m_Dialogue.IsOpen, Is.False);
            Assert.That(m_Input.GameplayBlocked, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            m_Dialogue.Npc.gameObject.SetActive(true);
            Assert.That(m_Dialogue.TryStart(), Is.True);
            m_Dialogue.enabled = false;
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(m_Input.GameplayBlocked, Is.False);
            m_Dialogue.enabled = true;
            Assert.That(m_Dialogue.TryStart(), Is.True);
            var view = Object.FindFirstObjectByType<DialogueView>();
            view.enabled = false;
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(m_Input.GameplayBlocked, Is.False);
            Assert.That(m_Dialogue.TryStart(), Is.False, "A hidden view must not allow an invisible paused conversation.");
            view.enabled = true;
            Assert.That(m_Dialogue.TryStart(), Is.True);
            m_Installer.Flow.Restart();
            yield return null;
            yield return null;
            var reloaded = Object.FindFirstObjectByType<DialogueController>();
            Assert.That(reloaded, Is.Not.Null);
            Assert.That(reloaded.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(Object.FindFirstObjectByType<PlayerInputReader>().GameplayBlocked, Is.False);
        }

        [UnityTest]
        public IEnumerator Dialogue_CannotStartDuringAbsorbOrAfterVictory()
        {
            yield return ReachGuide();
            m_Installer.Resources.TryAddCharge(3);
            yield return Press(Key.Q);
            Assert.That(m_Installer.Player.IsAbsorbing, Is.True);
            yield return Press(Key.E);
            Assert.That(m_Dialogue.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(.1f).Within(.001f));
            yield return Press(Key.Escape);
            Assert.That(m_Dialogue.TryStart(), Is.True);
            m_Installer.Enemy.Body.ReceiveHit(new HitRequest(999, 9999, 9999, Vector2.zero, false, false, m_Installer.Player.gameObject));
            yield return null;
            yield return null;
            Assert.That(m_Installer.Flow.State, Is.EqualTo(GameFlowState.Victory));
            Assert.That(m_Dialogue.IsOpen, Is.False);
            Assert.That(m_Input.GameplayBlocked, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(m_Dialogue.TryStart(), Is.False);
        }
    }
}
#endif
