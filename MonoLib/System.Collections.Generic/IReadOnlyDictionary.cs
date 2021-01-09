//
// IReadOnlyDictionary.cs
//
// Authors:
//	Marek Safar  <marek.safar@gmail.com>
//
// Copyright (C) 2011 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

//#if NET_4_5
#if NET_4_0

using System.Linq;

namespace System.Collections.Generic
{
	public interface IReadOnlyDictionary<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>>
	{
		TValue this [TKey key] { get; }
		IEnumerable<TKey> Keys { get; }
		IEnumerable<TValue> Values { get; }

		bool ContainsKey (TKey key);
		bool TryGetValue (TKey key, out TValue value);
	}

    public class ReadOnlyDictionaryEx<T, V> : IReadOnlyDictionary<T, V>, IDictionary<T, V>
    {
        private Dictionary<T,V> Arr;
        public ReadOnlyDictionaryEx(KeyValuePair<T, V>[] arr)
        {
            Arr = new Dictionary<T, V>();
            foreach(var item in arr)
            {
                Arr.Add(item.Key, item.Value);
            }
        }
        public ReadOnlyDictionaryEx(IEnumerable<KeyValuePair<T, V>> arr)
        {
            Arr = new Dictionary<T, V>();
            foreach (var item in arr)
            {
                Arr.Add(item.Key, item.Value);
            }
        }

        public ReadOnlyDictionaryEx(IDictionary<T, V> arr)
        {
            Arr = new Dictionary<T, V>();
            foreach (var item in arr)
            {
                Arr.Add(item.Key, item.Value);
            }
        }

        public V this[T key]
        {
            get
            {
                return Arr[key];
            }
        }

        V IDictionary<T, V>.this[T key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int Count => Arr.Count;

        public bool IsReadOnly => true;

        public IEnumerable<T> Keys => throw new NotImplementedException();

        public IEnumerable<V> Values => throw new NotImplementedException();

        ICollection<T> IDictionary<T, V>.Keys => throw new NotImplementedException();

        ICollection<V> IDictionary<T, V>.Values => throw new NotImplementedException();

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Add(T key, V value)
        {
            throw new NotImplementedException();
        }

        public void Add(KeyValuePair<T, V> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<T, V> item)
        {
            return Arr.Contains(item);
        }

        public bool ContainsKey(T key)
        {
            return Arr.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<T, V>[] array, int arrayIndex)
        {
            
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Arr.GetEnumerator() as IEnumerator<T>;
        }

        public bool Remove(T key)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<T, V> item)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(T key, out V value)
        {
            return Arr.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Arr.GetEnumerator();
        }

        IEnumerator<KeyValuePair<T, V>> IEnumerable<KeyValuePair<T, V>>.GetEnumerator()
        {
            foreach(var item in Arr)
            {
                yield return item;
            }
        }
    }

    public static class DictionaryExtensionList
    {
        public static IReadOnlyDictionary<T,V> ToReadOnlyDictionary<T, V>(this KeyValuePair<T,V>[] arr)
        {
            return new ReadOnlyDictionaryEx<T, V>(arr);
        }
        public static IReadOnlyDictionary<T, V> ToReadOnlyDictionary<T, V>(this IEnumerable<KeyValuePair<T,V>> arr)
        {
            return new ReadOnlyDictionaryEx<T,V>(arr);
        }
        public static IReadOnlyDictionary<T, V> ToReadOnlyDictionary<T, V>(this Dictionary<T, V> dict)
        {
            return new ReadOnlyDictionaryEx<T, V>(dict);
        }
    }
}

#endif
