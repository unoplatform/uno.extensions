using System;
using System.Collections.Generic;
using System.Text;

namespace DynamicData
{
	public static class ChangeSetExtensions
	{
		public static IEnumerable<T> GetAddedItems<T>(this IChangeSet<T> changeSet)
		{
			if (changeSet is null)
			{
				throw new ArgumentNullException(nameof(changeSet));
			}

			foreach (var change in changeSet)
			{
				switch (change.Type)
				{
					case ChangeType.Item:
						if (change.Reason == ListChangeReason.Add)
						{
							yield return change.Item.Current;
						}
						break;
					case ChangeType.Range:
						if (change.Reason == ListChangeReason.AddRange)
						{
							foreach (var item in change.Range)
							{
								yield return item;
							}
						}
						break;
				}
			}
		}

		public static IEnumerable<T> GetRemovedItems<T>(this IChangeSet<T> changeSet)
		{
			if (changeSet is null)
			{
				throw new ArgumentNullException(nameof(changeSet));
			}

			foreach (var change in changeSet)
			{
				switch (change.Type)
				{
					case ChangeType.Item:
						if (change.Reason == ListChangeReason.Remove)
						{
							yield return change.Item.Current;
						}
						break;
					case ChangeType.Range:
						if (change.Reason == ListChangeReason.RemoveRange)
						{
							foreach (var item in change.Range)
							{
								yield return item;
							}
						}
						break;
				}
			}
		}
	}
}
