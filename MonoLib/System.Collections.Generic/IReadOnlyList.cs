// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Interface:  IReadOnlyList<T>
** 
** <OWNER>matell</OWNER>
**
** Purpose: Base interface for read-only generic lists.
** 
===========================================================*/
using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{

    // Provides a read-only, covariant view of a generic list.

    // Note that T[] : IReadOnlyList<T>, and we want to ensure that if you use
    // IList<YourValueType>, we ensure a YourValueType[] can be used 
    // without jitting.  Hence the TypeDependencyAttribute on SZArrayHelper.
    // This is a special hack internally though - see VM\compile.cpp.
    // The same attribute is on IList<T>, IEnumerable<T>, ICollection<T> and IReadOnlyCollection<T>.
    // If we ever implement more interfaces on IReadOnlyList, we should also update RuntimeTypeCache.PopulateInterfaces() in rttype.cs
    public interface IReadOnlyList<T> : IReadOnlyCollection<T>
    {
        T this[int index] { get; }
    }

    public class ReadOnlyListEx<T> : IReadOnlyList<T>, IList<T>
    {
        private List<T> Arr;
        public ReadOnlyListEx(T[] arr)
        {
            Arr = new List<T>(arr);
        }
        public ReadOnlyListEx(IEnumerable<T> arr)
        {
            Arr = new List<T>(arr);
        }
        public T this[int index]
        {
            get
            {
                return Arr[index];
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int Count => Arr.Count;

        public bool IsReadOnly => true;

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            return Arr.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Arr.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Arr.GetEnumerator() as IEnumerator<T>;
        }

        public int IndexOf(T item)
        {
            return Arr.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Arr.GetEnumerator();
        }
    }

    public static class ExtensionList
    {
        public static IReadOnlyList<T> ToReadOnlyList<T>(this T[] arr)
        {
            return new ReadOnlyListEx<T>(arr);
        }
        public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> arr)
        {
            return new ReadOnlyListEx<T>(arr);
        }
        public static IReadOnlyList<T> ToReadOnlyList<T>(this IReadOnlyCollection<T> arr)
        {
            return new ReadOnlyListEx<T>(arr);
        }        
    }
}
