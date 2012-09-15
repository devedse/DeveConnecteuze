using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace DeveConnecteuze.Network
{
    /// <summary>
    /// Thread safe (blocking) expanding queue with TryDequeue() and EnqueueFirst()
    /// </summary>
    [DebuggerDisplay("Count={Count} Capacity={Capacity}")]
    public sealed class DeveQueue<T>
    {
        // Example:
        // m_capacity = 8
        // m_size = 6
        // m_head = 4
        //
        // [0] item
        // [1] item (tail = ((head + size - 1) % capacity)
        // [2] 
        // [3] 
        // [4] item (head)
        // [5] item
        // [6] item 
        // [7] item
        //
        private T[] m_items;
        private readonly object m_lock;
        private int m_size;
        private int m_head;

        /// <summary>
        /// Gets the number of items in the queue
        /// </summary>
        public int Count { get { return m_size; } }

        /// <summary>
        /// Gets the current capacity for the queue
        /// </summary>
        public int Capacity { get { return m_items.Length; } }

        public DeveQueue(int initialCapacity)
        {
            m_lock = new object();
            m_items = new T[initialCapacity];
        }

        /// <summary>
        /// Adds an item last/tail of the queue
        /// </summary>
        public void Enqueue(T item)
        {
            lock (m_lock)
            {
                if (m_size == m_items.Length)
                    SetCapacity(m_items.Length + 8);

                int slot = (m_head + m_size) % m_items.Length;
                m_items[slot] = item;
                m_size++;
            }
        }

        /// <summary>
        /// Places an item first, at the head of the queue
        /// </summary>
        public void EnqueueFirst(T item)
        {
            lock (m_lock)
            {
                if (m_size >= m_items.Length)
                    SetCapacity(m_items.Length + 8);

                m_head--;
                if (m_head < 0)
                    m_head = m_items.Length - 1;
                m_items[m_head] = item;
                m_size++;
            }
        }

        // must be called from within a lock(m_lock) !
        private void SetCapacity(int newCapacity)
        {
            if (m_size == 0)
            {
                if (m_size == 0)
                {
                    m_items = new T[newCapacity];
                    m_head = 0;
                    return;
                }
            }

            T[] newItems = new T[newCapacity];

            if (m_head + m_size - 1 < m_items.Length)
            {
                Array.Copy(m_items, m_head, newItems, 0, m_size);
            }
            else
            {
                Array.Copy(m_items, m_head, newItems, 0, m_items.Length - m_head);
                Array.Copy(m_items, 0, newItems, m_items.Length - m_head, (m_size - (m_items.Length - m_head)));
            }

            m_items = newItems;
            m_head = 0;

        }

        /// <summary>
        /// Gets an item from the head of the queue, or returns default(T) if empty
        /// </summary>
        public bool TryDequeue(out T item)
        {
            if (m_size == 0)
            {
                item = default(T);
                return false;
            }

            lock (m_lock)
            {
                if (m_size == 0)
                {
                    item = default(T);
                    return false;
                }

                item = m_items[m_head];
                m_items[m_head] = default(T);

                m_head = (m_head + 1) % m_items.Length;
                m_size--;

                return true;
            }
        }

        public T TryPeek(int offset)
        {
            if (m_size == 0)
                return default(T);

            lock (m_lock)
            {
                if (m_size == 0)
                    return default(T);
                return m_items[(m_head + offset) % m_items.Length];
            }
        }

        /// <summary>
        /// Determines whether an item is in the queue
        /// </summary>
        public bool Contains(T item)
        {
            lock (m_lock)
            {
                int ptr = m_head;
                for (int i = 0; i < m_size; i++)
                {
                    if (m_items[ptr] == null)
                    {
                        if (item == null)
                            return true;
                    }
                    else
                    {
                        if (m_items[ptr].Equals(item))
                            return true;
                    }
                    ptr = (ptr + 1) % m_items.Length;
                }
            }
            return false;
        }

        /// <summary>
        /// Copies the queue items to a new array
        /// </summary>
        public T[] ToArray()
        {
            lock (m_lock)
            {
                T[] retval = new T[m_size];
                int ptr = m_head;
                for (int i = 0; i < m_size; i++)
                {
                    retval[i] = m_items[ptr++];
                    if (ptr >= m_items.Length)
                        ptr = 0;
                }
                return retval;
            }
        }

        /// <summary>
        /// Removes all objects from the queue
        /// </summary>
        public void Clear()
        {
            lock (m_lock)
            {
                for (int i = 0; i < m_items.Length; i++)
                    m_items[i] = default(T);
                m_head = 0;
                m_size = 0;
            }
        }
    }
}
