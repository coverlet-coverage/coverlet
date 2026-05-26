// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// SwitchWorkload.cs
// Purpose: maximises branchPoints count per method to stress the inner IL-injection loop,
// the AddBranchTrampolineCode path, and (for P7) the branch-by-line lookup in reporters.
// Methods intentionally use large switch statements, nested if/switch combinations, and
// pattern-matching so that Cecil's GetBranchPoints returns many distinct BranchPoint entries.

using System;
using System.Collections.Generic;

namespace coverlet.benchmark.subject
{
    /// <summary>
    /// Methods with large switch / pattern-match expressions to maximise branch-point density.
    /// </summary>
    public class SwitchWorkload
    {
        // ── 20-arm integer switch ────────────────────────────────────────────

        public string ClassifyScore(int score) => score switch
        {
            < 0 => "invalid",
            < 10 => "F",
            < 20 => "F+",
            < 30 => "E",
            < 40 => "E+",
            < 50 => "D",
            < 55 => "D+",
            < 60 => "C",
            < 65 => "C+",
            < 70 => "B-",
            < 75 => "B",
            < 80 => "B+",
            < 85 => "A-",
            < 90 => "A",
            < 95 => "A+",
            <= 100 => "S",
            _ => "overflow",
        };

        public int MapCodeToValue(int code) => code switch
        {
            1 => 100,
            2 => 200,
            3 => 300,
            4 => 400,
            5 => 500,
            6 => 600,
            7 => 700,
            8 => 800,
            9 => 900,
            10 => 1000,
            11 => 1100,
            12 => 1200,
            13 => 1300,
            14 => 1400,
            15 => 1500,
            16 => 1600,
            17 => 1700,
            18 => 1800,
            19 => 1900,
            20 => 2000,
            _ => -1,
        };

        // ── string switch ────────────────────────────────────────────────────

        public int ParseMonthNumber(string month) => month?.ToUpperInvariant() switch
        {
            "JANUARY" or "JAN" => 1,
            "FEBRUARY" or "FEB" => 2,
            "MARCH" or "MAR" => 3,
            "APRIL" or "APR" => 4,
            "MAY" => 5,
            "JUNE" or "JUN" => 6,
            "JULY" or "JUL" => 7,
            "AUGUST" or "AUG" => 8,
            "SEPTEMBER" or "SEP" or "SEPT" => 9,
            "OCTOBER" or "OCT" => 10,
            "NOVEMBER" or "NOV" => 11,
            "DECEMBER" or "DEC" => 12,
            _ => throw new ArgumentOutOfRangeException(nameof(month)),
        };

        public string GetSeason(int month) => month switch
        {
            12 or 1 or 2 => "Winter",
            3 or 4 or 5 => "Spring",
            6 or 7 or 8 => "Summer",
            9 or 10 or 11 => "Autumn",
            _ => "Unknown",
        };

        // ── type-pattern switch ──────────────────────────────────────────────

        public string DescribeObject(object obj) => obj switch
        {
            null => "null",
            bool b => b ? "true" : "false",
            int i when i < 0 => $"negative int: {i}",
            int i => $"int: {i}",
            long l => $"long: {l}",
            double d => $"double: {d:F2}",
            float f => $"float: {f:F2}",
            decimal m => $"decimal: {m}",
            string s when s.Length == 0 => "empty string",
            string s => $"string[{s.Length}]: {s}",
            char c => $"char: {c}",
            byte[] arr => $"byte[{arr.Length}]",
            int[] arr => $"int[{arr.Length}]",
            IList<int> list => $"IList<int>[{list.Count}]",
            IEnumerable<int> seq => "IEnumerable<int>",
            Exception ex => $"exception: {ex.GetType().Name}",
            _ => $"other: {obj.GetType().Name}",
        };

        // ── nested switch ────────────────────────────────────────────────────

        public string ClassifyPair(int x, int y)
        {
            return x switch
            {
                < 0 => y switch
                {
                    < 0 => "both negative",
                    0 => "x negative, y zero",
                    _ => "x negative, y positive",
                },
                0 => y switch
                {
                    < 0 => "x zero, y negative",
                    0 => "both zero",
                    _ => "x zero, y positive",
                },
                _ => y switch
                {
                    < 0 => "x positive, y negative",
                    0 => "x positive, y zero",
                    _ => "both positive",
                },
            };
        }

        // ── switch with complex guards ───────────────────────────────────────

        public string ClassifyTriangle(int a, int b, int c)
        {
            if (a <= 0 || b <= 0 || c <= 0)
                return "invalid";
            if (a + b <= c || a + c <= b || b + c <= a)
                return "not a triangle";

            return (a, b, c) switch
            {
                var (x, y, z) when x == y && y == z => "equilateral",
                var (x, y, z) when x == y || y == z || x == z => "isosceles",
                var (x, y, z) when x * x + y * y == z * z
                                  || x * x + z * z == y * y
                                  || y * y + z * z == x * x => "right",
                _ => "scalene",
            };
        }

        // ── switch statement (not expression) with fall-through-style patterns

        public int ComputePoints(string category, int quantity)
        {
            int basePoints;
            switch (category)
            {
                case "gold":
                    basePoints = 100;
                    break;
                case "silver":
                    basePoints = 75;
                    break;
                case "bronze":
                    basePoints = 50;
                    break;
                case "iron":
                case "steel":
                    basePoints = 25;
                    break;
                case "wood":
                    basePoints = 10;
                    break;
                default:
                    basePoints = 0;
                    break;
            }

            int multiplier;
            if (quantity <= 0)
                multiplier = 0;
            else if (quantity < 5)
                multiplier = 1;
            else if (quantity < 10)
                multiplier = 2;
            else if (quantity < 20)
                multiplier = 3;
            else if (quantity < 50)
                multiplier = 4;
            else
                multiplier = 5;

            return basePoints * multiplier;
        }

        // ── switch over enum ─────────────────────────────────────────────────

        public enum Priority { Low, Normal, High, Critical, Emergency }

        public int GetDeadlineHours(Priority p) => p switch
        {
            Priority.Low => 168,
            Priority.Normal => 72,
            Priority.High => 24,
            Priority.Critical => 4,
            Priority.Emergency => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(p)),
        };

        public string GetPriorityColor(Priority p)
        {
            switch (p)
            {
                case Priority.Low: return "#808080";
                case Priority.Normal: return "#0000FF";
                case Priority.High: return "#FFA500";
                case Priority.Critical: return "#FF0000";
                case Priority.Emergency: return "#FF00FF";
                default: return "#000000";
            }
        }

        // ── large method with many if/else branches ──────────────────────────

        public string ClassifyCharacter(char c)
        {
            if (c >= 'A' && c <= 'Z') return "uppercase";
            if (c >= 'a' && c <= 'z') return "lowercase";
            if (c >= '0' && c <= '9') return "digit";
            if (c == ' ' || c == '\t' || c == '\n' || c == '\r') return "whitespace";
            if (c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || c == ':') return "punctuation";
            if (c == '+' || c == '-' || c == '*' || c == '/' || c == '%' || c == '=') return "operator";
            if (c == '(' || c == ')' || c == '[' || c == ']' || c == '{' || c == '}') return "bracket";
            if (c == '"' || c == '\'' || c == '`') return "quote";
            if (c == '@' || c == '#' || c == '$' || c == '^' || c == '&') return "special";
            return "other";
        }

        // ── switch inside loop ───────────────────────────────────────────────

        public int[] TransformArray(int[] data, string operation)
        {
            var result = new int[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = operation switch
                {
                    "double" => data[i] * 2,
                    "square" => data[i] * data[i],
                    "negate" => -data[i],
                    "abs" => Math.Abs(data[i]),
                    "increment" => data[i] + 1,
                    "decrement" => data[i] - 1,
                    "zero" => 0,
                    _ => data[i],
                };
            }
            return result;
        }

        // ── multi-condition dispatch ─────────────────────────────────────────

        public string Dispatch(int x, int y, int z)
        {
            bool xPos = x > 0, yPos = y > 0, zPos = z > 0;
            return (xPos, yPos, zPos) switch
            {
                (true, true, true) => "all positive",
                (true, true, false) => "x+y positive",
                (true, false, true) => "x+z positive",
                (false, true, true) => "y+z positive",
                (true, false, false) => "only x positive",
                (false, true, false) => "only y positive",
                (false, false, true) => "only z positive",
                (false, false, false) => "none positive",
            };
        }
    }
}
