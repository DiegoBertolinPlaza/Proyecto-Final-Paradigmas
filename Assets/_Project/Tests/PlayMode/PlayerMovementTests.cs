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
    /// P1.1 · Correr y saltar con el PlayerController de verdad (máquina de estados + CharacterController).
    /// El tiempo de frame es fijo (1/60 s) para que los resultados no dependan del PC.
    /// </summary>
    public sealed class PlayerMovementTests
    {
        private const float FrameTime = 1f / 60f;

        private readonly List<Object> created = new List<Object>();
        private PlayerConfig config;
        private PlayerController player;
        private ScriptedInputSource input;

        [SetUp]
        public void SetUp()
        {
            Time.captureDeltaTime = FrameTime;
            config = ScriptableObject.CreateInstance<PlayerConfig>();
            created.Add(config);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.captureDeltaTime = 0f;
            foreach (var item in created)
            {
                if (item != null) Object.Destroy(item);
            }

            created.Clear();
            yield return null;
        }

        // ---------- Correr ----------

        [UnityTest]
        public IEnumerator CorriendoLlegaALaVelocidadDeCarreraYMiraHaciaDondeVa()
        {
            yield return FloorAndPlayer();

            input.Move = Vector2.up;
            input.Run = true;
            yield return Frames(60);

            Assert.That(player.Motor.HorizontalVelocity.magnitude, Is.EqualTo(config.runSpeed).Within(0.05f));
            Assert.That(player.transform.forward.z, Is.GreaterThan(0.95f));
            Assert.That(player.CurrentStateName, Is.EqualTo("Run"));
        }

        [UnityTest]
        public IEnumerator SinCorrerVaAVelocidadDePaseo()
        {
            yield return FloorAndPlayer();

            input.Move = Vector2.right;
            yield return Frames(60);

            Assert.That(player.Motor.HorizontalVelocity.magnitude, Is.EqualTo(config.walkSpeed).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator AlSoltarElStickSeFrenaEnPocoTiempo()
        {
            yield return FloorAndPlayer();

            input.Move = Vector2.up;
            input.Run = true;
            yield return Frames(45);

            input.Move = Vector2.zero;
            yield return Frames(15); // 0.25 s

            Assert.That(player.Motor.HorizontalVelocity.magnitude, Is.LessThan(0.01f));
            Assert.That(player.CurrentStateName, Is.EqualTo("Idle"));
        }

        // ---------- Saltar ----------

        [UnityTest]
        public IEnumerator ManteniendoSaltoLlegaALaAlturaConfigurada()
        {
            yield return FloorAndPlayer();
            float startY = player.transform.position.y;

            input.PressJump(hold: true);
            float apex = 0f;
            yield return TrackApex(startY, 90, height => apex = height);

            // La integración por frames pierde unos centímetros respecto a la fórmula exacta.
            Assert.That(apex, Is.EqualTo(config.jumpHeight).Within(0.2f));
        }

        [UnityTest]
        public IEnumerator SoltandoSaltoEnseguidaElSaltoEsMasBajo()
        {
            yield return FloorAndPlayer();
            float startY = player.transform.position.y;

            input.PressJump(hold: false);
            float apex = 0f;
            yield return TrackApex(startY, 90, height => apex = height);

            Assert.That(apex, Is.InRange(0.3f, config.jumpHeight * 0.6f));
        }

        [UnityTest]
        public IEnumerator DespuesDeSaltarVuelveAlSuelo()
        {
            yield return FloorAndPlayer();

            input.PressJump();
            yield return Frames(5);
            Assert.That(player.CurrentStateName, Is.EqualTo("Jump"));

            input.JumpHeld = false;
            yield return WaitUntil(() => player.Motor.IsGrounded, 120, "No vuelve a aterrizar.");

            // El motor se mueve después de que decidan los estados: el aterrizaje se ve un frame más tarde.
            yield return null;
            Assert.That(player.CurrentStateName, Is.EqualTo("Idle").Or.EqualTo("Run"));
        }

        // ---------- Asistencias: coyote time y buffer de salto ----------

        [UnityTest]
        public IEnumerator CoyoteTimePermiteSaltarJustoDespuesDeSalirDelBorde()
        {
            yield return LedgeAndPlayer();
            yield return WalkOffLedge();

            yield return Frames(3); // 0.05 s en el aire, menos que coyoteTime (0.12 s)
            input.PressJump();
            yield return null;

            Assert.That(player.Motor.VerticalVelocity, Is.GreaterThan(0f), "Con coyote time debería saltar.");
        }

        [UnityTest]
        public IEnumerator PasadoElCoyoteTimeYaNoSePuedeSaltarEnElAire()
        {
            yield return LedgeAndPlayer();
            yield return WalkOffLedge();

            yield return Frames(20); // 0.33 s en el aire
            input.PressJump();
            yield return null;

            Assert.That(player.Motor.VerticalVelocity, Is.LessThan(0f));
            Assert.That(player.CurrentStateName, Is.EqualTo("Fall"));
        }

        [UnityTest]
        public IEnumerator ElBufferGuardaElSaltoPulsadoJustoAntesDeAterrizar()
        {
            yield return FloorAndPlayer();
            player.Respawn(new Vector3(0f, 2.5f, 0f), Quaternion.identity);

            // Pulsa cuando le faltan unos centímetros para tocar el suelo (antes de aterrizar).
            yield return WaitUntil(() => player.transform.position.y < 0.4f, 120, "No llega a caer.");
            Assert.That(player.Motor.IsGrounded, Is.False);
            input.PressJump();

            bool jumped = false;
            for (int i = 0; i < 15 && !jumped; i++)
            {
                yield return null;
                jumped = player.Motor.VerticalVelocity > 0f;
            }

            Assert.That(jumped, "El salto pulsado antes de aterrizar debería ejecutarse al tocar el suelo.");
        }

        // ---------- Montaje ----------

        private IEnumerator FloorAndPlayer()
        {
            CreateBox("Suelo", new Vector3(0f, -0.5f, 0f), new Vector3(60f, 1f, 60f));
            yield return SpawnPlayer(Vector3.zero);
        }

        /// <summary>Plataforma que termina en z = 0; más allá, vacío.</summary>
        private IEnumerator LedgeAndPlayer()
        {
            CreateBox("Plataforma", new Vector3(0f, -0.5f, -5f), new Vector3(4f, 1f, 10f));
            yield return SpawnPlayer(new Vector3(0f, 0f, -1.5f));
        }

        private IEnumerator WalkOffLedge()
        {
            input.Move = Vector2.up;
            yield return WaitUntil(() => !player.Motor.IsGrounded, 120, "No llega a salir del borde.");
            Assert.That(player.Motor.VerticalVelocity, Is.LessThanOrEqualTo(0f), "Se ha caído, no ha saltado.");
        }

        private IEnumerator SpawnPlayer(Vector3 feetPosition)
        {
            var root = new GameObject("Estudiante (test)");
            root.SetActive(false); // así Awake corre después de configurarlo
            root.transform.position = feetPosition + Vector3.up * 0.05f;
            created.Add(root);

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
            yield return WaitUntil(() => player.Motor.IsGrounded, 60, "El jugador no llega a tocar el suelo.");
        }

        private void CreateBox(string name, Vector3 center, Vector3 size)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = center;
            box.transform.localScale = size;
            created.Add(box);
        }

        private IEnumerator TrackApex(float startY, int frames, Action<float> result)
        {
            float apex = 0f;
            for (int i = 0; i < frames; i++)
            {
                yield return null;
                apex = Mathf.Max(apex, player.transform.position.y - startY);
            }

            result(apex);
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        private static IEnumerator WaitUntil(Func<bool> condition, int maxFrames, string failMessage)
        {
            for (int i = 0; i < maxFrames && !condition(); i++) yield return null;
            Assert.That(condition(), failMessage);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"{target.GetType().Name} ya no tiene el campo '{fieldName}'.");
            field.SetValue(target, value);
        }
    }
}
