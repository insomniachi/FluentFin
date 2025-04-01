using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class BaseItemViewModel : ObservableObject
{
	public BaseItemDto Dto { get; set; } = null!;

	public List<DayOfWeekObject?> AirDays { get; set; } = [];
	public int? AirsAfterSeasonNumber { get; set; }
	public int? AirsBeforeEpisodeNumber { get; set; }
	public int? AirsBeforeSeasonNumber { get; set; }
	public string AirTime { get; set; } = "";
	public string Album { get; set; } = "";
	public string AlbumArtist { get; set; } = "";
	public List<NameGuidPair> AlbumArtists { get; set; } = [];
	public int? AlbumCount { get; set; }
	public Guid? AlbumId { get; set; }
	public string AlbumPrimaryImageTag { get; set; } = "";
	public double? Altitude { get; set; }
	public double? Aperture { get; set; }
	public int? ArtistCount { get; set; }
	public List<NameGuidPair> ArtistItems { get; set; } = [];
	public List<string> Artists { get; set; } = [];
	public string AspectRatio { get; set; } = "";
	public BaseItemDto_Audio? Audio { get; set; }
	public List<string> BackdropImageTags { get; set; } = [];
	public string CameraMake { get; set; } = "";
	public string CameraModel { get; set; } = "";
	public bool? CanDelete { get; set; }
	public bool? CanDownload { get; set; }
	public Guid? ChannelId { get; set; }
	public string ChannelName { get; set; } = "";
	public string ChannelNumber { get; set; } = "";
	public string ChannelPrimaryImageTag { get; set; } = "";
	public BaseItemDto_ChannelType? ChannelType { get; set; }
	public List<ChapterInfo> Chapters { get; set; } = [];
	public int? ChildCount { get; set; }
	public BaseItemDto_CollectionType? CollectionType { get; set; }
	public float? CommunityRating { get; set; }
	public double? CompletionPercentage { get; set; }
	public string Container { get; set; } = "";
	public float? CriticRating { get; set; }
	public long? CumulativeRunTimeTicks { get; set; }
	public BaseItemDto? CurrentProgram { get; set; }
	public string CustomRating { get; set; } = "";
	public DateTimeOffset? DateCreated { get; set; }
	public DateTimeOffset? DateLastMediaAdded { get; set; }
	public string DisplayOrder { get; set; } = "";
	public string DisplayPreferencesId { get; set; } = "";
	public bool? EnableMediaSourceDisplay { get; set; }
	public DateTimeOffset? EndDate { get; set; }
	public int? EpisodeCount { get; set; }
	public string EpisodeTitle { get; set; } = "";
	public string Etag { get; set; } = "";
	public double? ExposureTime { get; set; }
	public List<ExternalUrl> ExternalUrls { get; set; } = [];
	public BaseItemDto_ExtraType? ExtraType { get; set; }
	public double? FocalLength { get; set; }
	public string ForcedSortName { get; set; } = "";
	public List<NameGuidPair> GenreItems { get; set; } = [];
	public List<string> Genres { get; set; } = [];
	public bool? HasLyrics { get; set; }
	public bool? HasSubtitles { get; set; }
	public int? Height { get; set; }
	public Guid? Id { get; set; }
	public BaseItemDto_ImageBlurHashes? ImageBlurHashes { get; set; }
	public BaseItemDto_ImageOrientation? ImageOrientation { get; set; }
	public BaseItemDto_ImageTags? ImageTags { get; set; }
	public int? IndexNumber { get; set; }
	public int? IndexNumberEnd { get; set; }
	public bool? IsFolder { get; set; }
	public bool? IsHD { get; set; }
	public bool? IsKids { get; set; }
	public bool? IsLive { get; set; }
	public bool? IsMovie { get; set; }
	public bool? IsNews { get; set; }
	public int? IsoSpeedRating { get; set; }
	public BaseItemDto_IsoType? IsoType { get; set; }
	public bool? IsPlaceHolder { get; set; }
	public bool? IsPremiere { get; set; }
	public bool? IsRepeat { get; set; }
	public bool? IsSeries { get; set; }
	public bool? IsSports { get; set; }
	public double? Latitude { get; set; }
	public int? LocalTrailerCount { get; set; }
	public BaseItemDto_LocationType? LocationType { get; set; }
	public bool? LockData { get; set; }
	public List<MetadataField?> LockedFields { get; set; } = [];
	public double? Longitude { get; set; }
	public int? MediaSourceCount { get; set; }
	public List<MediaSourceInfo> MediaSources { get; set; } = [];
	public List<MediaStream> MediaStreams { get; set; } = [];
	public BaseItemDto_MediaType? MediaType { get; set; }
	public int? MovieCount { get; set; }
	public int? MusicVideoCount { get; set; }
	public string Name { get; set; } = "";
	public float? NormalizationGain { get; set; }
	public string Number { get; set; } = "";
	public string OfficialRating { get; set; } = "";
	public string OriginalTitle { get; set; } = "";
	public string Overview { get; set; } = "";
	public string ParentArtImageTag { get; set; } = "";
	public Guid? ParentArtItemId { get; set; }
	public List<string> ParentBackdropImageTags { get; set; } = [];
	public Guid? ParentBackdropItemId { get; set; }
	public Guid? ParentId { get; set; }
	public int? ParentIndexNumber { get; set; }
	public string ParentLogoImageTag { get; set; } = "";
	public Guid? ParentLogoItemId { get; set; }
	public string ParentPrimaryImageItemId { get; set; } = "";
	public string ParentPrimaryImageTag { get; set; } = "";
	public string ParentThumbImageTag { get; set; } = "";
	public Guid? ParentThumbItemId { get; set; }
	public int? PartCount { get; set; }
	public string Path { get; set; } = "";
	public List<BaseItemPerson> People { get; set; } = [];
	public BaseItemDto_PlayAccess? PlayAccess { get; set; }
	public string PlaylistItemId { get; set; } = "";
	public string PreferredMetadataCountryCode { get; set; } = "";
	public string PreferredMetadataLanguage { get; set; } = "";
	public DateTimeOffset? PremiereDate { get; set; }
	public double? PrimaryImageAspectRatio { get; set; }
	public List<string> ProductionLocations { get; set; } = [];
	public int? ProductionYear { get; set; }
	public int? ProgramCount { get; set; }
	public string ProgramId { get; set; } = "";
	public BaseItemDto_ProviderIds? ProviderIds { get; set; }
	public int? RecursiveItemCount { get; set; }
	public List<MediaUrl> RemoteTrailers { get; set; } = [];
	public long? RunTimeTicks { get; set; }
	public List<string> ScreenshotImageTags { get; set; } = [];
	public Guid? SeasonId { get; set; }
	public string SeasonName { get; set; } = "";
	public int? SeriesCount { get; set; }
	public Guid? SeriesId { get; set; }
	public string SeriesName { get; set; } = "";
	public string SeriesPrimaryImageTag { get; set; } = "";
	public string SeriesStudio { get; set; } = "";
	public string SeriesThumbImageTag { get; set; } = "";
	public string SeriesTimerId { get; set; } = "";
	public string ServerId { get; set; } = "";
	public double? ShutterSpeed { get; set; }
	public string Software { get; set; } = "";
	public int? SongCount { get; set; }
	public string SortName { get; set; } = "";
	public string SourceType { get; set; } = "";
	public int? SpecialFeatureCount { get; set; }
	public DateTimeOffset? StartDate { get; set; }
	public string Status { get; set; } = "";
	public List<NameGuidPair> Studios { get; set; } = [];
	public List<string> Taglines { get; set; } = [];
	public List<string> Tags { get; set; } = [];
	public string TimerId { get; set; } = "";
	public int? TrailerCount { get; set; }
	public BaseItemDto_Trickplay? Trickplay { get; set; }
	public BaseItemDto_Type? Type { get; set; }

	[ObservableProperty]
	public partial UserItemDataViewModel? UserData { get; set; }
	public BaseItemDto_Video3DFormat? Video3DFormat { get; set; }
	public BaseItemDto_VideoType? VideoType { get; set; }
	public int? Width { get; set; }

	public static BaseItemViewModel FromDto(BaseItemDto dto)
	{
		return new BaseItemViewModel
		{
			Dto = dto,
			AirDays = [.. dto.AirDays ?? []],
			AirsAfterSeasonNumber = dto.AirsAfterSeasonNumber,
			AirsBeforeEpisodeNumber = dto.AirsBeforeEpisodeNumber,
			AirsBeforeSeasonNumber = dto.AirsBeforeSeasonNumber,
			AirTime = dto.AirTime ?? "",
			Album = dto.Album ?? "",
			AlbumArtist = dto.AlbumArtist ?? "",
			AlbumArtists = [.. dto.AlbumArtists ?? []],
			AlbumCount = dto.AlbumCount,
			AlbumId = dto.AlbumId,
			AlbumPrimaryImageTag = dto.AlbumPrimaryImageTag ?? "",
			Altitude = dto.Altitude,
			Aperture = dto.Aperture,
			ArtistCount = dto.ArtistCount,
			ArtistItems = [.. dto.ArtistItems ?? []],
			Artists = [.. dto.Artists ?? []],
			AspectRatio = dto.AspectRatio ?? "",
			Audio = dto.Audio,
			BackdropImageTags = [.. dto.BackdropImageTags ?? []],
			CameraMake = dto.CameraMake ?? "",
			CameraModel = dto.CameraModel ?? "",
			CanDelete = dto.CanDelete,
			CanDownload = dto.CanDownload,
			ChannelId = dto.ChannelId,
			ChannelName = dto.ChannelName ?? "",
			ChannelNumber = dto.ChannelNumber ?? "",
			ChannelPrimaryImageTag = dto.ChannelPrimaryImageTag ?? "",
			ChannelType = dto.ChannelType,
			Chapters = [.. dto.Chapters ?? []],
			ChildCount = dto.ChildCount,
			CollectionType = dto.CollectionType,
			CommunityRating = dto.CommunityRating,
			CompletionPercentage = dto.CompletionPercentage,
			Container = dto.Container ?? "",
			CriticRating = dto.CriticRating,
			CumulativeRunTimeTicks = dto.CumulativeRunTimeTicks,
			CurrentProgram = dto.CurrentProgram,
			CustomRating = dto.CustomRating ?? "",
			DateCreated = dto.DateCreated,
			DateLastMediaAdded = dto.DateLastMediaAdded,
			DisplayOrder = dto.DisplayOrder ?? "",
			DisplayPreferencesId = dto.DisplayPreferencesId ?? "",
			EnableMediaSourceDisplay = dto.EnableMediaSourceDisplay,
			EndDate = dto.EndDate,
			EpisodeCount = dto.EpisodeCount,
			EpisodeTitle = dto.EpisodeTitle ?? "",
			Etag = dto.Etag ?? "",
			ExposureTime = dto.ExposureTime,
			ExternalUrls = [.. dto.ExternalUrls ?? []],
			ExtraType = dto.ExtraType,
			FocalLength = dto.FocalLength,
			ForcedSortName = dto.ForcedSortName ?? "",
			GenreItems = [.. dto.GenreItems ?? []],
			Genres = [.. dto.Genres ?? []],
			HasLyrics = dto.HasLyrics,
			HasSubtitles = dto.HasSubtitles,
			Height = dto.Height,
			Id = dto.Id,
			ImageBlurHashes = dto.ImageBlurHashes,
			ImageOrientation = dto.ImageOrientation,
			ImageTags = dto.ImageTags,
			IndexNumber = dto.IndexNumber,
			IndexNumberEnd = dto.IndexNumberEnd,
			IsFolder = dto.IsFolder,
			IsHD = dto.IsHD,
			IsKids = dto.IsKids,
			IsLive = dto.IsLive,
			IsMovie = dto.IsMovie,
			IsNews = dto.IsNews,
			IsoSpeedRating = dto.IsoSpeedRating,
			IsoType = dto.IsoType,
			IsPlaceHolder = dto.IsPlaceHolder,
			IsPremiere = dto.IsPremiere,
			IsRepeat = dto.IsRepeat,
			IsSeries = dto.IsSeries,
			IsSports = dto.IsSports,
			Latitude = dto.Latitude,
			LocalTrailerCount = dto.LocalTrailerCount,
			LocationType = dto.LocationType,
			LockData = dto.LockData,
			LockedFields = [.. dto.LockedFields ?? []],
			Longitude = dto.Longitude,
			MediaSourceCount = dto.MediaSourceCount,
			MediaSources = [.. dto.MediaSources ?? []],
			MediaStreams = [.. dto.MediaStreams ?? []],
			MediaType = dto.MediaType,
			MovieCount = dto.MovieCount,
			MusicVideoCount = dto.MusicVideoCount,
			Name = dto.Name ?? "",
			NormalizationGain = dto.NormalizationGain,
			Number = dto.Number ?? "",
			OfficialRating = dto.OfficialRating ?? "",
			OriginalTitle = dto.OriginalTitle ?? "",
			Overview = dto.Overview ?? "",
			ParentArtImageTag = dto.ParentArtImageTag ?? "",
			ParentArtItemId = dto.ParentArtItemId,
			ParentBackdropImageTags = [.. dto.ParentBackdropImageTags ?? []],
			ParentBackdropItemId = dto.ParentBackdropItemId,
			ParentId = dto.ParentId,
			ParentIndexNumber = dto.ParentIndexNumber,
			ParentLogoImageTag = dto.ParentLogoImageTag ?? "",
			ParentLogoItemId = dto.ParentLogoItemId,
			ParentPrimaryImageItemId = dto.ParentPrimaryImageItemId ?? "",
			ParentPrimaryImageTag = dto.ParentPrimaryImageTag ?? "",
			ParentThumbImageTag = dto.ParentThumbImageTag ?? "",
			ParentThumbItemId = dto.ParentThumbItemId,
			PartCount = dto.PartCount,
			Path = dto.Path ?? "",
			People = [.. dto.People ?? []],
			PlayAccess = dto.PlayAccess,
			PlaylistItemId = dto.PlaylistItemId ?? "",
			PreferredMetadataCountryCode = dto.PreferredMetadataCountryCode ?? "",
			PreferredMetadataLanguage = dto.PreferredMetadataLanguage ?? "",
			PremiereDate = dto.PremiereDate,
			PrimaryImageAspectRatio = dto.PrimaryImageAspectRatio,
			ProductionLocations = [.. dto.ProductionLocations ?? []],
			ProductionYear = dto.ProductionYear,
			ProgramCount = dto.ProgramCount,
			ProgramId = dto.ProgramId ?? "",
			ProviderIds = dto.ProviderIds,
			RecursiveItemCount = dto.RecursiveItemCount,
			RemoteTrailers = [.. dto.RemoteTrailers ?? []],
			RunTimeTicks = dto.RunTimeTicks,
			ScreenshotImageTags = [.. dto.ScreenshotImageTags ?? []],
			SeasonId = dto.SeasonId,
			SeasonName = dto.SeasonName ?? "",
			SeriesCount = dto.SeriesCount,
			SeriesId = dto.SeriesId,
			SeriesName = dto.SeriesName ?? "",
			SeriesPrimaryImageTag = dto.SeriesPrimaryImageTag ?? "",
			SeriesStudio = dto.SeriesStudio ?? "",
			SeriesThumbImageTag = dto.SeriesThumbImageTag ?? "",
			SeriesTimerId = dto.SeriesTimerId ?? "",
			ServerId = dto.ServerId ?? "",
			ShutterSpeed = dto.ShutterSpeed,
			Software = dto.Software ?? "",
			SongCount = dto.SongCount,
			SortName = dto.SortName ?? "",
			SourceType = dto.SourceType ?? "",
			SpecialFeatureCount = dto.SpecialFeatureCount,
			StartDate = dto.StartDate,
			Status = dto.Status ?? "",
			Studios = [.. dto.Studios ?? []],
			Taglines = [.. dto.Taglines ?? []],
			Tags = [.. dto.Tags ?? []],
			TimerId = dto.TimerId ?? "",
			TrailerCount = dto.TrailerCount,
			Trickplay = dto.Trickplay,
			Type = dto.Type,
			UserData = UserItemDataViewModel.FromDto(dto.UserData),
			Video3DFormat = dto.Video3DFormat,
			VideoType = dto.VideoType,
			Width = dto.Width
		};
	}
}

public partial class UserItemDataViewModel : ObservableObject
{
	public UserItemDataDto Dto { get; set; } = null!;

	[ObservableProperty]
	public partial bool? IsFavorite { get; set; }

	[ObservableProperty]
	public partial bool? Played { get; set; }

	public Guid? ItemId { get; set; }
	public string? Key { get; set; }
	public DateTimeOffset? LastPlayedDate { get; set; }
	public bool? Likes { get; set; }
	public long? PlaybackPositionTicks { get; set; }
	public int? PlayCount { get; set; }
	public double? PlayedPercentage { get; set; }
	public double? Rating { get; set; }
	public int? UnplayedItemCount { get; set; }

	public static UserItemDataViewModel? FromDto(UserItemDataDto? dto)
	{
		if (dto is null)
		{
			return null;
		}

		return new UserItemDataViewModel
		{
			Dto = dto,
			IsFavorite = dto.IsFavorite,
			ItemId = dto.ItemId,
			Key = dto.Key,
			LastPlayedDate = dto.LastPlayedDate,
			Likes = dto.Likes,
			PlaybackPositionTicks = dto.PlaybackPositionTicks,
			PlayCount = dto.PlayCount,
			Played = dto.Played,
			PlayedPercentage = dto.PlayedPercentage,
			Rating = dto.Rating,
			UnplayedItemCount = dto.UnplayedItemCount
		};
	}
}
