using System.Collections.Generic;
using System.Text.RegularExpressions;
using MatriculaDeHonor.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MatriculaDeHonor.Tests
{
    public sealed class StateMachineTests
    {
        private sealed class RecordingState : IState
        {
            private readonly List<string> log;

            public RecordingState(string name, List<string> log)
            {
                Name = name;
                this.log = log;
            }

            public string Name { get; }
            public int Ticks { get; private set; }

            public void Enter() => log.Add($"enter {Name}");
            public void Exit() => log.Add($"exit {Name}");
            public void Tick(float deltaTime) => Ticks++;
            public void FixedTick(float fixedDeltaTime) { }
        }

        [Test]
        public void SetState_CallsExitThenEnter()
        {
            var log = new List<string>();
            var a = new RecordingState("A", log);
            var b = new RecordingState("B", log);
            var fsm = new StateMachine();

            fsm.SetState(a);
            fsm.SetState(b);

            CollectionAssert.AreEqual(new[] { "enter A", "exit A", "enter B" }, log);
            Assert.AreSame(b, fsm.Current);
            Assert.AreSame(a, fsm.Previous);
        }

        [Test]
        public void Tick_FollowsTransitionWhenConditionIsTrue()
        {
            var log = new List<string>();
            var a = new RecordingState("A", log);
            var b = new RecordingState("B", log);
            var fsm = new StateMachine();
            bool go = false;
            fsm.AddTransition(a, b, () => go);
            fsm.SetState(a);

            fsm.Tick(0.1f);
            Assert.AreSame(a, fsm.Current);

            go = true;
            fsm.Tick(0.1f);
            Assert.AreSame(b, fsm.Current);
            Assert.AreEqual(1, b.Ticks, "El estado nuevo debe recibir el Tick del mismo frame.");
        }

        [Test]
        public void AnyTransition_HasPriorityOverStateTransitions()
        {
            var log = new List<string>();
            var a = new RecordingState("A", log);
            var b = new RecordingState("B", log);
            var dead = new RecordingState("Dead", log);
            var fsm = new StateMachine();
            fsm.AddTransition(a, b, () => true);
            fsm.AddAnyTransition(dead, () => true);
            fsm.SetState(a);

            fsm.Tick(0.1f);

            Assert.AreSame(dead, fsm.Current);
        }

        [Test]
        public void AnyTransition_DoesNotReenterCurrentState()
        {
            var log = new List<string>();
            var dead = new RecordingState("Dead", log);
            var fsm = new StateMachine();
            fsm.AddAnyTransition(dead, () => true);
            fsm.SetState(dead);
            log.Clear();

            fsm.Tick(0.1f);
            fsm.Tick(0.1f);

            CollectionAssert.IsEmpty(log);
        }

        [Test]
        public void SetState_WithForceReenter_RestartsCurrentState()
        {
            var log = new List<string>();
            var jump = new RecordingState("Jump", log);
            var fsm = new StateMachine();
            fsm.SetState(jump);
            log.Clear();

            fsm.SetState(jump, forceReenter: true);

            CollectionAssert.AreEqual(new[] { "exit Jump", "enter Jump" }, log);
        }
    }

    public sealed class EventBusTests
    {
        private readonly struct TestEvent : IEvent
        {
            public readonly int Value;
            public TestEvent(int value) => Value = value;
        }

        [SetUp]
        public void SetUp() => EventBus<TestEvent>.Clear();

        [TearDown]
        public void TearDown() => EventBus<TestEvent>.Clear();

        [Test]
        public void Raise_DeliversToAllSubscribers()
        {
            int sum = 0;
            EventBus<TestEvent>.Subscribe(e => sum += e.Value);
            EventBus<TestEvent>.Subscribe(e => sum += e.Value * 10);

            EventBus<TestEvent>.Raise(new TestEvent(2));

            Assert.AreEqual(22, sum);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            int calls = 0;
            void Handler(TestEvent _) => calls++;

            EventBus<TestEvent>.Subscribe(Handler);
            EventBus<TestEvent>.Unsubscribe(Handler);
            EventBus<TestEvent>.Raise(new TestEvent(1));

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Subscribe_SameHandlerTwice_IsDeliveredOnce()
        {
            int calls = 0;
            void Handler(TestEvent _) => calls++;

            EventBus<TestEvent>.Subscribe(Handler);
            EventBus<TestEvent>.Subscribe(Handler);
            EventBus<TestEvent>.Raise(new TestEvent(1));

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Handler_CanUnsubscribeItselfDuringRaise()
        {
            int calls = 0;
            void Handler(TestEvent _)
            {
                calls++;
                EventBus<TestEvent>.Unsubscribe(Handler);
            }

            EventBus<TestEvent>.Subscribe(Handler);
            EventBus<TestEvent>.Raise(new TestEvent(1));
            EventBus<TestEvent>.Raise(new TestEvent(1));

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void ExceptionInOneHandler_DoesNotBlockTheOthers()
        {
            bool secondCalled = false;
            EventBus<TestEvent>.Subscribe(_ => throw new System.InvalidOperationException("boom"));
            EventBus<TestEvent>.Subscribe(_ => secondCalled = true);

            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            EventBus<TestEvent>.Raise(new TestEvent(1));

            Assert.IsTrue(secondCalled);
        }

        [Test]
        public void ClearAll_RemovesSubscribersOfEveryEventType()
        {
            EventBus<TestEvent>.Subscribe(_ => { });
            Assert.AreEqual(1, EventBus<TestEvent>.SubscriberCount);

            EventBusRegistry.ClearAll();

            Assert.AreEqual(0, EventBus<TestEvent>.SubscriberCount);
        }
    }

    public sealed class ServiceLocatorTests
    {
        private interface IFoo { }
        private sealed class Foo : IFoo { }

        [TearDown]
        public void TearDown() => ServiceLocator.Clear();

        [Test]
        public void RegisterAndGet_ReturnsSameInstance()
        {
            var foo = new Foo();
            ServiceLocator.Register<IFoo>(foo);

            Assert.AreSame(foo, ServiceLocator.Get<IFoo>());
        }

        [Test]
        public void Unregister_OnlyRemovesTheRegisteredInstance()
        {
            var registered = new Foo();
            var other = new Foo();
            ServiceLocator.Register<IFoo>(registered);

            ServiceLocator.Unregister<IFoo>(other);
            Assert.IsTrue(ServiceLocator.TryGet(out IFoo stillThere));
            Assert.AreSame(registered, stillThere);

            ServiceLocator.Unregister<IFoo>(registered);
            Assert.IsFalse(ServiceLocator.TryGet(out IFoo _));
        }

        [Test]
        public void Get_WithoutRegistration_Throws()
        {
            Assert.Throws<System.InvalidOperationException>(() => ServiceLocator.Get<IFoo>());
        }
    }
}
