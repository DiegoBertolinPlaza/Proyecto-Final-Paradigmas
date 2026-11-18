using System.Collections;
using MatriculaDeHonor.Combat;
using MatriculaDeHonor.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MatriculaDeHonor.Tests.PlayMode
{
    /// <summary>P1.3 · Daño y muerte: retroceso, invulnerabilidad breve, chuleta y 3 golpes = muerte.</summary>
    public sealed class PlayerDamageTests : PlayerTestBase
    {
        /// <summary>El golpe viene de detrás (z negativa): el retroceso debe empujar hacia z positiva.</summary>
        private static readonly Vector3 HitFrom = new Vector3(0f, 0f, -2f);

        [UnityTest]
        public IEnumerator UnGolpeQuitaUnaVidaYTeEmpujaLejosDelGolpe()
        {
            yield return FloorAndPlayer();

            Hit();
            yield return null;

            Assert.That(player.Health.Current, Is.EqualTo(config.maxHealth - 1));
            Assert.That(player.CurrentStateName, Is.EqualTo("Hurt"));
            Assert.That(player.Motor.HorizontalVelocity.z, Is.GreaterThan(config.knockbackSpeed * 0.8f));
            Assert.That(player.Motor.VerticalVelocity, Is.GreaterThan(0f), "El golpe da un saltito.");
        }

        [UnityTest]
        public IEnumerator TrasElGolpeVuelveATenerElControl()
        {
            yield return FloorAndPlayer();

            Hit();
            yield return Seconds(config.hurtDuration);
            yield return WaitUntil(() => player.CurrentStateName == "Idle" || player.CurrentStateName == "Run", 60,
                $"Sigue en {player.CurrentStateName} tras el golpe.");

            input.Move = Vector2.right;
            yield return Frames(30);
            Assert.That(player.Motor.HorizontalVelocity.x, Is.GreaterThan(0.5f));
        }

        [UnityTest]
        public IEnumerator DuranteLaInvulnerabilidadNoRecibeMasDano()
        {
            yield return FloorAndPlayer();

            Hit();
            yield return Frames(10);
            Hit();
            Assert.That(player.Health.Current, Is.EqualTo(config.maxHealth - 1), "El segundo golpe no debería contar.");
            Assert.That(player.Health.IsInvulnerable, Is.True);

            yield return Seconds(config.invulnerabilityTime);
            Assert.That(player.Health.IsInvulnerable, Is.False);
            Hit();
            Assert.That(player.Health.Current, Is.EqualTo(config.maxHealth - 2));
        }

        [UnityTest]
        public IEnumerator TresGolpesLeMatanYYaNoSeMueve()
        {
            Assert.That(config.maxHealth, Is.EqualTo(3), "El GDD dice 3 golpes.");
            yield return FloorAndPlayer();

            int deaths = 0;
            player.Died += _ => deaths++;

            for (int i = 0; i < 3; i++)
            {
                Hit();
                yield return Seconds(config.invulnerabilityTime + 0.05f);
            }

            Assert.That(player.Health.IsDead, Is.True);
            Assert.That(player.IsAlive, Is.False);
            Assert.That(player.CurrentStateName, Is.EqualTo("Dead"));
            Assert.That(deaths, Is.EqualTo(1));

            Vector3 before = player.transform.position;
            input.Move = Vector2.up;
            input.PressJump();
            yield return Frames(30);
            Assert.That(Vector3.Distance(before, player.transform.position), Is.LessThan(0.05f), "Muerto no se mueve.");

            Hit();
            Assert.That(deaths, Is.EqualTo(1), "No se muere dos veces.");
        }

        [UnityTest]
        public IEnumerator LaChuletaParaUnGolpe()
        {
            yield return FloorAndPlayer();
            player.Health.AddShield(1);

            Hit();
            yield return null;

            Assert.That(player.Health.Current, Is.EqualTo(config.maxHealth));
            Assert.That(player.Health.Shield, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CaerAlVacioMataAunqueSeaInvulnerable()
        {
            yield return FloorAndPlayer();

            Hit();
            yield return Frames(5);
            Assert.That(player.Health.IsInvulnerable, Is.True);

            player.Kill();
            yield return null;

            Assert.That(player.CurrentStateName, Is.EqualTo("Dead"));
        }

        [UnityTest]
        public IEnumerator ElDanoDeOtroEstudianteNoCuenta()
        {
            yield return FloorAndPlayer();

            player.TakeDamage(new DamageInfo(1, DamageKind.Contact, Faction.Student, HitFrom, null));
            yield return null;

            Assert.That(player.Health.Current, Is.EqualTo(config.maxHealth));
            Assert.That(player.CurrentStateName, Is.EqualTo("Idle"));
        }

        [UnityTest]
        public IEnumerator AlReaparecerVuelveConTodaLaVida()
        {
            yield return FloorAndPlayer();
            player.Kill();
            yield return Frames(5);

            player.Respawn(new Vector3(3f, 0.05f, 0f), Quaternion.identity);
            yield return WaitUntil(() => player.CurrentStateName == "Idle", 60, "No vuelve a estar de pie.");

            Assert.That(player.Health.Current, Is.EqualTo(config.maxHealth));
            Assert.That(player.Health.IsInvulnerable, Is.False);
            Assert.That(player.transform.position.x, Is.EqualTo(3f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator ParpadeaMientrasEsInvulnerableYLuegoSeVeNormal()
        {
            yield return FloorAndPlayer();

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(player.transform, false);
            var body = visual.GetComponent<Renderer>();
            var feedback = player.gameObject.AddComponent<PlayerDamageFeedback>();

            Hit();
            Assert.That(feedback.IsBlinking, Is.True);

            int hiddenFrames = 0;
            int shownFrames = 0;
            for (int i = 0; i < 60; i++)
            {
                yield return null;
                if (body.enabled) shownFrames++;
                else hiddenFrames++;
            }

            Assert.That(hiddenFrames, Is.GreaterThan(10), "Debería ocultarse a ratos.");
            Assert.That(shownFrames, Is.GreaterThan(10), "Debería volver a verse a ratos.");

            yield return Seconds(config.invulnerabilityTime);
            Assert.That(feedback.IsBlinking, Is.False);
            Assert.That(body.enabled, Is.True, "Al acabar la invulnerabilidad tiene que verse.");
        }

        private void Hit()
        {
            player.TakeDamage(new DamageInfo(1, DamageKind.Contact, Faction.Professor, HitFrom, null));
        }
    }
}
