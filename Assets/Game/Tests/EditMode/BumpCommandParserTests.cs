using Game.Webhook;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>Covers BumpCommandParser: query, JSON and form bodies, precedence, cleaning and the two error codes. Pure string in, command out -- no socket.</summary>
    public sealed class BumpCommandParserTests
    {
        [Test]
        public void EmptyEverything_GivesAllNullCommand()
        {
            Assert.IsTrue(BumpCommandParser.TryParse(string.Empty, string.Empty, out BumpCommand command, out string error));
            Assert.IsNull(error);
            Assert.IsNull(command.Polarity);
            Assert.IsNull(command.TypeId);
            Assert.IsNull(command.Tag);

            Assert.IsTrue(BumpCommandParser.TryParse(null, null, out command, out _));
            Assert.IsNull(command.Polarity);
        }

        [Test]
        public void QueryOnly_ParsesAllThreeFields()
        {
            Assert.IsTrue(BumpCommandParser.TryParse("polarity=positive&type=boxing&tag=Ana", null, out BumpCommand command, out _));

            Assert.AreEqual(BumpPolarity.Positive, command.Polarity);
            Assert.AreEqual("boxing", command.TypeId);
            Assert.AreEqual("Ana", command.Tag);
        }

        [Test]
        public void JsonBody_ParsesAllThreeFields_AndIgnoresUnknownKeys()
        {
            const string body = "{ \"polarity\": \"negative\", \"type\": \"boxing\", \"tag\": \"Bo\", \"extra\": \"x\", \"n\": 3, \"ok\": true, \"z\": null }";

            Assert.IsTrue(BumpCommandParser.TryParse(string.Empty, body, out BumpCommand command, out string error), error);

            Assert.AreEqual(BumpPolarity.Negative, command.Polarity);
            Assert.AreEqual("boxing", command.TypeId);
            Assert.AreEqual("Bo", command.Tag);
        }

        [Test]
        public void JsonBody_HandlesEscapes()
        {
            const string body = "{\"tag\":\"a\\\"b\\\\c\\/d\\u0041\\tE\"}";

            Assert.IsTrue(BumpCommandParser.TryParse(null, body, out BumpCommand command, out string error), error);

            // The tab is a control character, so cleaning strips it.
            Assert.AreEqual("a\"b\\c/dAE", command.Tag);
        }

        [Test]
        public void FormBody_ParsesFields()
        {
            Assert.IsTrue(BumpCommandParser.TryParse(null, "polarity=negative&type=Boxing&tag=Cy", out BumpCommand command, out _));

            Assert.AreEqual(BumpPolarity.Negative, command.Polarity);
            Assert.AreEqual("Boxing", command.TypeId);
            Assert.AreEqual("Cy", command.Tag);
        }

        [Test]
        public void BodyValuesOverrideQueryValues()
        {
            Assert.IsTrue(BumpCommandParser.TryParse("polarity=positive&tag=FromQuery&type=boxing", "{\"polarity\":\"negative\",\"tag\":\"FromBody\"}", out BumpCommand command, out _));

            Assert.AreEqual(BumpPolarity.Negative, command.Polarity);
            Assert.AreEqual("FromBody", command.Tag);
            Assert.AreEqual("boxing", command.TypeId, "fields the body omits keep their query value");
        }

        [Test]
        public void UrlDecoding_PlusAndPercent()
        {
            Assert.IsTrue(BumpCommandParser.TryParse("tag=Big+Fan%21", null, out BumpCommand command, out _));

            Assert.AreEqual("Big Fan!", command.Tag);
        }

        [Test]
        public void Values_AreCaseInsensitive()
        {
            Assert.IsTrue(BumpCommandParser.TryParse("POLARITY=PoSiTiVe&Type=BOXING", null, out BumpCommand command, out _));

            Assert.AreEqual(BumpPolarity.Positive, command.Polarity);
            Assert.AreEqual("BOXING", command.TypeId, "the id is kept as sent; the catalog matches it case-insensitively");
        }

        [Test]
        public void Tag_IsTrimmedStrippedAndCapped()
        {
            string longTag = new string('x', 40);
            Assert.IsTrue(BumpCommandParser.TryParse("tag=" + System.Uri.EscapeDataString("  a\u0001b\nc  "), null, out BumpCommand command, out _));
            Assert.AreEqual("abc", command.Tag);

            Assert.IsTrue(BumpCommandParser.TryParse("tag=" + longTag, null, out command, out _));
            Assert.AreEqual(BumpCommandParser.MaxTagLength, command.Tag.Length);
        }

        [Test]
        public void Tag_EmptyAfterCleaning_CountsAsMissing()
        {
            Assert.IsTrue(BumpCommandParser.TryParse("tag=%01%02+%20&type=", null, out BumpCommand command, out _));

            Assert.IsNull(command.Tag);
            Assert.IsNull(command.TypeId);
        }

        [Test]
        public void InvalidPolarity_IsRejected()
        {
            Assert.IsFalse(BumpCommandParser.TryParse("polarity=sideways", null, out _, out string error));
            Assert.AreEqual(BumpCommandParser.InvalidPolarity, error);

            Assert.IsFalse(BumpCommandParser.TryParse(null, "{\"polarity\":\"up\"}", out _, out error));
            Assert.AreEqual(BumpCommandParser.InvalidPolarity, error);
        }

        [TestCase("{\"tag\":")]
        [TestCase("{\"tag\":\"unterminated}")]
        [TestCase("{\"tag\":\"a\"")]
        [TestCase("{\"tag\":\"a\",}")]
        [TestCase("{tag:\"a\"}")]
        [TestCase("{\"tag\":\"a\"} trailing")]
        [TestCase("{\"tag\":\"bad\\q\"}")]
        [TestCase("{\"tag\":\"bad\\u12\"}")]
        [TestCase("{\"tag\":{\"nested\":1}}")]
        [TestCase("{\"other\":{\"nested\":1}}")]
        [TestCase("{\"other\":[1,2]}")]
        [TestCase("{\"tag\":5}")]
        [TestCase("{\"polarity\":true}")]
        [TestCase("{\"type\":null}")]
        public void MalformedOrUnsupportedJson_IsBadBody(string body)
        {
            Assert.IsFalse(BumpCommandParser.TryParse(null, body, out _, out string error));
            Assert.AreEqual(BumpCommandParser.BadBody, error);
        }

        [Test]
        public void EmptyValue_DoesNotClearAnEarlierValue()
        {
            Assert.IsTrue(BumpCommandParser.TryParse("polarity=positive&tag=Ana", "polarity=&tag=", out BumpCommand command, out _));
            Assert.AreEqual(BumpPolarity.Positive, command.Polarity);
            Assert.AreEqual("Ana", command.Tag);

            Assert.IsTrue(BumpCommandParser.TryParse("polarity=positive", "{\"polarity\":\"\"}", out command, out _));
            Assert.AreEqual(BumpPolarity.Positive, command.Polarity);
        }

        [Test]
        public void LeadingBom_BeforeJson_IsIgnored()
        {
            Assert.IsTrue(BumpCommandParser.TryParse(null, "\uFEFF{\"tag\":\"Bo\"}", out BumpCommand command, out string error), error);
            Assert.AreEqual("Bo", command.Tag);
        }

        [Test]
        public void Tag_StripsFormatCharactersAndLineSeparators()
        {
            Assert.IsTrue(BumpCommandParser.TryParse(null, "{\"tag\":\"a\u200Bb\u202Ec\u2028d\u2029e\uFEFFf\"}", out BumpCommand command, out _));
            Assert.AreEqual("abcdef", command.Tag);
        }

        [Test]
        public void EmptyJsonObject_IsFine()
        {
            Assert.IsTrue(BumpCommandParser.TryParse(null, " { } ", out BumpCommand command, out _));
            Assert.IsNull(command.Polarity);
        }
    }
}
