using System;
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
                Type typeDiff => ToTypeTextDiff(typeDiff),
                Value valueDiff => ToValueTypeTextDiff(valueDiff),
                ItemCount itemCountDiff => ToItemCountTextDiff(itemCountDiff),
                Properties propertiesDiff => ToPropertiesTextDiff(propertiesDiff),
                _ => throw new Exception("Unknown diff type")
            };
        }

        private static string ToTypeTextDiff(Type typeDiff)
        {
            switch (typeDiff.Left, typeDiff.Right)
            {
                case (True _, False _):
                    return $"Boolean value difference at {typeDiff.Path}: true vs false.";
                case (False _, True _):
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
                case True _:
                    return "the boolean true";
                case False _:
                    return "the boolean false";
                case String s:
                    return $"the string {s.Text}";
                case Number n:
                    return $"the number {n.TextRepresentation}";
                case Array a:
                    var itemCount = a.Items.Count;
                    return itemCount switch
                    {
                        0 => "an empty array",
                        1 => "an array with 1 item",
                        _ => $"an array with {itemCount} items"
                    };
                case Object _: return "an object";
                case Null _: return "null";
                default: return "something else";
            }
        }

        private static string ToValueTypeTextDiff(Value valueDiff)
        {
            switch (valueDiff.Left, valueDiff.Right)
            {
                case (String leftStringValue, String rightStringValue):
                    var leftStringText = leftStringValue.Text;
                    var rightStringText = rightStringValue.Text;
                    var maxStrLen = Math.Max(leftStringText.Length, rightStringText.Length);
                    var comparisonStr = maxStrLen > 30
                        ? $"    {leftStringText}\nvs\n    {rightStringText}"
                        : $"{leftStringText} vs {rightStringText}.";
                    return $"String value difference at {valueDiff.Path}: {comparisonStr}";
                case (Number leftNumberValue, Number rightNumberValue):
                    var leftNumberText = leftNumberValue.TextRepresentation;
                    var rightNumberText = rightNumberValue.TextRepresentation;
                    return $"Number value difference at {valueDiff.Path}: {leftNumberText} vs {rightNumberText}.";
                default:
                    return $"Some other value difference at {valueDiff.Path}.";
            }
        }

        private static string ToItemCountTextDiff(ItemCount itemCountDiff)
        {
            switch (itemCountDiff.Left, itemCountDiff.Right)
            {
                case (Array leftArray, Array rightArray):
                    var leftArrayLength = leftArray.Items.Count;
                    var rightArrayLength = rightArray.Items.Count;
                    return $"Array length difference at {itemCountDiff.Path}: {leftArrayLength} vs {rightArrayLength}.";
                default:
                    throw new Exception("A bug.");
            }
        }

        private static string ToPropertyTypeString(JsonValue jsonValue)
        {
            return jsonValue switch
            {
                True _ => "bool",
                False _ => "bool",
                String _ => "string",
                Number _ => "number",
                Object _ => "object",
                Array _ => "array",
                Null _ => "null",
                Undefined _ => "undefined",
                _ => "undefined"
            };
        }
        
        private static string ToPropertyString(string property, JsonValue jsonValue)
        {
            return $"'{property}' ({ToPropertyTypeString(jsonValue)})";
        }
        
        private static string ToPropertiesTextDiff(Properties propertiesDiff)
        {
            var mismatches = propertiesDiff.Mismatches;
            var leftOnlyProperties = mismatches.OfType<LeftOnlyProperty>();
            var lefts = leftOnlyProperties.Select(it => ToPropertyString(it.PropertyName, it.PropertyValue)).ToList();
            var leftsOnlyText = ToLeftOnlyText(lefts);
            var rightOnlyProperties = mismatches.OfType<RightOnlyProperty>();
            var rights = rightOnlyProperties.Select(it => ToPropertyString(it.PropertyName, it.PropertyValue)).ToList();
            var rightsOnlyText = ToRightOnlyText(rights);
            var bothCombined = new List<string> {leftsOnlyText, rightsOnlyText}.Where(it => it != null).ToList();
            var details = string.Join("\n", bothCombined);
            return $"Object difference at {propertiesDiff.Path}.\n{details}";
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