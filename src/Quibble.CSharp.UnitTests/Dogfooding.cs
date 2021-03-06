﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Quibble.CSharp.UnitTests
{
    public static class Dogfooding
    {
        public static IReadOnlyList<string> TextDiff(string leftJsonString, string rightJsonString)
        {
            var diffs = JsonStrings.Diff(leftJsonString, rightJsonString);
            var textDiffs = diffs.Select(ToTextDiff);
            return textDiffs.ToList();
        }

        private static string ToTextDiff(Diff diff)
        {
            return diff switch
            {
                TypeDiff typeDiff => ToTypeTextDiff(typeDiff),
                ValueDiff valueDiff => ToValueTypeTextDiff(valueDiff),
                ArrayDiff arrayDiff => ToArrayTextDiff(arrayDiff),
                ObjectDiff objectDiff => ToObjectTextDiff(objectDiff),
                _ => throw new Exception("Unknown diff type")
            };
        }

        private static string ToTypeTextDiff(TypeDiff typeDiff)
        {
            switch (typeDiff.Left, typeDiff.Right)
            {
                case (JsonTrue _, JsonFalse _):
                    return $"Boolean value difference at {typeDiff.Path}: true vs false.";
                case (JsonFalse _, JsonTrue _):
                    return $"Boolean value difference at {typeDiff.Path}: false vs true.";
                default:
                    var leftValueDescription = ToValueDescription(typeDiff.Left);
                    var rightValueDescription = ToValueDescription(typeDiff.Right);
                    return $"Type difference at {typeDiff.Path}: {leftValueDescription} vs {rightValueDescription}.";
            }
        }

        private static string ToValueDescription(JsonValue jsonValue)
        {
            switch (jsonValue)
            {
                case JsonTrue _:
                    return "the boolean true";
                case JsonFalse _:
                    return "the boolean false";
                case JsonString s:
                    return $"the string {s.Text}";
                case JsonNumber n:
                    return $"the number {n.TextRepresentation}";
                case JsonArray a:
                    var itemCount = a.Items.Count;
                    return itemCount switch
                    {
                        0 => "an empty array",
                        1 => "an array with 1 item",
                        _ => $"an array with {itemCount} items"
                    };
                case JsonObject _: return "an object";
                case JsonNull _: return "null";
                default: return "something else";
            }
        }

        private static string ToValueTypeTextDiff(ValueDiff valueDiff)
        {
            switch (valueDiff.Left, valueDiff.Right)
            {
                case (JsonString leftStringValue, JsonString rightStringValue):
                    var leftStringText = leftStringValue.Text;
                    var rightStringText = rightStringValue.Text;
                    var maxStrLen = Math.Max(leftStringText.Length, rightStringText.Length);
                    var comparisonStr = maxStrLen > 30
                        ? $"    {leftStringText}\nvs\n    {rightStringText}"
                        : $"{leftStringText} vs {rightStringText}.";
                    return $"String value difference at {valueDiff.Path}: {comparisonStr}";
                case (JsonNumber leftNumberValue, JsonNumber rightNumberValue):
                    var leftNumberText = leftNumberValue.TextRepresentation;
                    var rightNumberText = rightNumberValue.TextRepresentation;
                    return $"Number value difference at {valueDiff.Path}: {leftNumberText} vs {rightNumberText}.";
                default:
                    return $"Some other value difference at {valueDiff.Path}.";
            }
        }

        private static string ToPropertyTypeString(JsonValue jsonValue)
        {
            return jsonValue switch
            {
                JsonTrue _ => "bool",
                JsonFalse _ => "bool",
                JsonString _ => "string",
                JsonNumber _ => "number",
                JsonObject _ => "object",
                JsonArray _ => "array",
                JsonNull _ => "null",
                JsonUndefined _ => "undefined",
                _ => "undefined"
            };
        }

        private static string ToPropertyString(string property, JsonValue jsonValue)
        {
            return $"'{property}' ({ToPropertyTypeString(jsonValue)})";
        }

        private static string ToArrayTextDiff(ArrayDiff arrayDiff)
        {
            var mismatches = arrayDiff.Mismatches;
            var modifications = mismatches.Select(ToModification);
            var details = string.Join("\n", modifications);
            return $"Array difference at {arrayDiff.Path}.\n{details}";
        }

        private static string ToObjectTextDiff(ObjectDiff objectDiff)
        {
            var mismatches = objectDiff.Mismatches;
            var leftOnlyProperties = mismatches.OfType<LeftOnlyProperty>();
            var lefts = leftOnlyProperties.Select(it => ToPropertyString(it.PropertyName, it.PropertyValue)).ToList();
            var leftsOnlyText = ToLeftOnlyText(lefts);
            var rightOnlyProperties = mismatches.OfType<RightOnlyProperty>();
            var rights = rightOnlyProperties.Select(it => ToPropertyString(it.PropertyName, it.PropertyValue)).ToList();
            var rightsOnlyText = ToRightOnlyText(rights);
            var bothCombined = new List<string> {leftsOnlyText, rightsOnlyText}.Where(it => it != null).ToList();
            var details = string.Join("\n", bothCombined);
            return $"Object difference at {objectDiff.Path}.\n{details}";
            
        }

        private static string ToModification(ItemMismatch itemMismatch)
        {
            return itemMismatch switch
            {
                LeftOnlyItem leftOnlyItem => "foo",
                RightOnlyItem rightOnlyItem => "bar"
            };
        }

        private static string ToLeftOnlyText(List<string> lefts)
        {
            return ToPropsSummaryText("Left only", lefts);
        }

        private static string ToRightOnlyText(List<string> rights)
        {
            return ToPropsSummaryText("Right only", rights);
        }

        private static string ToPropsSummaryText(string kind, List<string> props)
        {
            if (props.Count == 0)
            {
                return null;
            }

            var text = props.Count == 1 ? "property" : "properties";
            var diffText = string.Join(", ", props);
            return $"{kind} {text}: {diffText}.";
        }
    }
}