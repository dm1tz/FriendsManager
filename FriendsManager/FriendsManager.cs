using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using FriendsManager.Helpers;
using SteamKit2;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Composition;
using System.Threading.Tasks;
using System;

namespace FriendsManager;

[Export(typeof(IPlugin))]
internal sealed class FriendsManagerPlugin : IGitHubPluginUpdates, IBotCommand2, IBotSteamClient {
	public string Name => nameof(FriendsManagerPlugin);
	public string RepositoryName => "dm1tz/FriendsManager";
	public Version Version => typeof(FriendsManagerPlugin).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	public static readonly ConcurrentDictionary<Bot, CallbackTracker> FriendAddTrackers = new();

	public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) {
		ArgumentNullException.ThrowIfNull(bot);

		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(message);

		if ((args == null) || (args.Length == 0)) {
			throw new ArgumentNullException(nameof(args));
		}

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		return await Commands.OnBotCommand(bot, access, args, steamID).ConfigureAwait(false);
	}

	public Task OnBotSteamCallbacksInit(Bot bot, CallbackManager callbackManager) {
		_ = callbackManager.Subscribe<SteamFriends.FriendAddedCallback>(callback => OnFriendAdded(bot, callback));

		return Task.CompletedTask;
	}

	private static void OnFriendAdded(Bot bot, SteamFriends.FriendAddedCallback callback) {
		if (FriendAddTrackers.TryGetValue(bot, out CallbackTracker? tracker)) {
			tracker.AddCallback(callback);
		}
	}

	public Task<IReadOnlyCollection<ClientMsgHandler>?> OnBotSteamHandlersInit(Bot bot) => Task.FromResult<IReadOnlyCollection<ClientMsgHandler>?>(null);

	public Task OnLoaded() => Task.CompletedTask;
}
