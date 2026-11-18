using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MatriculaDeHonor.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MatriculaDeHonor.Tests.PlayMode
{
    /// <summary>
    /// Montaje común de los tests PlayMode del jugador: frame fijo de 1/60 s, un PlayerController
    /// real con <see cref="ScriptedInputSource"/> y limpieza de todo lo creado al terminar.
    /// </summary>
    public abstract class PlayerTestBase
    {
        protected const float FrameTime = 1f / 60f;

        private readonly List<Object> created = new List<Object>();

        protected PlayerConfig config;
        protected PlayerController player;
        protected ScriptedInputSource input;

        [SetUp]
        public void SetUpPlayerTest()
        {
            Time.captureDeltaTime = FrameTime;
            config = ScriptableObject.CreateInstance<PlayerConfig>();
            created.Add(config);
        }

        [UnityTearDown]
        public IEnumerator TearDownPlayerTest()
        {
            Time.captureDeltaTime = 0f;
            foreach (var item in created)
            {
                if (item != null) Object.Destroy(item);
            }

            created.Clear();
            yield return null;
        }

        /// <summary>Lo que se cree a mano en el test y haya que destruir al final.</summary>
        protected T Track<T>(T item) where T : Object
        {
            created.Add(item);
            return item;
        }

        protected IEnumerator FloorAndPlayer()
        {
            CreateBox("Suelo", new Vector3(0f, -0.5f, 0f), new Vector3(60f, 1f, 60f));
            yield return SpawnPlayer(Vector3.zero);
        }

        protected IEnumerator SpawnPlayer(Vector3 feetPosition, bool waitForGround = true)
        {
            var root = Track(new GameObject("Estudiante (test)"));
            root.SetActive(false); // así Awake corre después de configurarlo
            root.transform.position = feetPosition + Vector3.up * 0.05f;

            var body = root.AddComponent<CharacterController>();
            body.height = 1.8f;
            body.radius = 0.35f;
            body.center = new Vector3(0f, 0.9f, 0f);

            input = root.AddComponent<ScriptedInputSource>();
            player = root.AddComponent<PlayerController>();
            SetPrivateField(player, "config", config);
            SetPrivateField(player, "inputSourceBehaviour", input);
            SetPrivateField(player, "isMainPlayer", false); // sin eventos globales ni ServiceLocator

            root.SetActive(true);
            if (waitForGround)
            {
                yield return WaitUntil(() => player.Motor.IsGrounded, 60, "El jugador no llega a tocar el suelo.");
            }
        }

        protected GameObject CreateBox(string name, Vector3 center, Vector3 size)
        {
            var box = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            box.name = name;
            box.transform.position = center;
            box.transform.localScale = size;
            return box;
        }

        /// <summary>Altura máxima (sobre <paramref name="startY"/>) que alcanza en los próximos frames.</summary>
        protected IEnumerator TrackApex(float startY, int frames, Action<float> result)
        {
            float apex = float.NegativeInfinity;
            for (int i = 0; i < frames; i++)
            {
                yield return null;
                apex = Mathf.Max(apex, player.transform.position.y - startY);
            }

            result(apex);
        }

        protected static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        protected static IEnumerator Seconds(float seconds)
        {
            yield return Frames(Mathf.CeilToInt(seconds / FrameTime));
        }

        protected static IEnumerator WaitUntil(Func<bool> condition, int maxFrames, string failMessage)
        {
            for (int i = 0; i < maxFrames && !condition(); i++) yield return null;
            Assert.That(condition(), failMessage);
        }

        protected static void SetPrivateField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            FieldInfo field = null;
            while (field == null && type != null)
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }

            Assert.That(field, Is.Not.Null, $"{target.GetType().Name} ya no tiene el campo '{fieldName}'.");
            field.SetValue(target, value);
        }
    }
}
