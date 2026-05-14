using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using SteamKit2;
using System.ComponentModel;
using System.Composition;
using System.Threading.Tasks;
using System;

namespace FriendsManager;

[Export(typeof(IPlugin))]
internal sealed class FriendsManagerPlugin : IGitHubPluginUpdates, IBotCommand2 {
	public string Name => nameof(FriendsManagerPlugin);
	public string RepositoryName => "dm1tz/FriendsManager";
	public Version Version => typeof(FriendsManagerPlugin).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

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

	public Task OnLoaded() => Task.CompletedTask;
}
