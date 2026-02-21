using System.Diagnostics;
using Sandbox.Reactivity.Internals;

namespace Sandbox.Reactivity;

public static partial class Reactive
{
	/// <param name="item">The item that this function corresponds to.</param>
	/// <param name="index">The index of the item.</param>
	/// <returns>
	/// The optional function to run when this index is removed from the collection, when the value at that index
	/// changes, or the current reactivity scope is disposed.
	/// </returns>
	public delegate Action? CollectionEachDelegate<in T>(T item, int index);

	/// <summary>
	/// Creates a function that will re-run whenever an index in a collection changes its value. The function is run
	/// in an effect root; the function itself will not track any reactive values, but you can create effects inside it
	/// that will.
	/// </summary>
	/// <param name="collection">The function returning the collection to iterate over.</param>
	/// <param name="callback">The function to run for each index in the collection.</param>
	/// <typeparam name="T">The type of item in the collection.</typeparam>
	public static void Each<T>(Func<ICollection<T>> collection, CollectionEachDelegate<T> callback)
	{
		if (Runtime.CurrentEffect is not { } parent)
		{
			throw new InvalidOperationException("Each must be created inside an effect root");
		}

		List<CollectionItem<T>> items = [];
		var isFirstRun = true;

		var eachRoot = new Effect(() =>
			{
				Effect(CollectionEffect);

				return () =>
				{
					foreach (var entry in items)
					{
						entry.Dispose();
					}

					items.Clear();
					isFirstRun = true;
				};
			},
			parent,
			false);

		eachRoot.Run();
		return;

		void CollectionEffect()
		{
			var newItems = collection();

			if (isFirstRun || (items.Count == 0 && newItems.Count > 0))
			{
				// first run or only adding new items to previously empty collection
				var i = 0;
				items.EnsureCapacity(newItems.Count);

				foreach (var item in newItems)
				{
					items.Add(new CollectionItem<T>(item, i++, callback));
				}

				isFirstRun = false;
				return;
			}

			if (items.Count > 0 && newItems.Count == 0)
			{
				// removing all items from collection
				foreach (var entry in items)
				{
					entry.Dispose();
				}

				items.Clear();
				return;
			}

			// handle any additions/changes
			{
				var i = 0;

				foreach (var item in newItems)
				{
					var index = i++;

					if (index < items.Count)
					{
						var existingEntry = items[index];

						if (!EqualityComparer<T>.Default.Equals(item, existingEntry.Value))
						{
							// item at existing index has changed value
							existingEntry.Dispose();
							items[index] = new CollectionItem<T>(item, index, callback);
						}
					}
					else if (index >= items.Count)
					{
						// item added to end of collection
						items.Add(new CollectionItem<T>(item, index, callback));
					}
				}
			}

			if (newItems.Count < items.Count)
			{
				// remove items that are no longer in the collection
				for (var i = items.Count - 1; i >= newItems.Count; i--)
				{
					items[i].Dispose();
				}

				items.RemoveRange(newItems.Count, items.Count - newItems.Count);
			}
		}
	}

	/// <inheritdoc cref="Each{T}(Func{ICollection{T}},CollectionEachDelegate{T})" />
	public static void Each<T>(Func<ICollection<T>> collection, Action<T> callback)
	{
		Each(collection,
			[StackTraceHidden] [DebuggerStepThrough](value, _) =>
			{
				callback(value);
				return null;
			});
	}

	/// <inheritdoc cref="Each{T}(Func{ICollection{T}},CollectionEachDelegate{T})" />
	public static void Each<T>(Func<ICollection<T>> collection, Func<T, Action?> callback)
	{
		Each(collection, [StackTraceHidden] [DebuggerStepThrough](value, _) => callback(value));
	}

	/// <summary>
	/// The entry in an <see cref="Each{T}(Func{ICollection{T}},CollectionEachDelegate{T})" /> collection.
	/// </summary>
	/// <typeparam name="T">The type of the item in the collection.</typeparam>
	private readonly struct CollectionItem<T> : IDisposable
	{
		/// <summary>
		/// The value of the item.
		/// </summary>
		public readonly T Value;

		/// <summary>
		/// The effect root for the item.
		/// </summary>
		private readonly Effect _root;

		public CollectionItem(T value, int index, CollectionEachDelegate<T> callback)
		{
			Value = value;

			// not a child of the collection effect since we don't want to re-run whenever the collection changes; the
			// `Each` method will handle the lifetime of each item's effect
			var effect = new Effect(() => callback(value, index), null, false);
			effect.Run();

			_root = effect;
		}

		/// <summary>
		/// Disposes this item's effect root.
		/// </summary>
		public void Dispose()
		{
			_root.Dispose();
		}
	}
}
