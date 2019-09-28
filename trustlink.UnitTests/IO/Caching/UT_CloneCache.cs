using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trustlink.IO;

namespace Trustlink.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_CloneCache
    {
        CloneCache<MyKey, MyValue> cloneCache;
        MyDataCache<MyKey, MyValue> myDataCache;

        [TestInitialize]
        public void Init()
        {
            myDataCache = new MyDataCache<MyKey, MyValue>();
            cloneCache = new CloneCache<MyKey, MyValue>(myDataCache);
        }

        [TestMethod]
        public void TestCloneCache()
        {
            AssertionExtensions.Should((object) cloneCache).NotBeNull();
        }

        [TestMethod]
        public void TestAddInternal()
        {
            cloneCache.Add(new MyKey("key1"), new MyValue("value1"));
            AssertionExtensions.Should((object) cloneCache[new MyKey("key1")]).Be(new MyValue("value1"));

            cloneCache.Commit();
            myDataCache[new MyKey("key1")].Should().Be(new MyValue("value1"));
        }

        [TestMethod]
        public void TestDeleteInternal()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            cloneCache.Delete(new MyKey("key1"));   //  trackable.State = TrackState.Deleted 
            cloneCache.Commit();

            AssertionExtensions.Should((object) cloneCache.TryGet(new MyKey("key1"))).BeNull();
            myDataCache.TryGet(new MyKey("key1")).Should().BeNull();
        }

        [TestMethod]
        public void TestFindInternal()
        {
            cloneCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));

            var items = cloneCache.Find(new MyKey("key1").ToArray());
            Enumerable.ElementAt<KeyValuePair<MyKey, MyValue>>(items, 0).Key.Should().Be(new MyKey("key1"));
            Enumerable.ElementAt<KeyValuePair<MyKey, MyValue>>(items, 0).Value.Should().Be(new MyValue("value1"));
            Enumerable.Count<KeyValuePair<MyKey, MyValue>>(items).Should().Be(1);

            items = cloneCache.Find(new MyKey("key2").ToArray());
            Enumerable.ElementAt<KeyValuePair<MyKey, MyValue>>(items, 0).Key.Should().Be(new MyKey("key2"));
            Enumerable.ElementAt<KeyValuePair<MyKey, MyValue>>(items, 0).Value.Should().Be(new MyValue("value2"));
            Enumerable.Count<KeyValuePair<MyKey, MyValue>>(items).Should().Be(1);

            items = cloneCache.Find(new MyKey("key3").ToArray());
            Enumerable.ElementAt<KeyValuePair<MyKey, MyValue>>(items, 0).Key.Should().Be(new MyKey("key3"));
            Enumerable.ElementAt<KeyValuePair<MyKey, MyValue>>(items, 0).Value.Should().Be(new MyValue("value3"));
            Enumerable.Count<KeyValuePair<MyKey, MyValue>>(items).Should().Be(1);

            items = cloneCache.Find(new MyKey("key4").ToArray());
            Enumerable.Count<KeyValuePair<MyKey, MyValue>>(items).Should().Be(0);
        }

        [TestMethod]
        public void TestGetInternal()
        {
            cloneCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));

            AssertionExtensions.Should((object) cloneCache[new MyKey("key1")]).Be(new MyValue("value1"));
            AssertionExtensions.Should((object) cloneCache[new MyKey("key2")]).Be(new MyValue("value2"));
            AssertionExtensions.Should((object) cloneCache[new MyKey("key3")]).Be(new MyValue("value3"));

            Action action = () =>
            {
                var item = cloneCache[new MyKey("key4")];
            };
            action.ShouldThrow<KeyNotFoundException>();
        }

        [TestMethod]
        public void TestTryGetInternal()
        {
            cloneCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));

            AssertionExtensions.Should((object) cloneCache.TryGet(new MyKey("key1"))).Be(new MyValue("value1"));
            AssertionExtensions.Should((object) cloneCache.TryGet(new MyKey("key2"))).Be(new MyValue("value2"));
            AssertionExtensions.Should((object) cloneCache.TryGet(new MyKey("key3"))).Be(new MyValue("value3"));
            AssertionExtensions.Should((object) cloneCache.TryGet(new MyKey("key4"))).BeNull();
        }

        [TestMethod]
        public void TestUpdateInternal()
        {
            cloneCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));

            cloneCache.GetAndChange(new MyKey("key1")).Value = "value_new_1";
            cloneCache.GetAndChange(new MyKey("key2")).Value = "value_new_2";
            cloneCache.GetAndChange(new MyKey("key3")).Value = "value_new_3";

            cloneCache.Commit();

            AssertionExtensions.Should((object) cloneCache[new MyKey("key1")]).Be(new MyValue("value_new_1"));
            AssertionExtensions.Should((object) cloneCache[new MyKey("key2")]).Be(new MyValue("value_new_2"));
            AssertionExtensions.Should((object) cloneCache[new MyKey("key3")]).Be(new MyValue("value_new_3"));
            myDataCache[new MyKey("key2")].Should().Be(new MyValue("value_new_2"));
        }
    }
}
