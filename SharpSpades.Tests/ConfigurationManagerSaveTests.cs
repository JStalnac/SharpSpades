using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SharpSpades.Api.Configuration;
using SharpSpades.Api.Configuration.Attributes;
using System;
using System.IO;
using System.Linq;
using Tommy;

namespace Tests
{
    public class ConfigurationManagerSaveTests
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly ConfigurationManager config;
        private readonly ILogger<ConfigurationManagerSaveTests> logger;
        private readonly int DefaultMaxLevels;

        public ConfigurationManagerSaveTests()
        {
            loggerFactory = LoggerFactory.Create(c =>
            {
                c.SetMinimumLevel(LogLevel.Debug);
                c.AddNUnit();
            });
            config = new(loggerFactory.CreateLogger<ConfigurationManager>());
            logger = loggerFactory.CreateLogger<ConfigurationManagerSaveTests>();
            DefaultMaxLevels = ConfigurationManager.tableMaxLevels;
        }

        [SetUp]
        public void SetUp()
        {
            ConfigurationManager.tableMaxLevels = DefaultMaxLevels;
        }

        [Test]
        public void ThrowIfTypeNotValid_Should_Fail_Generic_Type()
            => Assert.Throws<ArgumentException>(() =>
            {
                ConfigurationManager.ThrowIfTypeNotValid(typeof(GenericClass<>));
            });
        
        [Test]
        public void ThrowIfTypeNotValid_Should_Fail_Nested_Type()
            => Assert.Throws<ArgumentException>(() =>
            {
                ConfigurationManager.ThrowIfTypeNotValid(typeof(MyClass1.MyClass2));
            });

        [Test]
        public void GetProperties_Should_Return_Correct()
        {
            var properties = ConfigurationManager.GetProperties(typeof(TestConfiguration));
            CollectionAssert.AreEquivalent(new string[] { "Foo", "FooBar", "Test" }, properties.Select(p => p.Name));
        }

        [Test]
        public void LoadTable_Should_Succeed_Read_Table()
        {
            logger.LogInformation(nameof(LoadTable_Should_Succeed_Read_Table));
            var table = config.LoadTable(new TestConfiguration
            {
                Test = new()
            }, 0, null, null);
            var testTable = new TomlTable
            {
                ["foo"] = "Hello",
                ["foo_bar"] = "Hi!",
                ["test"] = new TomlTable
                {
                    ["foo"] = "Hello World!",
                    ["foo_bar"] = "Hi!"
                }
            };
            CollectionAssert.AreEquivalent(testTable, table);
        }

        [Test]
        public void LoadTable_Should_Fail_Max_Levels()
        {
            ConfigurationManager.tableMaxLevels = 1;
            logger.LogInformation(nameof(LoadTable_Should_Succeed_Read_Table));
            var table = config.LoadTable(new TestConfiguration
            {
                Test = new()
            }, 1, null, null);
            var testTable = new TomlTable
            {
                ["foo"] = "Hello",
                ["foo_bar"] = "Hi!"
            };
            CollectionAssert.AreEquivalent(testTable, table);
        }

        [Test]
        public void LoadTable_Should_Succeed_Null_Table()
        {
            logger.LogInformation(nameof(LoadTable_Should_Succeed_Null_Table));
            var table = config.LoadTable(new TestConfiguration(), 0, null, null);
            var testTable = new TomlTable
            {
                ["foo"] = "Hello",
                ["foo_bar"] = "Hi!"
            };
            CollectionAssert.AreEquivalent(testTable, table);
        }

        [Test]
        public void LoadTable_Should_Succeed_Three_Levels()
        {
            ConfigurationManager.tableMaxLevels = 2;
            logger.LogInformation(nameof(LoadTable_Should_Succeed_Three_Levels));
            var table = config.LoadTable(new TestConfiguration
            {
                Test = new()
                {
                    Test = new()
                },
            }, 0, null, null);
            var testTable = new TomlTable
            {
                ["foo"] = "Hello",
                ["foo_bar"] = "Hi!",
                ["test"] = new TomlTable
                {
                    ["foo"] = "Hello",
                    ["foo_bar"] = "Hi!",
                    ["test"] = new TomlTable
                    {
                        ["foo"] = "Hello",
                        ["foo_bar"] = "Hi!"
                    }
                }
            };
            CollectionAssert.AreEquivalent(testTable, table);
        }

        [Test]
        public void LoadTable_Should_Fail_No_TableAttribute()
        {
            logger.LogInformation(nameof(LoadTable_Should_Fail_No_TableAttribute));
            var table = config.LoadTable(new TestConfiguration2()
            {
                Test = new TestConfiguration()
            }, 0, null, null);
            var testTable = new TomlTable()
            {
                ["foo"] = "Hello"
            };
            CollectionAssert.AreEquivalent(testTable, table);
        }

        public void LoadTable_Should_Succeed_Collection()
        {
            logger.LogInformation(nameof(LoadTable_Should_Succeed_Collection));
            var table = config.LoadTable(new TestConfiguration3(), 0, null, null);
            var testTable = new TomlTable
            {
                ["motd"] = new TomlNode[] { "Hi!", "Welcome to my SharpSpades server!" }
            };
            CollectionAssert.AreEquivalent(testTable, table);
        }

        [Test]
        public void LoadTable_Should_Succeed_Table1()
        {
            logger.LogInformation(nameof(LoadTable_Should_Succeed_Table1));
            var table = config.LoadTable(new TestConfiguration4(), 0, null, null);
            var testTable = new TomlTable
            {
                ["hello_world"] = "Hello World!",
                ["motd_data"] = new TomlTable
                {
                    ["motd"] = new TomlNode[] { "Hi!", "Welcome to my SharpSpades server!" }
                }
            };
        }

        [Test]
        public void Save_Should_Succeed_Table1()
        {
            logger.LogInformation(nameof(Save_Should_Succeed_Table1));
            var sw = new StringWriter();
            config.Save(new TestConfiguration4(), sw);
            sw.Flush();
            string toml = sw.ToString();

            sw = new StringWriter();
            new TomlTable
            {
                ["hello_world"] = "Hello World!",
                ["motd_data"] = new TomlTable
                {
                    ["motd"] = new TomlNode[] { "Hi!", "Welcome to my SharpSpades server!" }
                }
            }.WriteTo(sw);
            sw.Flush();
            string testToml = sw.ToString();

            Assert.AreEqual(testToml, toml);
        }
    }

    class GenericClass<T>
    {

    }

    class MyClass1
    {
        public class MyClass2
        {

        }
    }

    class TestConfiguration : IConfiguration
    {
        public string Foo { get; set; } = "Hello World!";
        public int Bar { get; }
        public string FooBar { get; set; } = "Hi!";
        public string Baz { get; private set; }

        [Table]
        public TestConfiguration Test { get; set; }
    }

    class TestConfiguration2 : IConfiguration
    {
        public string Foo { get; set; } = "Hello";
        public TestConfiguration Test { get; set; } = new TestConfiguration();
    }

    class TestConfiguration3 : IConfiguration
    {
        public string[] Motd { get; set; } = new[] { "Hi!", "Welcome to my SharpSpades server!" };
    }

    class TestConfiguration4 : IConfiguration
    {
        public string HelloWorld { get; set; } = "Hello World!";
        [Table]
        public TestConfiguration3 MotdData { get; set; } = new();
    }
}