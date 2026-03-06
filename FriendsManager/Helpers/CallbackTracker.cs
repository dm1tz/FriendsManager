using SteamKit2;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FriendsManager.Helpers {
	internal sealed class CallbackTracker : IDisposable {
		private readonly TaskCompletionSource<List<SteamFriends.FriendAddedCallback>> Tcs;
		private readonly List<SteamFriends.FriendAddedCallback> Callbacks;

		private readonly int ExpectedCount;
		private readonly CancellationTokenSource TimeoutCts;

		private bool Disposed;

		public CallbackTracker(int expectedCount) {
			if (expectedCount <= 0) {
				throw new ArgumentOutOfRangeException(nameof(expectedCount), "Expected count must be greater than 0.");
			}
			ExpectedCount = expectedCount;
			Callbacks = new(expectedCount);

			Tcs = new TaskCompletionSource<List<SteamFriends.FriendAddedCallback>>(TaskCreationOptions.RunContinuationsAsynchronously);
			TimeoutCts = new CancellationTokenSource();
		}

		public void AddCallback(SteamFriends.FriendAddedCallback callback) {
			ObjectDisposedException.ThrowIf(Disposed, this);

			lock (Callbacks) {
				if (Tcs.Task.IsCompleted) {
					return;
				}

				Callbacks.Add(callback);
				if (Callbacks.Count >= ExpectedCount) {
					_ = Tcs.TrySetResult([.. Callbacks]);
					TimeoutCts.Cancel();
				}
			}
		}

		public async Task<List<SteamFriends.FriendAddedCallback>> WaitForCallbacks(TimeSpan timeout) {
			ObjectDisposedException.ThrowIf(Disposed, this);
			TimeoutCts.CancelAfter(timeout);

			try {
				return await Tcs.Task.ConfigureAwait(false);
			} catch (OperationCanceledException) {
				lock (Callbacks) {
					if (Callbacks.Count > 0) {
						return [.. Callbacks];
					}
				}
				throw;
			}
		}

		public void Dispose() {
			if (Disposed) {
				return;
			}
			Disposed = true;
			TimeoutCts.Dispose();
		}
	}
}
