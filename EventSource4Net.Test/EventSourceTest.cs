using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;

namespace EventSource4Net.Test
{
    [TestClass]
    public class EventSourceTest
    {
        [TestMethod]
        public void TestFailedConnection()
        {
            // setup
            Uri url = new Uri("http://test.com");
            CancellationTokenSource cts = new CancellationTokenSource();
            List<EventSourceState> states = new List<EventSourceState>();
            ServiceResponseMock response = new ServiceResponseMock(url, System.Net.HttpStatusCode.NotFound);
            WebRequesterFactoryMock factory = new WebRequesterFactoryMock(response);
            ManualResetEvent stateIsClosed = new ManualResetEvent(false);

            TestableEventSource es = new TestableEventSource(url, factory);
            es.StateChanged += (o, e) =>
            {
                states.Add(e.State);
                if (e.State == EventSourceState.CLOSED)
                {
                    stateIsClosed.Set();
                    cts.Cancel();
                }
            };


            // act
            stateIsClosed.Reset();

            es.Start(cts.Token);

            stateIsClosed.WaitOne();

            // assert
            Assert.IsTrue(states.Count == 2);
            Assert.AreEqual(states[0], EventSourceState.CONNECTING);
            Assert.AreEqual(states[1], EventSourceState.CLOSED);
        }

        [TestMethod]
        public void TestSuccesfulConnection()
        {
            // setup
            Uri url = new Uri("http://test.com");
            CancellationTokenSource cts = new CancellationTokenSource();
            List<EventSourceState> states = new List<EventSourceState>();
            ServiceResponseMock response = new ServiceResponseMock(url, System.Net.HttpStatusCode.OK);
            WebRequesterFactoryMock factory = new WebRequesterFactoryMock(response);
            ManualResetEvent stateIsOpen = new ManualResetEvent(false);

            TestableEventSource es = new TestableEventSource(url, factory);
            es.StateChanged += (o, e) =>
            {
                states.Add(e.State);
                if (e.State == EventSourceState.OPEN)
                {
                    stateIsOpen.Set();
                    cts.Cancel();
                }
            };


            // act
            stateIsOpen.Reset();

            es.Start(cts.Token);

            stateIsOpen.WaitOne();

            // assert
            Assert.IsTrue(states.Count == 2);
            Assert.AreEqual(states[0], EventSourceState.CONNECTING);
            Assert.AreEqual(states[1], EventSourceState.OPEN);
        }


        [TestMethod]
        public void TestSuccesfulConnectionWithHeaders()
        {
            // setup
            Uri url = new Uri("http://test.com");
            CancellationTokenSource cts = new CancellationTokenSource();
            List<EventSourceState> states = new List<EventSourceState>();
            ServiceResponseMock response = new ServiceResponseMock(url, System.Net.HttpStatusCode.OK);
            WebRequesterFactoryMock factory = new WebRequesterFactoryMock(response);
            ManualResetEvent stateIsOpen = new ManualResetEvent(false);

            var headers = new Dictionary<string, string>
            {
                { "x-key", "headerValue" }
            };

            TestableEventSource es = new TestableEventSource(url, factory, headers);
            es.StateChanged += (o, e) =>
            {
                states.Add(e.State);
                if (e.State == EventSourceState.OPEN)
                {
                    stateIsOpen.Set();
                    cts.Cancel();
                }
            };


            // act
            stateIsOpen.Reset();

            es.Start(cts.Token);

            stateIsOpen.WaitOne();

            // assert
            Assert.AreEqual(1, factory.WebRequesterMock.Response.Headers.Count);
            Assert.AreEqual("headerValue", factory.WebRequesterMock.Response.Headers["x-key"]);
        }
    }
}
        [TestMethod]
        public void TestReConnectionAfterConnectionLost()
        {
            // setup
            Uri url = new Uri("http://test.com");
            CancellationTokenSource cts = new CancellationTokenSource();
            List<EventSourceState> states = new List<EventSourceState>();
            ServiceResponseMock serviceResponseMock = new ServiceResponseMock(url, System.Net.HttpStatusCode.OK);
            WebRequesterFactoryMock factory = new WebRequesterFactoryMock(serviceResponseMock);
            ManualResetEvent stateIsOpen = new ManualResetEvent(false);
            ManualResetEvent stateIsClosed = new ManualResetEvent(false);

            TestableEventSource es = new TestableEventSource(url, factory);
            es.StateChanged += (o, e) =>
            {
                states.Add(e.State);
                if (e.State == EventSourceState.OPEN)
                {
                    stateIsClosed.Reset();
                    stateIsOpen.Set();
                }
                else if (e.State == EventSourceState.CLOSED)
                {
                    stateIsOpen.Reset();
                    stateIsClosed.Set();
                }
            };


            // act
            stateIsOpen.Reset();

            es.Start(cts.Token);

            stateIsOpen.WaitOne();
            states.Clear();

            serviceResponseMock.DistantConnectionClose();

            stateIsClosed.WaitOrThrow();
            stateIsOpen.WaitOrThrow();

            // assert
            Assert.AreEqual(states[0], EventSourceState.CLOSED);
            Assert.AreEqual(states[1], EventSourceState.CONNECTING);
            Assert.AreEqual(states[2], EventSourceState.OPEN);
        }
    }
}
