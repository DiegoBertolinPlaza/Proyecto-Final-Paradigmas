using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace MatriculaDeHonor.IODP.Editor
{
    /// <summary>
    /// Automatización local del editor: solo cuatro acciones fijas, sin ejecutar código ni comandos
    /// recibidos. Permite generar y verificar dentro del editor cuando el modo batch se bloquea.
    /// Petición explícita en Logs/iodp/authoring-request.json; resultado en authoring-response.json.
    /// </summary>
    [InitializeOnLoad]
    public static class CampusAuthoringRequests
    {
        [Serializable] private sealed class Request { public string id; public string action; }
        [Serializable] private sealed class Response { public string id; public string action; public bool success; public string message; }
        private const string Folder = "Logs/iodp";
        private const string PendingId = "IODP.Authoring.PendingId";
        private const string PendingAction = "IODP.Authoring.PendingAction";
        private static double nextCheck;
        private static bool busy;
        private static readonly TestRunnerApi Runner;

        static CampusAuthoringRequests()
        {
            Runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            Runner.RegisterCallbacks(new Callbacks());
            EditorApplication.update += CheckRequest;
        }

        private static void CheckRequest()
        {
            if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1;
            string path = Folder + "/authoring-request.json";
            if (!File.Exists(path)) return;
            Request request;
            try { request = JsonUtility.FromJson<Request>(File.ReadAllText(path)); }
            catch (IOException) { return; } // El escritor puede estar terminando el archivo.
            if (request == null || string.IsNullOrWhiteSpace(request.id) ||
                request.id == SessionState.GetString("IODP.Authoring.LastId", "")) return;
            SessionState.SetString("IODP.Authoring.LastId", request.id);
            busy = true;
            try
            {
                switch (request.action)
                {
                    case "rebuild":
                        CampusSceneGenerator.RebuildGeneratedScenesForValidation();
                        Finish(request.id, request.action, true, "Diez escenas y selector regenerados.");
                        break;
                    case "previews":
                        CampusSceneGenerator.ExportPreviews();
                        Finish(request.id, request.action, true, "Diez vistas Unity exportadas.");
                        break;
                    case "editmode":
                    case "playmode":
                        SessionState.SetString(PendingId, request.id);
                        SessionState.SetString(PendingAction, request.action);
                        Runner.Execute(new ExecutionSettings(new Filter {
                            testMode = request.action == "editmode" ? TestMode.EditMode : TestMode.PlayMode
                        }));
                        break;
                    default:
                        Finish(request.id, request.action, false, "Acción desconocida. Permitidas: rebuild, previews, editmode, playmode.");
                        break;
                }
            }
            catch (Exception exception)
            {
                Finish(request.id, request.action, false, exception.ToString());
                Debug.LogException(exception);
            }
            finally { busy = false; }
        }

        private static void Finish(string id, string action, bool success, string message)
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder + "/authoring-response.json", JsonUtility.ToJson(
                new Response { id = id, action = action, success = success, message = message }, true));
            Debug.Log($"[IODP] {action}: {message}");
        }

        private sealed class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                string id = SessionState.GetString(PendingId, "");
                if (string.IsNullOrEmpty(id)) return;
                string action = SessionState.GetString(PendingAction, "tests");
                SessionState.EraseString(PendingId);
                SessionState.EraseString(PendingAction);
                TestRunnerApi.SaveResultToFile(result, Folder + "/" + action + ".xml");
                Finish(id, action, result.FailCount == 0 && result.PassCount > 0,
                    $"{result.PassCount} aprobados, {result.FailCount} fallidos, {result.SkipCount} omitidos. {result.ResultState}");
            }
        }
    }
}
