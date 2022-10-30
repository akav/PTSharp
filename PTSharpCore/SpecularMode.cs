using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace PTSharpCore
{
    public enum SpecularMode
    {
        SpecularModeNaive, SpecularModeFirst, SpecularModeAll
    }


    public class MyList<T> : IList<T>
    {
        // the (thread-unsafe) collection that actually stores everything
        private List<T> m_Inner;
        // this is the object we shall lock on. 
        private readonly object m_Lock = new object();

        int ICollection<T>.Count => throw new System.NotImplementedException();

        bool ICollection<T>.IsReadOnly => throw new System.NotImplementedException();

        T IList<T>.this[int index] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            // instead of returning an usafe enumerator,
            // we wrap it into our thread-safe class
            return new SafeEnumerator<T>(m_Inner.GetEnumerator(), m_Lock);
        }

        // To be actually thread-safe, our collection
        // must be locked on all other operations
        // For example, this is how Add() method should look
        public void Add(T item)
        {
            lock (m_Lock)
                m_Inner.Add(item);
        }

        int IList<T>.IndexOf(T item)
        {
            throw new System.NotImplementedException();
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new System.NotImplementedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new System.NotImplementedException();
        }

        void ICollection<T>.Clear()
        {
            throw new System.NotImplementedException();
        }

        bool ICollection<T>.Contains(T item)
        {
            throw new System.NotImplementedException();
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        
    }

    public class SafeEnumerator<T> : IEnumerator<T>
    {
        // this is the (thread-unsafe)
        // enumerator of the underlying collection
        private readonly IEnumerator<T> m_Inner;
        // this is the object we shall lock on. 
        private readonly object m_Lock;

        public SafeEnumerator(IEnumerator<T> inner, object @lock)
        {
            m_Inner = inner;
            m_Lock = @lock;
            // entering lock in constructor
            Monitor.Enter(m_Lock);
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            // .. and exiting lock on Dispose()
            // This will be called when foreach loop finishes
            Monitor.Exit(m_Lock);
        }

        #endregion

        #region Implementation of IEnumerator

        // we just delegate actual implementation
        // to the inner enumerator, that actually iterates
        // over some collection

        public bool MoveNext()
        {
            return m_Inner.MoveNext();
        }

        public void Reset()
        {
            m_Inner.Reset();
        }

        public T Current
        {
            get { return m_Inner.Current; }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        #endregion
    }
}
