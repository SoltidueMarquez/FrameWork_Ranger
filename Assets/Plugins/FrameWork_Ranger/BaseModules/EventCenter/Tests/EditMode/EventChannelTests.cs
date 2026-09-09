using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FrameWork_Ranger.Events.Tests
{
    internal sealed class EventChannelTests
    {
        private sealed class Payload : EventBase { public override void OnReturn() { } }
        private sealed class Listener
        {
            internal int Calls;
            internal void Receive(Payload payload) { Calls++; }
        }

        [Test]
        public void DuplicateIdentity_AndDifferentTargets_AreHandledIndependently()
        {
            var channel = new EventChannel<Payload>();
            var first = new Listener();
            var second = new Listener();
            channel.Subscribe(first.Receive);
            channel.Subscribe(first.Receive);
            channel.Subscribe(second.Receive);
            channel.Dispatch(new Payload());
            Assert.That(first.Calls, Is.EqualTo(1));
            Assert.That(second.Calls, Is.EqualTo(1));
            channel.Unsubscribe(first.Receive);
            channel.Unsubscribe(first.Receive);
            channel.Dispatch(new Payload());
            Assert.That(first.Calls, Is.EqualTo(1));
            Assert.That(second.Calls, Is.EqualTo(2));
        }

        [Test]
        public void Mutation_SkipsRemovedListener_AndDefersAddedListener()
        {
            var channel = new EventChannel<Payload>();
            var calls = new List<string>();
            Action<Payload> c = _ => calls.Add("C");
            Action<Payload> d = _ => calls.Add("D");
            channel.Subscribe(_ => { calls.Add("A"); channel.Unsubscribe(c); channel.Subscribe(d); });
            channel.Subscribe(_ => calls.Add("B"));
            channel.Subscribe(c);
            channel.Dispatch(new Payload());
            CollectionAssert.AreEqual(new[] { "A", "B" }, calls);
            calls.Clear();
            channel.Dispatch(new Payload());
            CollectionAssert.AreEqual(new[] { "A", "B", "D" }, calls);
        }

        [Test]
        public void RemoveAndReadd_UsesNewPosition_AndSelfRemovalDoesNotInterruptCallback()
        {
            var channel = new EventChannel<Payload>();
            var calls = new List<string>();
            Action<Payload> b = _ => calls.Add("B");
            Action<Payload> a = null;
            a = _ => { channel.Unsubscribe(a); channel.Unsubscribe(b); channel.Subscribe(b); calls.Add("A"); };
            channel.Subscribe(a);
            channel.Subscribe(b);
            channel.Subscribe(_ => calls.Add("C"));
            channel.Dispatch(new Payload());
            CollectionAssert.AreEqual(new[] { "A", "C" }, calls);
            calls.Clear();
            channel.Dispatch(new Payload());
            CollectionAssert.AreEqual(new[] { "C", "B" }, calls);
        }

        [Test]
        public void NestedDispatch_SeesNewListener_WithoutChangingOuterBoundary()
        {
            var channel = new EventChannel<Payload>();
            var calls = new List<string>();
            var nested = false;
            channel.Subscribe(payload =>
            {
                calls.Add(nested ? "inner A" : "outer A start");
                if (nested) return;
                channel.Subscribe(_ => calls.Add("inner D"));
                nested = true;
                channel.Dispatch(payload);
                nested = false;
                calls.Add("outer A end");
            });
            channel.Subscribe(_ => calls.Add(nested ? "inner B" : "outer B"));
            channel.Dispatch(new Payload());
            CollectionAssert.AreEqual(new[] { "outer A start", "inner A", "inner B", "inner D", "outer A end", "outer B" }, calls);
        }

        [Test]
        public void CallbackFailure_LogsOriginalCause_AndContinues()
        {
            var channel = new EventChannel<Payload>();
            var called = false;
            channel.Subscribe(_ => throw new ApplicationException("listener boom"));
            channel.Subscribe(_ => called = true);
            LogAssert.Expect(LogType.Exception, new Regex("listener boom"));
            Assert.DoesNotThrow(() => channel.Dispatch(new Payload()));
            Assert.That(called, Is.True);
        }

        [Test]
        public void ClearDuringDispatch_ReleasesRemainingListeners()
        {
            var channel = new EventChannel<Payload>();
            var calls = 0;
            channel.Subscribe(_ => { calls++; channel.Clear(); });
            channel.Subscribe(_ => calls++);
            channel.Dispatch(new Payload());
            channel.Dispatch(new Payload());
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(channel.IsEmpty, Is.True);
        }
    }
}
