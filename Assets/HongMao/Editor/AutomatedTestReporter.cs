using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace HongMao.Editor
{
    /// <summary>
    /// Persists automated test results across the domain reload used by PlayMode tests.
    /// This editor-only helper is also useful for CI and future Codex validation runs.
    /// </summary>
    [InitializeOnLoad]
    public sealed class AutomatedTestReporter : ScriptableObject, ICallbacks
    {
        const string ResultPathKey = "HongMao.AutomatedTestReporter.ResultPath";

        static AutomatedTestReporter()
        {
            var reporter = CreateInstance<AutomatedTestReporter>();
            reporter.hideFlags = HideFlags.HideAndDontSave;
            TestRunnerApi.RegisterTestCallback(reporter, 100);
        }

        public static void Run(TestMode mode, string resultPath)
        {
            if (string.IsNullOrWhiteSpace(resultPath))
                throw new System.ArgumentException("A result path is required.", nameof(resultPath));

            EditorPrefs.SetString(ResultPathKey, resultPath);
            var api = CreateInstance<TestRunnerApi>();
            api.Execute(new ExecutionSettings(new Filter { testMode = mode }));
            Debug.Log($"HongMao {mode} test run started; results will be written to {resultPath}.");
        }

        public void RunStarted(ITestAdaptor testsToRun) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            string path = EditorPrefs.GetString(ResultPathKey, string.Empty);
            if (string.IsNullOrEmpty(path)) return;

            TestRunnerApi.SaveResultToFile(result, path);
            EditorPrefs.DeleteKey(ResultPathKey);
            Debug.Log($"HongMao tests finished: {result.TestStatus}; passed {result.PassCount}, failed {result.FailCount}, skipped {result.SkipCount}. Results: {path}");
        }

        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
    }
}
