/*
MIT License

Copyright (c) 2019 Bar Arnon

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

==============================================================================

CoreFX (https://github.com/dotnet/corefx)
The MIT License (MIT)
Copyright (c) .NET Foundation and Contributors
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SBModManager.Other {

	/// <summary>
	/// <strong>Copied from The Conservatory</strong>
	/// <para/>
	/// 
	/// Represents a threadsafe variation of a <see cref="HashSet{T}"/> which uniquely stores items by their
	/// hashes (see <see cref="object.GetHashCode"/> and <see cref="IEqualityComparer{T}"/>).
	/// <para/>
	/// The original version of this class was <see href="https://github.com/i3arnon/ConcurrentHashSet/tree/main">created by Bar Arnon under the MIT license</see>. Some design changes
	/// have been made for The Conservatory, such as the move to <see cref="Lock"/> as per .NET 9 Specification, as well as some new names for various methods.
	/// </summary>
	/// <typeparam name="T">The type of the items in the collection.</typeparam>
	/// <remarks>
	/// <strong>Contract:</strong> All public members of <see cref="ConcurrentHashSet{T}"/> are thread-safe 
	/// and may be used concurrently from multiple threads simultaneously.
	/// </remarks>
	[DebuggerDisplay("Count = {Count}")]
	public class ConcurrentHashSet<T> : IReadOnlyCollection<T>, ICollection<T> {
		private const int DEFAULT_CAPACITY = 31;
		private const int MAX_LOCK_NUMBER = 1024;

		private readonly IEqualityComparer<T> _comparer;
		private readonly bool _growLockArray;

		private int _budget;
		private volatile Tables _tables;

		private static int DefaultConcurrencyLevel => System.Environment.ProcessorCount;

		/// <summary>
		/// Gets the <see cref="IEqualityComparer{T}" />
		/// that is used to determine equality for the values in the set.
		/// </summary>
		/// <remarks>
		/// <see cref="ConcurrentHashSet{T}" /> requires an equality implementation to determine
		/// whether values are equal. You can specify an implementation of the <see cref="IEqualityComparer{T}" />
		/// generic interface by using a constructor that accepts a comparer parameter;
		/// if you do not specify one, the default generic equality comparer <see cref="EqualityComparer{T}.Default" /> is used.
		/// </remarks>
		public IEqualityComparer<T> Comparer => _comparer;

		/// <summary>
		/// Gets the number of items contained in the <see cref="ConcurrentHashSet{T}"/>.
		/// </summary>
		/// <remarks>Count has snapshot semantics and represents the number of items in the <see cref="ConcurrentHashSet{T}"/>
		/// at the moment when Count was accessed.</remarks>
		public int Count {
			get {
				int count = 0;
				int acquiredLocks = 0;
				try {
					AcquireAllLocks(ref acquiredLocks);

					int[] countPerLocks = _tables.countPerLock;
					for (int i = 0; i < countPerLocks.Length; i++) {
						count += countPerLocks[i];
					}
				} finally {
					ReleaseLocks(0, acquiredLocks);
				}

				return count;
			}
		}

		/// <summary>
		/// Gets a value that indicates whether the <see cref="ConcurrentHashSet{T}"/> is empty.
		/// </summary>
		public bool IsEmpty {
			get {
				if (!AreAllBucketsEmpty()) {
					return false;
				}

				int acquiredLocks = 0;
				try {
					AcquireAllLocks(ref acquiredLocks);

					return AreAllBucketsEmpty();
				} finally {
					ReleaseLocks(0, acquiredLocks);
				}
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConcurrentHashSet{T}"/>
		/// class that is empty, has the default concurrency level, has the default initial capacity, and
		/// uses the default comparer for the item type.
		/// </summary>
		public ConcurrentHashSet() : this(DefaultConcurrencyLevel, DEFAULT_CAPACITY, true, null) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConcurrentHashSet{T}"/>
		/// class that is empty, has the specified concurrency level and capacity, and uses the default
		/// comparer for the item type.
		/// </summary>
		/// <param name="concurrencyLevel">The estimated number of threads that will update the
		/// <see cref="ConcurrentHashSet{T}"/> concurrently.</param>
		/// <param name="capacity">The initial number of elements that the <see cref="ConcurrentHashSet{T}"/>
		/// can contain.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="concurrencyLevel"/> is
		/// less than 1.</exception>
		/// <exception cref="ArgumentOutOfRangeException"> <paramref name="capacity"/> is less than
		/// 0.</exception>
		public ConcurrentHashSet(int concurrencyLevel, int capacity)
			: this(concurrencyLevel, capacity, false, null) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConcurrentHashSet{T}"/>
		/// class that contains elements copied from the specified <see cref="IEnumerable{T}"/>, has the default concurrency
		/// level, has the default initial capacity, and uses the default comparer for the item type.
		/// </summary>
		/// <param name="collection">The <see cref="IEnumerable{T}"/> whose elements are copied to
		/// the new
		/// <see cref="ConcurrentHashSet{T}"/>.</param>
		/// <exception cref="ArgumentNullException"><paramref name="collection"/> is a null reference.</exception>
		public ConcurrentHashSet(IEnumerable<T> collection)
			: this(collection, null) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConcurrentHashSet{T}"/>
		/// class that is empty, has the specified concurrency level and capacity, and uses the specified
		/// <see cref="IEqualityComparer{T}"/>.
		/// </summary>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/>
		/// implementation to use when comparing items.</param>
		public ConcurrentHashSet(IEqualityComparer<T>? comparer)
			: this(DefaultConcurrencyLevel, DEFAULT_CAPACITY, true, comparer) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConcurrentHashSet{T}"/>
		/// class that contains elements copied from the specified <see cref="IEnumerable"/>, has the default concurrency level, has the default
		/// initial capacity, and uses the specified
		/// <see cref="IEqualityComparer{T}"/>.
		/// </summary>
		/// <param name="collection">The <see cref="IEnumerable{T}"/> whose elements are copied to
		/// the new
		/// <see cref="ConcurrentHashSet{T}"/>.</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/>
		/// implementation to use when comparing items.</param>
		/// <exception cref="ArgumentNullException"><paramref name="collection"/> is a null reference
		/// (Nothing in Visual Basic).
		/// </exception>
		public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer)
			: this(comparer) {
			if (collection == null) throw new ArgumentNullException(nameof(collection));

			InitializeFromCollection(collection);
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="ConcurrentHashSet{T}"/> 
		/// class that contains elements copied from the specified <see cref="IEnumerable"/>, 
		/// has the specified concurrency level, has the specified initial capacity, and uses the specified 
		/// <see cref="IEqualityComparer{T}"/>.
		/// </summary>
		/// <param name="concurrencyLevel">The estimated number of threads that will update the 
		/// <see cref="ConcurrentHashSet{T}"/> concurrently.</param>
		/// <param name="collection">The <see cref="IEnumerable{T}"/> whose elements are copied to the new 
		/// <see cref="ConcurrentHashSet{T}"/>.</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use 
		/// when comparing items.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="collection"/> is a null reference.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="concurrencyLevel"/> is less than 1.
		/// </exception>
		public ConcurrentHashSet(int concurrencyLevel, IEnumerable<T> collection, IEqualityComparer<T>? comparer)
			: this(concurrencyLevel, DEFAULT_CAPACITY, false, comparer) {
			if (collection == null) throw new ArgumentNullException(nameof(collection));

			InitializeFromCollection(collection);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConcurrentHashSet{T}"/>
		/// class that is empty, has the specified concurrency level, has the specified initial capacity, and
		/// uses the specified <see cref="IEqualityComparer{T}"/>.
		/// </summary>
		/// <param name="concurrencyLevel">The estimated number of threads that will update the
		/// <see cref="ConcurrentHashSet{T}"/> concurrently.</param>
		/// <param name="capacity">The initial number of elements that the <see cref="ConcurrentHashSet{T}"/>
		/// can contain.</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/>
		/// implementation to use when comparing items.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="concurrencyLevel"/> is less than 1. -or-
		/// <paramref name="capacity"/> is less than 0.
		/// </exception>
		public ConcurrentHashSet(int concurrencyLevel, int capacity, IEqualityComparer<T>? comparer)
			: this(concurrencyLevel, capacity, false, comparer) {
		}

		private ConcurrentHashSet(int concurrencyLevel, int capacity, bool growLockArray, IEqualityComparer<T>? comparer) {
			ArgumentOutOfRangeException.ThrowIfLessThan(concurrencyLevel, 1);
			ArgumentOutOfRangeException.ThrowIfNegative(capacity);

			// The capacity should be at least as large as the concurrency level. Otherwise, we would have locks that don't guard
			// any buckets.
			if (capacity < concurrencyLevel) {
				capacity = concurrencyLevel;
			}

			object[] locks = new object[concurrencyLevel];
			for (int i = 0; i < locks.Length; i++) {
				locks[i] = new object();
			}

			int[] countPerLock = new int[locks.Length];
			Node[] buckets = new Node[capacity];
			_tables = new Tables(buckets, locks, countPerLock);

			_growLockArray = growLockArray;
			_budget = buckets.Length / locks.Length;
			_comparer = comparer ?? EqualityComparer<T>.Default;
		}

		/// <summary>
		/// Adds the specified item to the <see cref="ConcurrentHashSet{T}"/>.
		/// </summary>
		/// <param name="item">The item to add.</param>
		/// <returns>true if the items was added to the <see cref="ConcurrentHashSet{T}"/>
		/// successfully; false if it already exists.</returns>
		/// <exception cref="OverflowException">The <see cref="ConcurrentHashSet{T}"/>
		/// contains too many items.</exception>
		public bool Add(T item) => AddInternal(item, _comparer.GetHashCode(item!), true);

		/// <summary>
		/// Removes all items from the <see cref="ConcurrentHashSet{T}"/>.
		/// </summary>
		public void Clear() {
			int locksAcquired = 0;
			try {
				AcquireAllLocks(ref locksAcquired);

				if (AreAllBucketsEmpty()) {
					return;
				}

				Tables tables = _tables;
				Tables newTables = new Tables(new Node[DEFAULT_CAPACITY], tables.locks, new int[tables.countPerLock.Length]);
				_tables = newTables;
				_budget = Math.Max(1, newTables.buckets.Length / newTables.locks.Length);
			} finally {
				ReleaseLocks(0, locksAcquired);
			}
		}

		/// <summary>
		/// Determines whether the <see cref="ConcurrentHashSet{T}"/> contains the specified
		/// item.
		/// </summary>
		/// <param name="item">The item to locate in the <see cref="ConcurrentHashSet{T}"/>.</param>
		/// <returns>true if the <see cref="ConcurrentHashSet{T}"/> contains the item; otherwise, false.</returns>
		public bool Contains(T item) => TryGetValue(item, out _);

		/// <summary>
		/// Searches the <see cref="ConcurrentHashSet{T}"/> for a given value and returns the equal value it finds, if any.
		/// </summary>
		/// <param name="equalValue">The value to search for.</param>
		/// <param name="actualValue">The value from the set that the search found, or the default value of <typeparamref name="T"/> when the search yielded no match.</param>
		/// <returns>A value indicating whether the search was successful.</returns>
		/// <remarks>
		/// This can be useful when you want to reuse a previously stored reference instead of
		/// a newly constructed one (so that more sharing of references can occur) or to look up
		/// a value that has more complete data than the value you currently have, although their
		/// comparer functions indicate they are equal.
		/// </remarks>
		public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue) {
			int hashcode = _comparer.GetHashCode(equalValue!);

			// We must capture the _buckets field in a local variable. It is set to a new table on each table resize.
			Tables tables = _tables;

			int bucketNo = GetBucket(hashcode, tables.buckets.Length);

			// We can get away w/out a lock here.
			// The Volatile.Read ensures that the load of the fields of 'n' doesn't move before the load from buckets[i].
			Node? current = Volatile.Read(ref tables.buckets[bucketNo]);

			while (current != null) {
				if (hashcode == current.hashcode && _comparer.Equals(current.item, equalValue)) {
					actualValue = current.item;
					return true;
				}

				current = current.next;
			}

			actualValue = default;
			return false;
		}

		/// <summary>
		/// Attempts to remove the item from the <see cref="ConcurrentHashSet{T}"/>.
		/// </summary>
		/// <param name="item">The item to remove.</param>
		/// <returns>true if an item was removed successfully; otherwise, false.</returns>
		public bool TryRemove(T item) {
			int hashcode = _comparer.GetHashCode(item!);
			while (true) {
				Tables tables = _tables;

				GetBucketAndLockNo(hashcode, out int bucketNo, out int lockNo, tables.buckets.Length, tables.locks.Length);

				lock (tables.locks[lockNo]) {
					// If the table just got resized, we may not be holding the right lock, and must retry.
					// This should be a rare occurrence.
					if (tables != _tables) {
						continue;
					}

					Node? previous = null;
					for (Node? current = tables.buckets[bucketNo]; current != null; current = current.next) {
						Debug.Assert((previous == null && current == tables.buckets[bucketNo]) || previous!.next == current);

						if (hashcode == current.hashcode && _comparer.Equals(current.item, item)) {
							if (previous == null) {
								Volatile.Write(ref tables.buckets[bucketNo], current.next);
							} else {
								previous.next = current.next;
							}

							tables.countPerLock[lockNo]--;
							return true;
						}
						previous = current;
					}
				}

				return false;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

		/// <summary>Returns an enumerator that iterates through the <see cref="ConcurrentHashSet{T}"/>.</summary>
		/// <returns>An enumerator for the <see cref="ConcurrentHashSet{T}"/>.</returns>
		/// <remarks>
		/// The enumerator returned from the collection is safe to use concurrently with
		/// reads and writes to the collection, however it does not represent a moment-in-time snapshot
		/// of the collection.  The contents exposed through the enumerator may contain modifications
		/// made to the collection after <see cref="IEnumerable{T}.GetEnumerator"/> was called.
		/// </remarks>
		IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

		/// <summary>Returns a value-type enumerator that iterates through the <see cref="ConcurrentHashSet{T}"/>.</summary>
		/// <returns>An enumerator for the <see cref="ConcurrentHashSet{T}"/>.</returns>
		/// <remarks>
		/// The enumerator returned from the collection is safe to use concurrently with
		/// reads and writes to the collection, however it does not represent a moment-in-time snapshot
		/// of the collection.  The contents exposed through the enumerator may contain modifications
		/// made to the collection after <see cref="GetEnumerator"/> was called.
		/// </remarks>
		public Enumerator GetEnumerator() => new Enumerator(this);

		/// <summary>
		/// Represents an enumerator for <see cref="ConcurrentHashSet{T}" />.
		/// </summary>
		public struct Enumerator : IEnumerator<T> {
			// Provides a manually-implemented version of (approximately) this iterator:
			//     Node?[] buckets = _tables.Buckets;
			//     for (int i = 0; i < buckets.Length; i++)
			//         for (Node? current = Volatile.Read(ref buckets[i]); current != null; current = current.Next)
			//             yield return new current.Item;

			private readonly ConcurrentHashSet<T> _set;

			private Node?[]? _buckets;
			private Node? _node;
			private int _i;
			private int _state;

			private const int STATE_UNINITIALIZED = 0;
			private const int STATE_OUTER_LOOP = 1;
			private const int STATE_INNER_LOOP = 2;
			private const int STATE_DONE = 3;

			/// <summary>
			/// Constructs an enumerator for <see cref="ConcurrentHashSet{T}" />.
			/// </summary>
			public Enumerator(ConcurrentHashSet<T> set) {
				_set = set;
				_buckets = null;
				_node = null;
				Current = default!;
				_i = -1;
				_state = STATE_UNINITIALIZED;
			}

			/// <summary>
			/// Gets the element in the collection at the current position of the enumerator.
			/// </summary>
			public T Current { get; private set; }

			readonly object? IEnumerator.Current => Current;

			/// <summary>
			/// Sets the enumerator to its initial position, which is before the first element in the collection.
			/// </summary>
			public void Reset() {
				_buckets = null;
				_node = null;
				Current = default!;
				_i = -1;
				_state = STATE_UNINITIALIZED;
			}

			/// <summary>
			/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
			/// </summary>
			public readonly void Dispose() { }

			/// <summary>
			/// Advances the enumerator to the next element of the collection.
			/// </summary>
			/// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
			public bool MoveNext() {
				switch (_state) {
					case STATE_UNINITIALIZED:
						_buckets = _set._tables.buckets;
						_i = -1;
						goto case STATE_OUTER_LOOP;

					case STATE_OUTER_LOOP:
						Node?[]? buckets = _buckets;
						Debug.Assert(buckets != null);

						int i = ++_i;
						if ((uint)i < (uint)buckets!.Length) {
							// The Volatile.Read ensures that we have a copy of the reference to buckets[i]:
							// this protects us from reading fields ('_key', '_value' and '_next') of different instances.
							_node = Volatile.Read(ref buckets[i]);
							_state = STATE_INNER_LOOP;
							goto case STATE_INNER_LOOP;
						}
						goto default;

					case STATE_INNER_LOOP:
						Node? node = _node;
						if (node != null) {
							Current = node.item;
							_node = node.next;
							return true;
						}
						goto case STATE_OUTER_LOOP;

					default:
						_state = STATE_DONE;
						return false;
				}
			}
		}

		void ICollection<T>.Add(T item) => Add(item);

		bool ICollection<T>.IsReadOnly => false;

		/// <summary>
		/// Copies all elements of this <see cref="ConcurrentHashSet{T}"/> into an array.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public T[] ToArray() {
			int locksAcquired = 0;
			try {
				AcquireAllLocks(ref locksAcquired);

				ulong count = 0;
				int[] countPerLock = _tables.countPerLock;
				for (int i = 0; i < countPerLock.Length && count >= 0; i++) {
					count += (ulong)countPerLock[i];
				}

				if (count > int.MaxValue) {
					throw new ArgumentException("The number of elements in the set is greater than the maximum size of an array.");
				}
				T[] array = new T[(int)count];
				CopyToItems(array, 0);
				return array;
			} finally {
				ReleaseLocks(0, locksAcquired);
			}
		}

		void ICollection<T>.CopyTo(T[] array, int arrayIndex) {
			ArgumentNullException.ThrowIfNull(array);
			ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

			int locksAcquired = 0;
			try {
				AcquireAllLocks(ref locksAcquired);

				int count = 0;

				int[] countPerLock = _tables.countPerLock;
				for (int i = 0; i < countPerLock.Length && count >= 0; i++) {
					count += countPerLock[i];
				}

				if (array.Length - count < arrayIndex || count < 0) //"count" itself or "count + arrayIndex" can overflow
				{
					throw new ArgumentException("The index is equal to or greater than the length of the array, or the number of elements in the set is greater than the available space from index to the end of the destination array.");
				}

				CopyToItems(array, arrayIndex);
			} finally {
				ReleaseLocks(0, locksAcquired);
			}
		}

		bool ICollection<T>.Remove(T item) => TryRemove(item);

		private void InitializeFromCollection(IEnumerable<T> collection) {
			foreach (T? item in collection) {
				AddInternal(item, _comparer.GetHashCode(item!), false);
			}

			if (_budget == 0) {
				Tables tables = _tables;
				_budget = tables.buckets.Length / tables.locks.Length;
			}
		}

		private bool AddInternal(T item, int hashcode, bool acquireLock) {
			while (true) {
				Tables tables = _tables;

				GetBucketAndLockNo(hashcode, out int bucketNo, out int lockNo, tables.buckets.Length, tables.locks.Length);

				bool resizeDesired = false;
				bool lockTaken = false;
				try {
					if (acquireLock) {
						Monitor.Enter(tables.locks[lockNo], ref lockTaken);
					}

					// If the table just got resized, we may not be holding the right lock, and must retry.
					// This should be a rare occurrence.
					if (tables != _tables) {
						continue;
					}

					// Try to find this item in the bucket
					Node? previous = null;
					for (Node? current = tables.buckets[bucketNo]; current != null; current = current.next) {
						Debug.Assert(previous == null && current == tables.buckets[bucketNo] || previous!.next == current);
						if (hashcode == current.hashcode && _comparer.Equals(current.item, item)) {
							return false;
						}
						previous = current;
					}

					// The item was not found in the bucket. Insert the new item.
					Volatile.Write(ref tables.buckets[bucketNo], new Node(item, hashcode, tables.buckets[bucketNo]));
					checked {
						tables.countPerLock[lockNo]++;
					}

					//
					// If the number of elements guarded by this lock has exceeded the budget, resize the bucket table.
					// It is also possible that GrowTable will increase the budget but won't resize the bucket table.
					// That happens if the bucket table is found to be poorly utilized due to a bad hash function.
					//
					if (tables.countPerLock[lockNo] > _budget) {
						resizeDesired = true;
					}
				} finally {
					if (lockTaken) {
						Monitor.Exit(tables.locks[lockNo]);
					}
				}

				//
				// The fact that we got here means that we just performed an insertion. If necessary, we will grow the table.
				//
				// Concurrency notes:
				// - Notice that we are not holding any locks at when calling GrowTable. This is necessary to prevent deadlocks.
				// - As a result, it is possible that GrowTable will be called unnecessarily. But, GrowTable will obtain lock 0
				//   and then verify that the table we passed to it as the argument is still the current table.
				//
				if (resizeDesired) {
					GrowTable(tables);
				}

				return true;
			}
		}

		private static int GetBucket(int hashcode, int bucketCount) {
			int bucketNo = (hashcode & 0x7FFFFFFF) % bucketCount;
			Debug.Assert(bucketNo >= 0 && bucketNo < bucketCount);
			return bucketNo;
		}

		private static void GetBucketAndLockNo(int hashcode, out int bucketNo, out int lockNo, int bucketCount, int lockCount) {
			bucketNo = (hashcode & 0x7FFFFFFF) % bucketCount;
			lockNo = bucketNo % lockCount;

			Debug.Assert(bucketNo >= 0 && bucketNo < bucketCount);
			Debug.Assert(lockNo >= 0 && lockNo < lockCount);
		}

		private bool AreAllBucketsEmpty() {
			int[] countPerLock = _tables.countPerLock;
			for (int i = 0; i < countPerLock.Length; i++) {
				if (countPerLock[i] != 0) {
					return false;
				}
			}

			return true;
		}

		private void GrowTable(Tables tables) {
			const int maxArrayLength = 0X7FEFFFFF;
			int locksAcquired = 0;
			try {
				// The thread that first obtains _locks[0] will be the one doing the resize operation
				AcquireLocks(0, 1, ref locksAcquired);

				// Make sure nobody resized the table while we were waiting for lock 0:
				if (tables != _tables) {
					// We assume that since the table reference is different, it was already resized (or the budget
					// was adjusted). If we ever decide to do table shrinking, or replace the table for other reasons,
					// we will have to revisit this logic.
					return;
				}

				// Compute the (approx.) total size. Use an Int64 accumulation variable to avoid an overflow.
				long approxCount = 0;
				for (int i = 0; i < tables.countPerLock.Length; i++) {
					approxCount += tables.countPerLock[i];
				}

				//
				// If the bucket array is too empty, double the budget instead of resizing the table
				//
				if (approxCount < tables.buckets.Length / 4) {
					_budget = 2 * _budget;
					if (_budget < 0) {
						_budget = int.MaxValue;
					}
					return;
				}

				// Compute the new table size. We find the smallest integer larger than twice the previous table size, and not divisible by
				// 2,3,5 or 7. We can consider a different table-sizing policy in the future.
				int newLength = 0;
				bool maximizeTableSize = false;
				try {
					checked {
						// Double the size of the buckets table and add one, so that we have an odd integer.
						newLength = tables.buckets.Length * 2 + 1;

						// Now, we only need to check odd integers, and find the first that is not divisible
						// by 3, 5 or 7.
						while (newLength % 3 == 0 || newLength % 5 == 0 || newLength % 7 == 0) {
							newLength += 2;
						}

						Debug.Assert(newLength % 2 != 0);

						if (newLength > maxArrayLength) {
							maximizeTableSize = true;
						}
					}
				} catch (OverflowException) {
					maximizeTableSize = true;
				}

				if (maximizeTableSize) {
					newLength = maxArrayLength;

					// We want to make sure that GrowTable will not be called again, since table is at the maximum size.
					// To achieve that, we set the budget to int.MaxValue.
					//
					// (There is one special case that would allow GrowTable() to be called in the future: 
					// calling Clear() on the ConcurrentHashSet will shrink the table and lower the budget.)
					_budget = int.MaxValue;
				}

				// Now acquire all other locks for the table
				AcquireLocks(1, tables.locks.Length, ref locksAcquired);

				object[] newLocks = tables.locks;

				// Add more locks
				if (_growLockArray && tables.locks.Length < MAX_LOCK_NUMBER) {
					newLocks = new object[tables.locks.Length * 2];
					Array.Copy(tables.locks, newLocks, tables.locks.Length);
					for (int i = tables.locks.Length; i < newLocks.Length; i++) {
						newLocks[i] = new object();
					}
				}

				Node[] newBuckets = new Node[newLength];
				int[] newCountPerLock = new int[newLocks.Length];

				// Copy all data into a new table, creating new nodes for all elements
				for (int i = 0; i < tables.buckets.Length; i++) {
					Node? current = tables.buckets[i];
					while (current != null) {
						Node? next = current.next;
						GetBucketAndLockNo(current.hashcode, out int newBucketNo, out int newLockNo, newBuckets.Length, newLocks.Length);

						newBuckets[newBucketNo] = new Node(current.item, current.hashcode, newBuckets[newBucketNo]);

						checked {
							newCountPerLock[newLockNo]++;
						}

						current = next;
					}
				}

				// Adjust the budget
				_budget = Math.Max(1, newBuckets.Length / newLocks.Length);

				// Replace tables with the new versions
				_tables = new Tables(newBuckets, newLocks, newCountPerLock);
			} finally {
				// Release all locks that we took earlier
				ReleaseLocks(0, locksAcquired);
			}
		}

		private void AcquireAllLocks(ref int locksAcquired) {
			// First, acquire lock 0
			AcquireLocks(0, 1, ref locksAcquired);

			// Now that we have lock 0, the _locks array will not change (i.e., grow),
			// and so we can safely read _locks.Length.
			AcquireLocks(1, _tables.locks.Length, ref locksAcquired);
			Debug.Assert(locksAcquired == _tables.locks.Length);
		}

		private void AcquireLocks(int fromInclusive, int toExclusive, ref int locksAcquired) {
			Debug.Assert(fromInclusive <= toExclusive);
			object[] locks = _tables.locks;

			for (int i = fromInclusive; i < toExclusive; i++) {
				bool lockTaken = false;
				try {
					Monitor.Enter(locks[i], ref lockTaken);
				} finally {
					if (lockTaken) {
						locksAcquired++;
					}
				}
			}
		}

		private void ReleaseLocks(int fromInclusive, int toExclusive) {
			Debug.Assert(fromInclusive <= toExclusive);

			for (int i = fromInclusive; i < toExclusive; i++) {
				Monitor.Exit(_tables.locks[i]);
			}
		}

		private void CopyToItems(T[] array, int index) {
			Node?[] buckets = _tables.buckets;
			for (int i = 0; i < buckets.Length; i++) {
				for (Node? current = buckets[i]; current != null; current = current.next) {
					array[index] = current.item;
					index++; //this should never flow, CopyToItems is only called when there's no overflow risk
				}
			}
		}

		private class Tables {
			public readonly Node?[] buckets;
			public readonly object[] locks;
			public readonly int[] countPerLock;

			public Tables(Node?[] buckets, object[] locks, int[] countPerLock) {
				this.buckets = buckets;
				this.locks = locks;
				this.countPerLock = countPerLock;
			}
		}

		private class Node {
			public readonly T item;
			public readonly int hashcode;
			public volatile Node? next;

			public Node(T item, int hashcode, Node? next) {
				this.item = item;
				this.hashcode = hashcode;
				this.next = next;
			}
		}
	}
}
