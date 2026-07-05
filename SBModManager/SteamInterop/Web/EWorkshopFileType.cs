using System;
using System.Collections.Generic;
using System.Text;

namespace SBModManager.SteamInterop.Web {

	/// <summary>
	/// Corresponds to a workshop file type. This is sourced from Valve themselves.
	/// </summary>
	public enum EWorkshopFileType {

		/// <summary>
		/// Normal Workshop item that can be subscribed to.
		/// </summary>
		Community,

		/// <summary>
		/// Workshop item that is meant to be voted on for the purpose of selling in-game. (See: Curated Workshop)
		/// </summary>
		Microtransaction,

		/// <summary>
		/// A collection of Workshop items.
		/// </summary>
		Collection,

		/// <summary>
		/// Artwork
		/// </summary>
		Art,

		/// <summary>
		/// External video
		/// </summary>
		Video,

		/// <summary>
		/// External screenshot
		/// </summary>
		Screenshot,

		/// <summary>
		/// Unused, used to be for Greenlight game entries
		/// </summary>
		Game,

		/// <summary>
		/// Unused, used to be for Greenlight software entries.
		/// </summary>
		Software,

		/// <summary>
		/// Unused, used to be for Greenlight concepts.
		/// </summary>
		Concept,

		/// <summary>
		/// Steam web guide.
		/// </summary>
		WebGuide,

		/// <summary>
		/// Application integrated guide.
		/// </summary>
		IntegratedGuide,

		/// <summary>
		/// Workshop merchandise meant to be voted on for the purpose of being sold.
		/// </summary>
		Merch,

		/// <summary>
		/// Steam Controller bindings.
		/// </summary>
		ControllerBinding,

		/// <summary>
		/// Only used internally in Steam.
		/// </summary>
		SteamworksAccessInvite,

		/// <summary>
		/// Steam video.
		/// </summary>
		SteamVideo,

		/// <summary>
		/// Managed completely by the game, not the user, and not shown on the web.
		/// </summary>
		GameManagedItem,
		
		/// <summary>
		/// Not used in Steam
		/// </summary>
		Error = 0xFF

	}
}
