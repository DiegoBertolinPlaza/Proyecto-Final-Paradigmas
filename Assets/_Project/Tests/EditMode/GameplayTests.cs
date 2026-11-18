using System.Collections.Generic;
using MatriculaDeHonor.Boss;
using MatriculaDeHonor.Combat;
using MatriculaDeHonor.Core;
using MatriculaDeHonor.Enemies.Perception;
using MatriculaDeHonor.GameFlow;
using MatriculaDeHonor.Player;
using MatriculaDeHonor.Player.Stats;
using NUnit.Framework;
using UnityEngine;

namespace MatriculaDeHonor.Tests
{
    public sealed class PredictiveAimTests
    {
        [Test]
        public void StationaryTarget_AimsStraightAtIt()
        {
            bool solved = PredictiveAim.TrySolveIntercept(Vector3.zero, new Vector3(0f, 0f, 10f), Vector3.zero, 5f,
                out var aim, out float time);

            Assert.IsTrue(solved);
            Assert.That(Vector3.Distance(aim, new Vector3(0f, 0f, 10f)), Is.LessThan(1e-3f));
            Assert.That(time, Is.EqualTo(2f).Within(1e-3f));
        }

        [Test]
        public void MovingTarget_ProjectileAndTargetMeetAtAimPoint()
        {
            Vector3 shooter = Vector3.zero;
            Vector3 target = new Vector3(0f, 0f, 10f);
            Vector3 targetVelocity = new Vector3(4f, 0f, 0f);
            const float speed = 12f;

            Assert.IsTrue(PredictiveAim.TrySolveIntercept(shooter, target, targetVelocity, speed, out var aim, out float time));

            Vector3 targetAtImpact = target + targetVelocity * time;
            Vector3 projectileAtImpact = shooter + (aim - shooter).normalized * speed * time;
            Assert.That(Vector3.Distance(targetAtImpact, projectileAtImpact), Is.LessThan(1e-2f));
            Assert.That(aim.x, Is.GreaterThan(0f), "Debe apuntar por delante del objetivo.");
        }

        [Test]
        public void TargetFasterThanProjectileAndFleeing_HasNoSolution()
        {
            bool solved = PredictiveAim.TrySolveIntercept(Vector3.zero, new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 20f), 5f,
                out _, out _);

            Assert.IsFalse(solved);
        }

        [Test]
        public void AimDirection_WithZeroSkill_AimsDirectly()
        {
            Vector3 direction = PredictiveAim.AimDirection(Vector3.zero, new Vector3(0f, 0f, 10f), new Vector3(5f, 0f, 0f), 10f, 0f);

            Assert.That(Vector3.Angle(direction, Vector3.forward), Is.LessThan(0.01f));
        }
    }

    public sealed class VisionConeTests
    {
        [Test]
        public void PointInFrontWithinRange_IsInside()
        {
            Assert.IsTrue(VisionSensor.IsInsideCone(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 5f), 10f, 45f));
        }

        [Test]
        public void PointBehind_IsOutside()
        {
            Assert.IsFalse(VisionSensor.IsInsideCone(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -5f), 10f, 45f));
        }

        [Test]
        public void PointTooFar_IsOutside()
        {
            Assert.IsFalse(VisionSensor.IsInsideCone(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 11f), 10f, 45f));
        }

        [Test]
        public void PointAboveButInFront_IsInside_BecauseAngleIsHorizontal()
        {
            Assert.IsTrue(VisionSensor.IsInsideCone(Vector3.zero, Vector3.forward, new Vector3(0f, 4f, 4f), 10f, 30f));
        }
    }

    public sealed class PlayerStatsTests
    {
        private sealed class FakePowerUp : IStatModifierSource
        {
            private readonly StatType stat;
            private readonly float factor;

            public FakePowerUp(string name, float duration, StatType stat, float factor)
            {
                DisplayName = name;
                Duration = duration;
                this.stat = stat;
                this.factor = factor;
            }

            public string DisplayName { get; }
            public float Duration { get; }

            public IStatProvider Decorate(IStatProvider inner) => new StatMultiplierDecorator(inner, stat, factor);
        }

        private static PlayerStats CreateStats()
        {
            return new PlayerStats(new BaseStatProvider(new Dictionary<StatType, float>
            {
                { StatType.RunSpeed, 10f },
                { StatType.NoiseMultiplier, 1f }
            }));
        }

        [Test]
        public void Decorators_Stack()
        {
            var stats = CreateStats();
            stats.Apply(new FakePowerUp("Café", 10f, StatType.RunSpeed, 1.5f));
            stats.Apply(new FakePowerUp("Zapatillas", 10f, StatType.RunSpeed, 2f));

            Assert.That(stats.Get(StatType.RunSpeed), Is.EqualTo(30f).Within(1e-4f));
            Assert.That(stats.Get(StatType.NoiseMultiplier), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void ExpiredModifier_IsRemovedFromTheChain()
        {
            var stats = CreateStats();
            var coffee = new FakePowerUp("Café", 1f, StatType.RunSpeed, 1.5f);
            var hoodie = new FakePowerUp("Sudadera", 5f, StatType.NoiseMultiplier, 0.2f);
            stats.Apply(coffee);
            stats.Apply(hoodie);

            stats.Tick(1.5f);

            Assert.IsFalse(stats.IsActive(coffee));
            Assert.IsTrue(stats.IsActive(hoodie));
            Assert.That(stats.Get(StatType.RunSpeed), Is.EqualTo(10f).Within(1e-4f));
            Assert.That(stats.Get(StatType.NoiseMultiplier), Is.EqualTo(0.2f).Within(1e-4f));
        }

        [Test]
        public void ApplyingSameSourceAgain_RefreshesInsteadOfStacking()
        {
            var stats = CreateStats();
            var coffee = new FakePowerUp("Café", 2f, StatType.RunSpeed, 1.5f);
            stats.Apply(coffee);
            stats.Tick(1.5f);
            stats.Apply(coffee);
            stats.Tick(1.5f);

            Assert.IsTrue(stats.IsActive(coffee), "La duración debe haberse renovado.");
            Assert.AreEqual(1, stats.ActiveCount);
            Assert.That(stats.Get(StatType.RunSpeed), Is.EqualTo(15f).Within(1e-4f));
        }
    }

    public sealed class PlayerHealthTests
    {
        private static DamageInfo Hit(int amount = 1, DamageKind kind = DamageKind.Contact)
        {
            return new DamageInfo(amount, kind, Faction.Professor, Vector3.zero, null);
        }

        [Test]
        public void Shield_AbsorbsOneHit()
        {
            var health = new PlayerHealth(3, 1);
            health.AddShield(1);

            Assert.AreEqual(DamageResult.Absorbed, health.ApplyDamage(Hit(), 0f));
            Assert.AreEqual(3, health.Current);
            Assert.AreEqual(0, health.Shield);
        }

        [Test]
        public void Invulnerability_IgnoresHitsUntilItExpires()
        {
            var health = new PlayerHealth(3, 0);
            health.ApplyDamage(Hit(), 1f);

            Assert.AreEqual(DamageResult.Ignored, health.ApplyDamage(Hit(), 1f));
            health.Tick(1.1f);
            Assert.AreEqual(DamageResult.Damaged, health.ApplyDamage(Hit(), 1f));
            Assert.AreEqual(1, health.Current);
        }

        [Test]
        public void Falling_KillsEvenWhenInvulnerable()
        {
            var health = new PlayerHealth(3, 0);
            bool died = false;
            health.Died += () => died = true;
            health.ApplyDamage(Hit(), 5f);

            Assert.AreEqual(DamageResult.Killed, health.ApplyDamage(Hit(1, DamageKind.Fall), 5f));
            Assert.IsTrue(died);
            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void ResetToFull_RestoresHealthAndClearsShield()
        {
            var health = new PlayerHealth(3, 1);
            health.AddShield(1);
            health.Kill();

            health.ResetToFull();

            Assert.AreEqual(3, health.Current);
            Assert.AreEqual(0, health.Shield);
            Assert.IsFalse(health.IsDead);
        }
    }

    public sealed class GameSessionTests
    {
        [TearDown]
        public void TearDown() => EventBusRegistry.ClearAll();

        [Test]
        public void EveryHundredCredits_GivesAnExtraLife()
        {
            var session = new GameSession(4);
            session.AddCredits(99);
            Assert.AreEqual(4, session.Lives);

            session.AddCredits(1);
            Assert.AreEqual(5, session.Lives);

            session.AddCredits(250);
            Assert.AreEqual(7, session.Lives);
        }

        [Test]
        public void LoseLife_ReturnsFalseWhenNoLivesLeft()
        {
            var session = new GameSession(2);

            Assert.IsTrue(session.LoseLife());
            Assert.IsFalse(session.LoseLife());
            Assert.AreEqual(0, session.Lives);
            Assert.IsFalse(session.LoseLife(), "No debe bajar de cero.");
        }

        [Test]
        public void Honors_AreCountedOncePerIndex_AndResetPerLevel()
        {
            var session = new GameSession();
            session.BeginLevel(0, 3);

            Assert.IsTrue(session.RegisterHonor(1));
            Assert.IsFalse(session.RegisterHonor(1));
            Assert.AreEqual(1, session.HonorsThisLevel);

            session.BeginLevel(1, 3);
            Assert.AreEqual(0, session.HonorsThisLevel);
        }

        [Test]
        public void LosingLife_PublishesLivesChangedEvent()
        {
            int lastLives = -1;
            EventBus<LivesChangedEvent>.Subscribe(e => lastLives = e.Lives);
            var session = new GameSession(3);

            session.LoseLife();

            Assert.AreEqual(2, lastLives);
        }
    }

    public sealed class BossObservationTests
    {
        [Test]
        public void WriteTo_WritesExactlySizeValues()
        {
            var observation = new BossObservation(true, 0.5f, 0.1f, new Vector3(0.2f, 0f, -0.3f), true, false,
                1f, 0.66f, 0f, 0.5f, false, new Vector2(0.1f, -0.2f));

            var buffer = new float[BossObservation.Size + 2];
            for (int i = 0; i < buffer.Length; i++) buffer[i] = float.NaN;

            observation.WriteTo(buffer, 1);

            Assert.IsNaN(buffer[0]);
            Assert.IsNaN(buffer[buffer.Length - 1]);
            for (int i = 1; i <= BossObservation.Size; i++)
            {
                Assert.IsFalse(float.IsNaN(buffer[i]), $"Índice {i} sin escribir.");
            }
        }

        [Test]
        public void Constructor_ClampsValuesToNetworkFriendlyRanges()
        {
            var observation = new BossObservation(true, 3f, -7f, new Vector3(5f, -5f, 0.5f), false, false,
                2f, -1f, 9f, -9f, false, new Vector2(4f, -4f));

            Assert.AreEqual(1f, observation.Distance01);
            Assert.AreEqual(-1f, observation.TargetHeight);
            Assert.AreEqual(new Vector3(1f, -1f, 0.5f), observation.TargetLocalVelocity);
            Assert.AreEqual(1f, observation.OwnHealth01);
            Assert.AreEqual(0f, observation.TargetHealth01);
            Assert.AreEqual(new Vector2(1f, -1f), observation.ArenaPosition);
        }

        [Test]
        public void ScriptedBrain_Retreats_WhenTargetIsAbove()
        {
            var brain = new ScriptedBossBrain();
            var observation = new BossObservation(true, 0.05f, 0.4f, Vector3.zero, true, true,
                1f, 1f, 1f, 1f, false, Vector2.zero);

            var command = brain.Decide(observation, 0.02f);

            Assert.That(command.Move.y, Is.LessThan(0f));
            Assert.IsFalse(command.Charge);
        }
    }
}
