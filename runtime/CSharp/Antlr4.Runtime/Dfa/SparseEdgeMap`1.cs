/*
 * [The "BSD license"]
 *  Copyright (c) 2013 Terence Parr
 *  Copyright (c) 2013 Sam Harwell
 *  All rights reserved.
 *
 *  Redistribution and use in source and binary forms, with or without
 *  modification, are permitted provided that the following conditions
 *  are met:
 *
 *  1. Redistributions of source code must retain the above copyright
 *     notice, this list of conditions and the following disclaimer.
 *  2. Redistributions in binary form must reproduce the above copyright
 *     notice, this list of conditions and the following disclaimer in the
 *     documentation and/or other materials provided with the distribution.
 *  3. The name of the author may not be used to endorse or promote products
 *     derived from this software without specific prior written permission.
 *
 *  THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 *  IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 *  OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 *  IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 *  INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 *  NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 *  DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 *  THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 *  (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 *  THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Antlr4.Runtime.Dfa;
using Sharpen;

namespace Antlr4.Runtime.Dfa
{
    /// <author>Sam Harwell</author>
    public class SparseEdgeMap<T> : AbstractEdgeMap<T>
        where T : class
    {
        private const int DefaultMaxSize = 5;

        private readonly int[] keys;

        private readonly List<T> values;

        public SparseEdgeMap(int minIndex, int maxIndex) : this(minIndex, maxIndex, DefaultMaxSize
            )
        {
        }

        public SparseEdgeMap(int minIndex, int maxIndex, int maxSparseSize) : base(minIndex
            , maxIndex)
        {
            this.keys = new int[maxSparseSize];
            this.values = new List<T>(maxSparseSize);
        }

        private SparseEdgeMap(Antlr4.Runtime.Dfa.SparseEdgeMap<T> map, int maxSparseSize)
             : base(map.minIndex, map.maxIndex)
        {
            if (maxSparseSize < map.values.Count)
            {
                throw new ArgumentException();
            }
            keys = Arrays.CopyOf(map.keys, maxSparseSize);
            values = new List<T>(maxSparseSize);
            values.AddRange(map.values);
        }

        public virtual int[] GetKeys()
        {
            return keys;
        }

        public virtual IList<T> GetValues()
        {
            return values;
        }

        public virtual int GetMaxSparseSize()
        {
            return keys.Length;
        }

        public override int Count
        {
            get
            {
                return values.Count;
            }
        }

        public override bool IsEmpty
        {
            get
            {
                return values.Count == 0;
            }
        }

        public override bool ContainsKey(int key)
        {
            return this[key] != null;
        }

        public override T this[int key]
        {
            get
            {
                int index = System.Array.BinarySearch(keys, 0, Count, key);
                if (index < 0)
                {
                    return null;
                }
                return values[index];
            }
        }

        public override AbstractEdgeMap<T> Put(int key, T value)
        {
            if (key < minIndex || key > maxIndex)
            {
                return this;
            }
            if (value == null)
            {
                return ((Antlr4.Runtime.Dfa.SparseEdgeMap<T>)Remove(key));
            }
            lock (values)
            {
                int index = System.Array.BinarySearch(keys, 0, Count, key);
                if (index >= 0)
                {
                    // replace existing entry
                    values[index] = value;
                    return this;
                }
                System.Diagnostics.Debug.Assert(index < 0 && value != null);
                int insertIndex = -index - 1;
                if (Count < GetMaxSparseSize() && insertIndex == Count)
                {
                    // stay sparse and add new entry
                    keys[insertIndex] = key;
                    values.Add(value);
                    return this;
                }
                int desiredSize = Count >= GetMaxSparseSize() ? GetMaxSparseSize() * 2 : GetMaxSparseSize
                    ();
                int space = maxIndex - minIndex + 1;
                // SparseEdgeMap only uses less memory than ArrayEdgeMap up to half the size of the symbol space
                if (desiredSize >= space / 2)
                {
                    ArrayEdgeMap<T> arrayMap = new ArrayEdgeMap<T>(minIndex, maxIndex);
                    arrayMap = ((ArrayEdgeMap<T>)arrayMap.PutAll(this));
                    arrayMap.Put(key, value);
                    return arrayMap;
                }
                else
                {
                    Antlr4.Runtime.Dfa.SparseEdgeMap<T> resized = new Antlr4.Runtime.Dfa.SparseEdgeMap
                        <T>(this, desiredSize);
                    System.Array.Copy(resized.keys, insertIndex, resized.keys, insertIndex + 1, resized
                        .keys.Length - insertIndex - 1);
                    resized.keys[insertIndex] = key;
                    resized.values.Insert(insertIndex, value);
                    return resized;
                }
            }
        }

        public override AbstractEdgeMap<T> Remove(int key)
        {
            int index = System.Array.BinarySearch(keys, 0, Count, key);
            if (index < 0)
            {
                return this;
            }
            if (index == values.Count - 1)
            {
                values.RemoveAt(index);
                return this;
            }
            Antlr4.Runtime.Dfa.SparseEdgeMap<T> result = new Antlr4.Runtime.Dfa.SparseEdgeMap
                <T>(this, GetMaxSparseSize());
            System.Array.Copy(result.keys, index + 1, result.keys, index, Count - index - 1);
            result.values.RemoveAt(index);
            return result;
        }

        public override AbstractEdgeMap<T> Clear()
        {
            if (IsEmpty)
            {
                return this;
            }
            Antlr4.Runtime.Dfa.SparseEdgeMap<T> result = new Antlr4.Runtime.Dfa.SparseEdgeMap
                <T>(this, GetMaxSparseSize());
            result.values.Clear();
            return result;
        }

#if NET_4_5
        public override IReadOnlyDictionary<int, T> ToMap()
#else
        public override IDictionary<int, T> ToMap()
#endif
        {
            if (IsEmpty)
            {
                return Sharpen.Collections.EmptyMap<int, T>();
            }

#if NET_CF
            IDictionary<int, T> result = new SortedList<int, T>();
#else
            IDictionary<int, T> result = new SortedDictionary<int, T>();
#endif
            for (int i = 0; i < Count; i++)
            {
                result[keys[i]] = values[i];
            }
#if NET_4_5
            return new ReadOnlyDictionary<int, T>(result);
#else
            return result;
#endif
        }
    }
}
