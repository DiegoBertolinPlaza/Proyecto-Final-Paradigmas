using System.Collections;
using MatriculaDeHonor.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MatriculaDeHonor.Tests.PlayMode
{
    /// <summary>
    /// P1.1 · Correr y saltar con el PlayerController de verdad (máquina de estados + CharacterController).
    /// El tiempo de frame es fijo (1/60 s) para que los resultados no dependan del PC.
    /// </summary>
    public sealed class PlayerMovementTests : PlayerTestBase
    {
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
    }
}
