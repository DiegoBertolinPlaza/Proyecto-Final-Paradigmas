using System.Collections;
using MatriculaDeHonor.Combat;
using MatriculaDeHonor.Enemies;
using MatriculaDeHonor.Enemies.Professors;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace MatriculaDeHonor.Tests.PlayMode
{
    /// <summary>P1.2 · Pisotón y rebote: caer encima de algo pisable lo pisa y te lanza hacia arriba.</summary>
    public sealed class PlayerStompTests : PlayerTestBase
    {
        private const float TargetTop = 1f;

        private StompTarget target;

        [UnityTest]
        public IEnumerator CaerEncimaDeAlgoPisableLoPisaYRebota()
        {
            yield return FloorTargetAndFallingPlayer();
            yield return WaitForStomp();

            Assert.That(target.LastWasGroundPound, Is.False);
            Assert.That(player.CurrentStateName, Is.EqualTo("Jump"));
            Assert.That(player.Motor.VerticalVelocity, Is.GreaterThan(0f));

            float apex = 0f;
            yield return TrackApex(TargetTop, 60, height => apex = height);
            Assert.That(apex, Is.EqualTo(config.stompBounceHeight).Within(0.25f),
                "Sin mantener saltar, el rebote debe llegar a stompBounceHeight.");
        }

        [UnityTest]
        public IEnumerator ManteniendoSaltarElReboteEsMasAlto()
        {
            yield return FloorTargetAndFallingPlayer();
            input.JumpHeld = true;
            yield return WaitForStomp();

            float apex = 0f;
            yield return TrackApex(TargetTop, 90, height => apex = height);
            Assert.That(apex, Is.EqualTo(config.stompBounceHeightHeld).Within(0.3f));
        }

        [UnityTest]
        public IEnumerator SiNoAceptaElPisotonNoRebotaYSeQuedaEncima()
        {
            yield return FloorTargetAndFallingPlayer(accept: false);
            yield return WaitUntil(() => player.Motor.IsGrounded, 120, "No llega a aterrizar encima.");
            yield return Frames(2);

            Assert.That(target.Attempts, Is.GreaterThan(0), "Debería haber intentado pisarlo.");
            Assert.That(target.Accepted, Is.Zero);
            Assert.That(player.CurrentStateName, Is.EqualTo("Idle"));
            Assert.That(player.transform.position.y, Is.EqualTo(TargetTop).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator UnCuletazoEncimaLlegaComoCuletazo()
        {
            CreateBox("Suelo", new Vector3(0f, -0.5f, 0f), new Vector3(20f, 1f, 20f));
            target = CreateTarget(accept: true);
            yield return SpawnPlayer(new Vector3(0f, 4f, 0f), waitForGround: false);

            yield return Frames(5);
            input.PressGroundPound();
            yield return WaitForStomp();

            Assert.That(target.LastWasGroundPound, Is.True);
            Assert.That(player.CurrentStateName, Is.EqualTo("Jump"), "Tras el culetazo también rebota.");
        }

        [UnityTest]
        public IEnumerator ChocarDeLadoNoEsPisoton()
        {
            CreateBox("Suelo", new Vector3(0f, -0.5f, 0f), new Vector3(20f, 1f, 20f));
            target = CreateTarget(accept: true, height: 2f);
            target.transform.position = new Vector3(0f, 1f, 2f);
            yield return SpawnPlayer(Vector3.zero);

            input.Move = Vector2.up;
            yield return Frames(60);

            Assert.That(target.Attempts, Is.Zero);
            Assert.That(player.Motor.IsGrounded, Is.True);
        }

        [UnityTest]
        public IEnumerator PisarAlBedelLoDerrotaSinHacerteDano()
        {
            CreateBox("Suelo", new Vector3(0f, -0.5f, 0f), new Vector3(20f, 1f, 20f));
            var janitor = CreateJanitor(Vector3.zero);
            yield return SpawnPlayer(new Vector3(0f, 4f, 0f), waitForGround: false);

            yield return WaitUntil(() => !janitor.gameObject.activeSelf, 120, "El bedel no cae derrotado.");

            Assert.That(janitor.IsDead, Is.True);
            Assert.That(player.Health.Current, Is.EqualTo(player.Health.Max), "El pisotón no debe hacerte daño.");
            Assert.That(player.CurrentStateName, Is.EqualTo("Jump"));
        }

        // ---------- Montaje ----------

        private IEnumerator FloorTargetAndFallingPlayer(bool accept = true)
        {
            CreateBox("Suelo", new Vector3(0f, -0.5f, 0f), new Vector3(20f, 1f, 20f));
            target = CreateTarget(accept);
            yield return SpawnPlayer(new Vector3(0f, 3f, 0f), waitForGround: false);
        }

        private IEnumerator WaitForStomp()
        {
            yield return WaitUntil(() => target.Accepted > 0, 120, "No llega a pisarlo.");
        }

        private StompTarget CreateTarget(bool accept, float height = TargetTop)
        {
            var box = CreateBox("Pisable", new Vector3(0f, height * 0.5f, 0f), new Vector3(1f, height, 1f));
            var stompable = box.AddComponent<StompTarget>();
            stompable.Accept = accept;
            return stompable;
        }

        private JanitorEnemy CreateJanitor(Vector3 feetPosition)
        {
            var root = Track(new GameObject("Bedel (test)"));
            root.SetActive(false);
            root.transform.position = feetPosition;

            var body = root.AddComponent<CapsuleCollider>();
            body.height = 2f;
            body.radius = 0.4f;
            body.center = Vector3.up;

            var janitor = root.AddComponent<JanitorEnemy>();
            root.GetComponent<NavMeshAgent>().enabled = false; // sin NavMesh: que se quede quieto
            SetPrivateField(janitor, "config", Track(ScriptableObject.CreateInstance<EnemyConfig>()));

            root.SetActive(true);
            return janitor;
        }

        /// <summary>Algo pisable mínimo que cuenta los pisotones que recibe.</summary>
        private sealed class StompTarget : MonoBehaviour, IStompable
        {
            public bool Accept = true;
            public int Attempts;
            public int Accepted;
            public bool LastWasGroundPound;

            public bool TryStomp(GameObject stomper, bool isGroundPound)
            {
                Attempts++;
                if (!Accept) return false;

                Accepted++;
                LastWasGroundPound = isGroundPound;
                return true;
            }
        }
    }
}
