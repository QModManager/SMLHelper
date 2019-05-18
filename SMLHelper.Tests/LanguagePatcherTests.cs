﻿namespace SMLHelper.Tests
{
    using NUnit.Framework;
    using SMLHelper.V2.Patchers;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class LanguagePatcherTests
    {
        private static readonly IEnumerable<string> Keys = new string[]
        {
            "Key",
            "Tooltip_Key",            
        };

        private static readonly IEnumerable<string> CustomValues = new string[]
        {
            // No special tokens
            "CustomValue",
            "CustomValue1",
            "2Custom:Value1",
            "CustomValue%",
            // string.format tokens
            "CustomValue{0}",
            "{0}CustomValue",
            "{0}CustomValue{1}",
            "{0}Custom{1}Value{2}",
            "Custom{0}Value",
            // With Unity line breaks
            "Custom\nValue",
            "\nCustomValue",
            "CustomValue\n",
            "\nCustom\nValue\n",
            // With mix
            "Custom:{0}\n{1}:Value;",
            "Custom-Value\n{0}",
            "#1\nCustom_Value\n",
            "Custom{0}:{1}%Value%",
        };

        private static readonly IEnumerable<string> LineEndings = new string[]
        {
            new string(new char[]{'\n' }),
            new string(new char[]{'\r', '\n' }),
            string.Empty,
        };

        [Test, Combinatorial]
        public void ExtractCustomLinesFromText_WhenTextIsValid_SingleEntry_KeyIsKnown_Overrides(
            [ValueSource(nameof(LineEndings))] string endOfLine,
            [ValueSource(nameof(CustomValues))] string customValue)
        {
            var originalLines = new Dictionary<string, string>
            {
                { "Key", "OriginalValue" }
            };

            string text = "Key:{" + customValue + "}" + endOfLine;

            Console.WriteLine("TestText");
            Console.WriteLine(text);
            int overridesApplied = LanguagePatcher.ExtractCustomLinesFromText("Test1", text, originalLines);

            Assert.AreEqual(1, overridesApplied);
            Assert.AreEqual(customValue, LanguagePatcher.GetCustomLine("Key"));
        }


        [Test, Combinatorial]
        public void ExtractCustomLinesFromText_WhenTextIsValid_MultipleEntries_KeyIsKnown_Overrides(
            [ValueSource(nameof(LineEndings))] string endOfLine,
            [ValueSource(nameof(CustomValues))] string customValue1,
            [ValueSource(nameof(CustomValues))] string customValue2,
            [ValueSource(nameof(Keys))] string secondKey)
        {
            var originalLines = new Dictionary<string, string>
            {
                { "Key1", "OriginalValue1" },
                { secondKey, "OriginalValue2" },
            };

            string text = "Key1:{" + customValue1 + "}" + endOfLine +
                          secondKey + ":{" + customValue2 + "}" + endOfLine;
            Console.WriteLine("TestText");
            Console.WriteLine(text);
            int overridesApplied = LanguagePatcher.ExtractCustomLinesFromText("Test1", text, originalLines);

            Assert.AreEqual(2, overridesApplied);
            Assert.AreEqual(customValue1, LanguagePatcher.GetCustomLine("Key1"));
            Assert.AreEqual(customValue2, LanguagePatcher.GetCustomLine(secondKey));
        }
    }
}
