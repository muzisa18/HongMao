using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HongMao.Tests
{
    public sealed class PrototypeSceneTests
    {
        [UnityTest]
        public IEnumerator Installer_CreatesRequiredCombatReferences()
        {
            var root = new GameObject("TestInstaller");
            var installer = root.AddComponent<PrototypeInstaller>();
            yield return null;
            Assert.That(installer.Player, Is.Not.Null);
            Assert.That(installer.Enemy, Is.Not.Null);
            Assert.That(installer.Resources, Is.Not.Null);
            Assert.That(installer.Flow, Is.Not.Null);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator DisablingTimeController_RestoresTimeScale()
        {
            var root = new GameObject("TimeControllerTest");
            var controller = root.AddComponent<CombatTimeController>();
            controller.EnterAbsorb();
            Assert.That(Time.timeScale, Is.EqualTo(0.1f).Within(0.001f));
            controller.enabled = false;
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.001f));
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator RedSource_TurnsUnavailableAfterExtraction()
        {
            var sourceObject = new GameObject("SourceTest");
            sourceObject.AddComponent<SpriteRenderer>().sprite = PrototypeVisuals.WhiteSprite;
            sourceObject.AddComponent<BoxCollider2D>().isTrigger = true;
            var source = sourceObject.AddComponent<RedColorSource>();
            source.Configure();
            Assert.That(source.TryExtractColor(out ColorKind color), Is.True);
            Assert.That(color, Is.EqualTo(ColorKind.Red));
            Assert.That(source.IsAvailable, Is.False);
            Object.Destroy(sourceObject);
            yield return null;
        }
    }
}
