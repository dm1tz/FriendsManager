using ConsoleTables;
using SteamKit2;
using PluginLocale = FriendsManager.Localization;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using Interaction = ArchiSteamFarm.Steam.Interaction;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System;
using System.ComponentModel;
using ArchiSteamFarm.Core;
using System.Linq;
using ArchiSteamFarm;
using FriendsManager.Helpers;
using System.Threading;

namespace FriendsManager;

internal static class Commands {
	private static readonly string[] TableHeader = ["SteamID", "Name"];

	internal static async Task<string?> OnBotCommand(Bot bot, EAccess access, string[] args, ulong steamID = 0) {
		ArgumentNullException.ThrowIfNull(bot);

		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		if ((args == null) || (args.Length == 0)) {
			throw new ArgumentNullException(nameof(args));
		}

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		switch (args.Length) {
			case 1:
				switch (args[0].ToUpperInvariant()) {
					case "FR" or "FRIENDS":
						return ResponseFriends(access, bot);
					case "SINV" or "SENTINVITES":
						return ResponseSentInvites(access, bot);
					case "RINV" or "RECEIVEDINVITES":
						return ResponseReceivedInvites(access, bot);
					case "FMVERSION" or "FMV":
						return ResponseVersion(access);
				}
				break;

			default:
				switch (args[0].ToUpperInvariant()) {
					case "FRIENDS" or "FR":
						return await ResponseFriends(access, Utilities.GetArgsAsText(args, 1, ","), steamID).ConfigureAwait(false);
					case "SENTINVITES" or "SINV":
						return await ResponseSentInvites(access, Utilities.GetArgsAsText(args, 1, ","), steamID).ConfigureAwait(false);
					case "RECEIVEDINVITES" or "RINV":
						return await ResponseReceivedInvites(access, Utilities.GetArgsAsText(args, 1, ","), steamID).ConfigureAwait(false);
					case "ADFR" or "ADDFRIEND" when args.Length > 2:
						return await ResponseAddFriend(access, args[1], Utilities.GetArgsAsText(args, 2, ","), steamID).ConfigureAwait(false);
					case "ADFR" or "ADDFRIEND":
						return await ResponseAddFriend(access, bot, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);
					case "RMFR" or "REMOVEFRIEND" when args.Length > 2:
						return await ResponseRemoveFriend(access, args[1], Utilities.GetArgsAsText(args, 2, ","), steamID).ConfigureAwait(false);
					case "RMFR" or "REMOVEFRIEND":
						return await ResponseRemoveFriend(access, bot, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);
				}

				break;
		}
		return null;
	}

	private static List<SteamID>? ParseSteamIDs(string steamIDsText) {
		string[] steamIDsArgs = steamIDsText.Split(SharedInfo.ListElementSeparators, StringSplitOptions.RemoveEmptyEntries);

		List<SteamID> steamIDs = [];

		foreach (string steamIDsArg in steamIDsArgs) {
			if (!ulong.TryParse(steamIDsArg, out ulong steamID) || !new SteamID(steamID).IsValid || !new SteamID(steamID).IsIndividualAccount) {
				return null;
			}

			steamIDs.Add(steamID);
		}

		return steamIDs;
	}

	private static List<SteamID> GetFriendList(SteamFriends steamFriends, EFriendRelationship relationship = EFriendRelationship.Friend) => [.. Enumerable.Range(0, steamFriends.GetFriendCount()).Select(i => steamFriends.GetFriendByIndex(i)).Where(friendID => steamFriends.GetFriendRelationship(friendID) == relationship)];

	private static string? ResponseFriends(EAccess access, Bot bot) {
		if (access < EAccess.FamilySharing) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
		}

		SteamFriends steamFriends = bot.SteamFriends;

		List<SteamID> friends = GetFriendList(steamFriends);

		if (friends.Count < 1) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(friends)));
		}

		ConsoleTable friendTable = new ConsoleTable(TableHeader).Configure(o => o.EnableCount = false);

		friends.ForEach(friend => friendTable.AddRow(friend.ConvertToUInt64(), steamFriends.GetFriendPersonaName(friend) ?? string.Empty));

		string result = string.Join(Environment.NewLine, PluginLocale.Strings.FormatBotFriends(friends.Count), friendTable);

		return bot.Commands.FormatBotResponse(result);
	}

	private static async Task<string?> ResponseFriends(EAccess access, string botNames, ulong steamID = 0) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseFriends(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot)))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}

	private static string? ResponseSentInvites(EAccess access, Bot bot) {
		if (access < EAccess.FamilySharing) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
		}

		SteamFriends steamFriends = bot.SteamFriends;

		List<SteamID> sentInvites = GetFriendList(steamFriends, EFriendRelationship.RequestInitiator);

		if (sentInvites.Count < 1) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(sentInvites)));
		}

		ConsoleTable sentInvitesTable = new ConsoleTable(TableHeader).Configure(o => o.EnableCount = false);

		sentInvites.ForEach(friend => sentInvitesTable.AddRow(friend.ConvertToUInt64(), steamFriends.GetFriendPersonaName(friend) ?? string.Empty));

		string result = string.Join(Environment.NewLine, PluginLocale.Strings.FormatBotSentInvites(sentInvites.Count), sentInvitesTable);

		return bot.Commands.FormatBotResponse(result);
	}

	private static async Task<string?> ResponseSentInvites(EAccess access, string botNames, ulong steamID = 0) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseSentInvites(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot)))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}

	private static string? ResponseReceivedInvites(EAccess access, Bot bot) {
		if (access < EAccess.FamilySharing) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
		}

		SteamFriends steamFriends = bot.SteamFriends;

		List<SteamID> receivedInvites = GetFriendList(steamFriends, EFriendRelationship.RequestRecipient);

		if (receivedInvites.Count < 1) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(receivedInvites)));
		}

		ConsoleTable receivedInvitesTable = new ConsoleTable(TableHeader).Configure(o => o.EnableCount = false);

		receivedInvites.ForEach(friend => receivedInvitesTable.AddRow(friend.ConvertToUInt64(), steamFriends.GetFriendPersonaName(friend) ?? string.Empty));

		string result = string.Join(Environment.NewLine, PluginLocale.Strings.FormatBotReceivedInvites(receivedInvites.Count), receivedInvitesTable);

		return bot.Commands.FormatBotResponse(result);
	}

	private static async Task<string?> ResponseReceivedInvites(EAccess access, string botNames, ulong steamID = 0) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseReceivedInvites(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot)))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}

	private static async Task<string?> ResponseAddFriend(EAccess access, Bot bot, string steamIDsText) {
		ArgumentException.ThrowIfNullOrEmpty(steamIDsText);

		if (access < EAccess.Master) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
		}

		List<SteamID>? steamIDs = ParseSteamIDs(steamIDsText);

		if ((steamIDs == null) || (steamIDs.Count == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(steamIDs)));
		}

		SteamFriends steamFriends = bot.SteamFriends;

		CallbackTracker tracker = new(steamIDs.Count);
		FriendsManagerPlugin.FriendAddTrackers[bot] = tracker;

		try {
			steamIDs.ForEach(steamFriends.AddFriend);

			List<SteamFriends.FriendAddedCallback> callbacks;

			try {
				callbacks = await tracker.WaitForCallbacks(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
			} catch (OperationCanceledException) {
				callbacks = [];
			}

			List<SteamID> sentInvites = [.. callbacks.Select(static c => c.SteamID)];

			ConsoleTable sentInvitesTable = new ConsoleTable(TableHeader).Configure(o => o.EnableCount = false);

			sentInvites.ForEach(steamID => sentInvitesTable.AddRow(steamID.ConvertToUInt64(), steamFriends.GetFriendPersonaName(steamID) ?? string.Empty));

			string result = string.Join(Environment.NewLine, PluginLocale.Strings.FormatBotAddedFriends(steamIDs.Count, sentInvites.Count), sentInvitesTable);

			return bot.Commands.FormatBotResponse(result);
		} finally {
			_ = FriendsManagerPlugin.FriendAddTrackers.TryRemove(bot, out _);
		}
	}

	private static async Task<string?> ResponseAddFriend(EAccess access, string botNames, string steamIDsText, ulong steamID = 0) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseAddFriend(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot, steamIDsText)))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}

	private static async Task<string?> ResponseRemoveFriend(EAccess access, Bot bot, string steamIDsText) {
		ArgumentException.ThrowIfNullOrEmpty(steamIDsText);

		if (access < EAccess.Master) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
		}

		List<SteamID>? steamIDs = ParseSteamIDs(steamIDsText);

		if ((steamIDs == null) || (steamIDs.Count == 0)) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(steamIDs)));
		}

		SteamFriends steamFriends = bot.SteamFriends;

		List<SteamID> friendsToRemove = [.. GetFriendList(steamFriends).Where(steamIDs.Contains)];

		friendsToRemove.ForEach(steamFriends.RemoveFriend);

		await Task.Delay(1000).ConfigureAwait(false);

		List<SteamID> removedFriends = [.. friendsToRemove.Where(friendID => !GetFriendList(steamFriends).Contains(friendID))];

		ConsoleTable removedFriendsTable = new ConsoleTable(TableHeader).Configure(o => o.EnableCount = false);

		removedFriends.ForEach(friend => removedFriendsTable.AddRow(friend.ConvertToUInt64(), steamFriends.GetFriendPersonaName(friend) ?? string.Empty));

		string result = string.Join(Environment.NewLine, PluginLocale.Strings.FormatBotRemovedFriends(friendsToRemove.Count, removedFriends.Count), removedFriendsTable);

		return bot.Commands.FormatBotResponse(result);
	}

	private static async Task<string?> ResponseRemoveFriend(EAccess access, string botNames, string steamIDsText, ulong steamID = 0) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseRemoveFriend(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot, steamIDsText)))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}

	private static string? ResponseVersion(EAccess access) {
		if (access < EAccess.FamilySharing) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		return Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotVersion, nameof(FriendsManagerPlugin), typeof(FriendsManagerPlugin).Assembly.GetName().Version));
	}
}
