// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// GenericWorkload.cs
// Purpose: stresses filter regex matching against generic type names such as
// "coverlet.benchmark.subject.Repository`1" and "coverlet.benchmark.subject.Pair`2".
// Also exercises the IsTypeExcluded / IsTypeIncluded cache (P2) with many distinct
// type names that contain backtick suffixes.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace coverlet.benchmark.subject
{
    // ── generic value types ──────────────────────────────────────────────────

    public readonly struct Pair<TFirst, TSecond>
    {
        public TFirst First { get; }
        public TSecond Second { get; }

        public Pair(TFirst first, TSecond second)
        {
            First = first;
            Second = second;
        }

        public void Deconstruct(out TFirst first, out TSecond second)
        {
            first = First;
            second = Second;
        }

        public Pair<TSecond, TFirst> Swap() => new(Second, First);

        public override string ToString() => $"({First}, {Second})";
    }

    public readonly struct Optional<T>
    {
        private readonly T _value;

        public bool HasValue { get; }
        public T Value => HasValue ? _value : throw new InvalidOperationException("No value");

        public Optional(T value) { _value = value; HasValue = true; }

        public T GetValueOrDefault(T fallback = default!) => HasValue ? _value : fallback;

        public Optional<TResult> Map<TResult>(Func<T, TResult> mapper)
            => HasValue ? new Optional<TResult>(mapper(_value)) : default;

        public Optional<T> Where(Func<T, bool> predicate)
            => HasValue && predicate(_value) ? this : default;

        public override string ToString() => HasValue ? $"Some({_value})" : "None";
    }

    // ── generic repository / collection ──────────────────────────────────────

    public class Repository<T> where T : class
    {
        private readonly Dictionary<int, T> _store = [];
        private int _nextId;

        public int Add(T item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));
            int id = ++_nextId;
            _store[id] = item;
            return id;
        }

        public bool TryGet(int id, [MaybeNullWhen(false)] out T item)
            => _store.TryGetValue(id, out item);

        public bool Remove(int id) => _store.Remove(id);

        public IReadOnlyCollection<T> All() => _store.Values;

        public IEnumerable<T> Find(Func<T, bool> predicate)
            => _store.Values.Where(predicate);

        public int Count => _store.Count;
    }

    // ── generic stack ────────────────────────────────────────────────────────

    public class BoundedStack<T>
    {
        private readonly T[] _buffer;
#pragma warning disable IDE0032 // _top is mutated via ++ / -- and cannot be an auto-property
        private int _top;
#pragma warning restore IDE0032

        public BoundedStack(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _buffer = new T[capacity];
        }

        public bool TryPush(T item)
        {
            if (_top >= _buffer.Length) return false;
            _buffer[_top++] = item;
            return true;
        }

        public bool TryPop([MaybeNullWhen(false)] out T item)
        {
            if (_top == 0) { item = default; return false; }
            item = _buffer[--_top];
            _buffer[_top] = default!;
            return true;
        }

        public bool TryPeek([MaybeNullWhen(false)] out T item)
        {
            if (_top == 0) { item = default; return false; }
            item = _buffer[_top - 1];
            return true;
        }

        public bool IsEmpty => _top == 0;
        public bool IsFull => _top == _buffer.Length;
        public int Count => _top;
    }

    // ── generic methods on a non-generic class ───────────────────────────────

    public class GenericWorkload
    {
        public T Identity<T>(T value) => value;

        public T? FirstOrNull<T>(IEnumerable<T> source, Func<T, bool> predicate)
            where T : class
        {
            foreach (T item in source)
            {
                if (predicate(item))
                    return item;
            }
            return null;
        }

        public TResult Transform<TInput, TResult>(TInput input, Func<TInput, TResult> transform)
            => transform(input);

        public IEnumerable<TResult> Select<TSource, TResult>(
            IEnumerable<TSource> source,
            Func<TSource, TResult> selector)
        {
            foreach (TSource item in source)
                yield return selector(item);
        }

        public IEnumerable<T> Filter<T>(IEnumerable<T> source, Func<T, bool> predicate)
        {
            foreach (T item in source)
                if (predicate(item)) yield return item;
        }

        public Pair<TFirst, TSecond> MakePair<TFirst, TSecond>(TFirst a, TSecond b)
            => new(a, b);

        public Optional<T> TryFind<T>(IEnumerable<T> source, Func<T, bool> predicate)
        {
            foreach (T item in source)
                if (predicate(item)) return new Optional<T>(item);
            return default;
        }

        // ── constrained generics ─────────────────────────────────────────────

        public T Max<T>(T a, T b) where T : IComparable<T>
            => a.CompareTo(b) >= 0 ? a : b;

        public T Min<T>(T a, T b) where T : IComparable<T>
            => a.CompareTo(b) <= 0 ? a : b;

        public T Clamp<T>(T value, T lo, T hi) where T : IComparable<T>
        {
            if (value.CompareTo(lo) < 0) return lo;
            if (value.CompareTo(hi) > 0) return hi;
            return value;
        }

        public T[] Sort<T>(T[] items) where T : IComparable<T>
        {
            T[] copy = (T[])items.Clone();
            Array.Sort(copy);
            return copy;
        }

        public bool Contains<T>(IEnumerable<T> source, T value) where T : IEquatable<T>
        {
            foreach (T item in source)
                if (item.Equals(value)) return true;
            return false;
        }

        public int BinarySearch<T>(T[] sortedArray, T target) where T : IComparable<T>
        {
            int lo = 0, hi = sortedArray.Length - 1;
            while (lo <= hi)
            {
                int mid = lo + (hi - lo) / 2;
                int cmp = sortedArray[mid].CompareTo(target);
                if (cmp == 0) return mid;
                if (cmp < 0) lo = mid + 1;
                else hi = mid - 1;
            }
            return -1;
        }

        // ── new() constraint ─────────────────────────────────────────────────

        public T CreateDefault<T>() where T : new() => new();

        public List<T> CreateList<T>(int capacity) where T : new()
        {
            var list = new List<T>(capacity);
            for (int i = 0; i < capacity; i++)
                list.Add(new T());
            return list;
        }

        // ── struct constraint ────────────────────────────────────────────────

        public T[] FillArray<T>(int size, T value) where T : struct
        {
            var arr = new T[size];
            arr.AsSpan().Fill(value);
            return arr;
        }

        public Optional<T> FromNullable<T>(T? value) where T : struct
            => value.HasValue ? new Optional<T>(value.Value) : default;

        // ── covariance / contravariance helpers ──────────────────────────────

        public bool AllSatisfy<T>(IReadOnlyList<T> items, Func<T, bool> predicate)
        {
            for (int i = 0; i < items.Count; i++)
                if (!predicate(items[i])) return false;
            return true;
        }

        public TResult Reduce<T, TResult>(
            IEnumerable<T> source,
            TResult seed,
            Func<TResult, T, TResult> accumulator)
        {
            TResult acc = seed;
            foreach (T item in source)
                acc = accumulator(acc, item);
            return acc;
        }
    }

    // ── deeply generic class (exercises backtick-3 type name) ────────────────

    public class TripleStore<TKey, TValue, TMeta>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, (TValue value, TMeta meta)> _data = [];

        public void Set(TKey key, TValue value, TMeta meta) => _data[key] = (value, meta);

        public bool TryGet(TKey key, out TValue value, out TMeta meta)
        {
            if (_data.TryGetValue(key, out (TValue v, TMeta m) entry))
            {
                value = entry.v;
                meta = entry.m;
                return true;
            }
            value = default!;
            meta = default!;
            return false;
        }

        public IEnumerable<TKey> Keys => _data.Keys;
        public int Count => _data.Count;

        public Dictionary<TKey, TValue> ToValueDictionary()
            => _data.ToDictionary(kv => kv.Key, kv => kv.Value.value);
    }
}
