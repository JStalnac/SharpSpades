using NUnit.Framework;
using SharpSpades.Api.Configuration;
using System;
using Tommy;

namespace Tests
{
    public class FieldBuilderTests
    {
        private FieldBuilder builder;

        [SetUp]
        public void Setup()
        {
            builder = new FieldBuilder();
        }

        [Test]
        public void InitialValue_Should_Fail_Null()
            => Assert.Throws<ArgumentNullException>(() => builder.InitialValue(null));

        [Test]
        public void InitialValue_Should_Fail_Invalid_Array()
            => Assert.Throws<InvalidOperationException>(() => builder.InitialValue(new TomlNode[]
            {
                1, 1, "2", "3", 5, 8
            }));

        [Test]
        public void InitialValue_Should_Fail_Table()
            => Assert.Throws<InvalidOperationException>(() => builder.InitialValue(new TomlTable()));

        [Test]
        public void InitialValue_Should_Fail_Table_Array()
            => Assert.Throws<InvalidOperationException>(() => builder.InitialValue(new TomlArray
            {
                new TomlTable
                {
                    ["Hi"] = "Hello!"
                }
            }));

        [Test]
        public void InitialValue_Should_Succeed_Array()
            => builder.InitialValue(new TomlNode[] { 1, 1, 2, 3, 5, 8 });

        [Test]
        public void InitialValue_Should_Succeed_Int()
            => builder.InitialValue(1);
    }
}