using Jellyfin.Sdk.Generated.Models;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using Windows.Media.Core;
using Windows.Media.Devices;
using Windows.Media.Render;

namespace FluentFin;

public sealed class WmpDeviceProfileManager
{
	public static DeviceProfile Profile { get; private set; } = null!;

	// This logic is adapted from the web client's browserDeviceProfile.js
	public static async Task InitializeAsync()
	{
		CodecQuery codecQuery = new();

		HashSet<string> videoCodecGuids = new(StringComparer.OrdinalIgnoreCase);

		// For some reason querying video codecs without a specific subtype on Xbox results in an Access Violation.
		// So just query for each specific codec we care about.
		string[] subtypes = [
			CodecSubtypes.VideoFormatHevc.ToString(),
			CodecSubtypes.VideoFormatH264.ToString(),
			CodecSubtypes.VideoFormatMpeg2.ToString(),
			CodecSubtypes.VideoFormatWvc1.ToString(),
			CodecSubtypes.VideoFormatVP80.ToString(),
			CodecSubtypes.VideoFormatVP90.ToString(),
		];
		foreach (string subtype in subtypes)
		{
			foreach (CodecInfo codecInfo in await codecQuery.FindAllAsync(CodecKind.Video, CodecCategory.Decoder, subtype))
			{
				foreach (string subType in codecInfo.Subtypes)
				{
					videoCodecGuids.Add(subType);
				}
			}
		}

		HashSet<string> audioCodecGuids = new(StringComparer.OrdinalIgnoreCase);
		foreach (CodecInfo codecInfo in await codecQuery.FindAllAsync(CodecKind.Audio, CodecCategory.Decoder, string.Empty))
		{
			foreach (string subType in codecInfo.Subtypes)
			{
				audioCodecGuids.Add(subType);
			}
		}

		const int maxBitrate = 120_000_000;
		uint audioChannelCount = await GetAudioChannelCountAsync();

		List<string> webmAudioCodecs = ["vorbis"];

		bool canPlayMkv = true; // Can we just assume true? UWP supports it generally.

		DeviceProfile profile = new()
		{
			MaxStreamingBitrate = maxBitrate,
			MaxStaticBitrate = 100_000_000,
			MusicStreamingTranscodingBitrate = Math.Min(maxBitrate, 384_000),
			DirectPlayProfiles = [],
			TranscodingProfiles = [],
			ContainerProfiles = [],
			CodecProfiles = [],
			SubtitleProfiles = [],
		};

		List<string> videoAudioCodecs = [];
		List<string> hlsInTsVideoAudioCodecs = [];
		List<string> hlsInFmp4VideoAudioCodecs = [];

		// TODO: Check codecs (any):
		// video/mp4; codecs="avc1.640029, mp4a.69
		// video/mp4; codecs="avc1.640029, mp4a.6B"
		// video/mp4; codecs="avc1.640029, mp3"
		bool supportsMp3VideoAudio = true;

		// Xbox always renders at 1920 x 1080
		// TODO: For genericism should this be detected anyway?
		int maxVideoWidth = 1920;

		// TODO: Check codecs
		bool canPlayAacVideoAudio = true; // 'video/mp4; codecs="avc1.640029, mp4a.40.2"
		bool canPlayMp3VideoAudioInHls = true; // 'application/x-mpegurl; codecs="avc1.64001E, mp4a.40.34"'
		bool canPlayAc3VideoAudio = true; // 'audio/mp4; codecs="ac-3"'
		bool canPlayEac3VideoAudio = true; // 'audio/mp4; codecs="ec-3"'
		bool canPlayAc3VideoAudioInHls = true; // application/x-mpegurl; codecs="avc1.42E01E, ac-3

		// Transcoding codec is the first in hlsVideoAudioCodecs.
		// Prefer AAC, MP3 to other codecs when audio transcoding.
		if (canPlayAacVideoAudio)
		{
			videoAudioCodecs.Add("aac");
			hlsInTsVideoAudioCodecs.Add("aac");
			hlsInFmp4VideoAudioCodecs.Add("aac");
		}

		if (supportsMp3VideoAudio)
		{
			videoAudioCodecs.Add("mp3");
			hlsInTsVideoAudioCodecs.Add("mp3");
		}

		if (canPlayMp3VideoAudioInHls)
		{
			hlsInFmp4VideoAudioCodecs.Add("mp3");
		}

		// For AC3/EAC3 remuxing.
		// Do not use AC3 for audio transcoding unless AAC and MP3 are not supported.
		if (canPlayAc3VideoAudio)
		{
			videoAudioCodecs.Add("ac3");

			if (canPlayEac3VideoAudio)
			{
				videoAudioCodecs.Add("eac3");
			}

			if (canPlayAc3VideoAudioInHls)
			{
				hlsInTsVideoAudioCodecs.Add("ac3");
				hlsInFmp4VideoAudioCodecs.Add("ac3");

				if (canPlayEac3VideoAudio)
				{
					hlsInTsVideoAudioCodecs.Add("eac3");
					hlsInFmp4VideoAudioCodecs.Add("eac3");
				}
			}
		}

		bool supportsMp2VideoAudio = true;
		if (supportsMp2VideoAudio)
		{
			videoAudioCodecs.Add("mp2");
			hlsInTsVideoAudioCodecs.Add("mp2");
			hlsInFmp4VideoAudioCodecs.Add("mp2");
		}

		// TODO: Check user setting: enableDts
		bool supportsDts = audioCodecGuids.Contains(CodecSubtypes.AudioFormatDts);
		if (supportsDts)
		{
			videoAudioCodecs.Add("dca");
			videoAudioCodecs.Add("dts");
		}

		// TODO: Check user setting: enableTrueHd
		videoAudioCodecs.Add("truehd");

		bool canPlayOpus = audioCodecGuids.Contains(CodecSubtypes.AudioFormatOpus);
		if (canPlayOpus)
		{
			videoAudioCodecs.Add("opus");
			webmAudioCodecs.Add("opus");
			hlsInFmp4VideoAudioCodecs.Add("opus");
		}

		bool canPlayFlac = audioCodecGuids.Contains(CodecSubtypes.AudioFormatFlac);
		if (canPlayFlac)
		{
			videoAudioCodecs.Add("flac");
			hlsInFmp4VideoAudioCodecs.Add("flac");
		}

		bool canPlayAlac = audioCodecGuids.Contains(CodecSubtypes.AudioFormatAlac);
		if (canPlayAlac)
		{
			videoAudioCodecs.Add("alac");
			hlsInFmp4VideoAudioCodecs.Add("alac");
		}

		List<string> mp4VideoCodecs = [];
		List<string> webmVideoCodecs = [];
		List<string> hlsInTsVideoCodecs = [];
		List<string> hlsInFmp4VideoCodecs = [];

		// av1 main level 5.3
		// TODO: Check codec: 'video/mp4; codecs="av01.0.15M.08"' or 'video/mp4; codecs="av01.0.15M.10"'
		bool canPlayAv1 = true;
		if (canPlayAv1)
		{
			// disable av1 on non-safari mobile browsers since it can be very slow software decoding
			hlsInFmp4VideoCodecs.Add("av1");
		}

		bool canPlayHevc = videoCodecGuids.Contains(CodecSubtypes.VideoFormatHevc);
		if (canPlayHevc)
		{
			hlsInFmp4VideoCodecs.Add("hevc");
		}

		bool canPlayH264 = videoCodecGuids.Contains(CodecSubtypes.VideoFormatH264);
		if (canPlayH264)
		{
			mp4VideoCodecs.Add("h264");
			hlsInTsVideoCodecs.Add("h264");
			hlsInFmp4VideoCodecs.Add("h264");
		}

		if (canPlayHevc)
		{
			mp4VideoCodecs.Add("hevc");
		}

		bool supportsMpeg2Video = videoCodecGuids.Contains(CodecSubtypes.VideoFormatMpeg2);
		if (supportsMpeg2Video)
		{
			mp4VideoCodecs.Add("mpeg2video");
		}

		bool supportsVc1 = videoCodecGuids.Contains(CodecSubtypes.VideoFormatWvc1);
		if (supportsVc1)
		{
			mp4VideoCodecs.Add("vc1");
		}

		bool canPlayVp8 = videoCodecGuids.Contains(CodecSubtypes.VideoFormatVP80);
		if (canPlayVp8)
		{
			webmVideoCodecs.Add("vp8");
		}

		bool canPlayVp9 = videoCodecGuids.Contains(CodecSubtypes.VideoFormatVP90);
		if (canPlayVp9)
		{
			mp4VideoCodecs.Add("vp9");
			hlsInFmp4VideoCodecs.Add("vp9");
			webmVideoCodecs.Add("vp9");
		}

		if (canPlayAv1)
		{
			mp4VideoCodecs.Add("av1");
			webmVideoCodecs.Add("av1");
		}

		if (canPlayVp8)
		{
			videoAudioCodecs.Add("vorbis");
		}

		if (webmVideoCodecs.Count > 0)
		{
			profile.DirectPlayProfiles.Add(
				new DirectPlayProfile
				{
					Container = "webm",
					Type = DirectPlayProfile_Type.Video,
					VideoCodec = string.Join(',', webmVideoCodecs),
					AudioCodec = string.Join(',', webmAudioCodecs),
				});
		}

		if (mp4VideoCodecs.Count > 0)
		{
			profile.DirectPlayProfiles.Add(
				new DirectPlayProfile
				{
					Container = "mp4,m4v",
					Type = DirectPlayProfile_Type.Video,
					VideoCodec = string.Join(',', mp4VideoCodecs),
					AudioCodec = string.Join(',', videoAudioCodecs),
				});
		}

		if (canPlayMkv && mp4VideoCodecs.Count > 0)
		{
			profile.DirectPlayProfiles.Add(
				new DirectPlayProfile
				{
					Container = "mkv",
					Type = DirectPlayProfile_Type.Video,
					VideoCodec = string.Join(',', mp4VideoCodecs),
					AudioCodec = string.Join(',', videoAudioCodecs),
				});
		}

		// m2ts container
		{
			List<string> videoCodecs = ["h264"];

			if (supportsVc1)
			{
				videoCodecs.Add("vc1");
			}

			if (supportsMpeg2Video)
			{
				videoCodecs.Add("mpeg2video");
			}

			profile.DirectPlayProfiles.Add(
				new DirectPlayProfile
				{
					Container = "m2ts",
					Type = DirectPlayProfile_Type.Video,
					VideoCodec = string.Join(',', videoCodecs),
					AudioCodec = string.Join(',', videoAudioCodecs),
				});
		}

		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "wmv",
				Type = DirectPlayProfile_Type.Video,
			});

		// ts container
		{
			List<string> videoCodecs = ["h264"];

			if (supportsVc1)
			{
				videoCodecs.Add("vc1");
			}

			if (supportsMpeg2Video)
			{
				videoCodecs.Add("mpeg2video");
			}

			profile.DirectPlayProfiles.Add(
				new DirectPlayProfile
				{
					Container = "ts,mpegts",
					Type = DirectPlayProfile_Type.Video,
					VideoCodec = string.Join(',', videoCodecs),
					AudioCodec = string.Join(',', videoAudioCodecs),
				});
		}

		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "asf",
				Type = DirectPlayProfile_Type.Video
			});
		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "avi",
				Type = DirectPlayProfile_Type.Video,
				AudioCodec = string.Join(',', videoAudioCodecs),
			});
		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "mpg",
				Type = DirectPlayProfile_Type.Video,
				AudioCodec = string.Join(',', videoAudioCodecs),
			});
		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "mpeg",
				Type = DirectPlayProfile_Type.Video,
				AudioCodec = string.Join(',', videoAudioCodecs),
			});
		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "mov",
				Type = DirectPlayProfile_Type.Video,
				VideoCodec = "h264",
				AudioCodec = string.Join(',', videoAudioCodecs),
			});

		if (canPlayOpus)
		{
			profile.DirectPlayProfiles.Add(
				new DirectPlayProfile
				{
					Container = "webm",
					Type = DirectPlayProfile_Type.Audio,
					AudioCodec = "opus",
				});
		}

		bool canPlayMp3 = audioCodecGuids.Contains(CodecSubtypes.AudioFormatMP3);
		if (canPlayMp3)
		{
			if (!canPlayMp3VideoAudioInHls)
			{
				profile.DirectPlayProfiles.Add(
					new DirectPlayProfile
					{
						Container = "ts",
						Type = DirectPlayProfile_Type.Audio,
						AudioCodec = "mp3",
					});
			}
		}

		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "mp2",
				Type = DirectPlayProfile_Type.Audio,
			});

		bool canPlayAac = audioCodecGuids.Contains(CodecSubtypes.AudioFormatAac);
		if (canPlayAac)
		{
			profile.DirectPlayProfiles.Add(
				new DirectPlayProfile
				{
					Container = "aac",
					Type = DirectPlayProfile_Type.Audio,
				});
			profile.DirectPlayProfiles.Add(
				new DirectPlayProfile
				{
					Container = "m4a",
					Type = DirectPlayProfile_Type.Audio,
					AudioCodec = "aac",
				});
			profile.DirectPlayProfiles.Add(
				new DirectPlayProfile
				{
					Container = "m4b",
					Type = DirectPlayProfile_Type.Audio,
					AudioCodec = "aac",
				});
		}

		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "flac",
				Type = DirectPlayProfile_Type.Audio,
			});
		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "alac",
				Type = DirectPlayProfile_Type.Audio,
			});
		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "m4a",
				Type = DirectPlayProfile_Type.Audio,
				AudioCodec = "alac",
			});
		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "m4b",
				Type = DirectPlayProfile_Type.Audio,
				AudioCodec = "alac",
			});
		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "webma",
				Type = DirectPlayProfile_Type.Audio,
			});
		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "webm",
				Type = DirectPlayProfile_Type.Audio,
				AudioCodec = "webma",
			});
		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "wma",
				Type = DirectPlayProfile_Type.Audio,
			});
		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "wav",
				Type = DirectPlayProfile_Type.Audio,
			});
		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "ogg",
				Type = DirectPlayProfile_Type.Audio,
			});
		profile.DirectPlayProfiles.Add(
			new DirectPlayProfile
			{
				Container = "oga",
				Type = DirectPlayProfile_Type.Audio,
			});

		bool hlsBreakOnNonKeyFrames = false;
		bool enableFmp4Hls = true; // TODO: check user setting: preferFmp4HlsContainer

		profile.TranscodingProfiles.Add(
			new TranscodingProfile
			{
				Container = enableFmp4Hls ? "mp4" : "ts",
				Type = TranscodingProfile_Type.Audio,
				AudioCodec = "aac",
				Context = TranscodingProfile_Context.Streaming,
				Protocol = TranscodingProfile_Protocol.Hls,
				MaxAudioChannels = audioChannelCount.ToString(),
				MinSegments = 1,
				BreakOnNonKeyFrames = hlsBreakOnNonKeyFrames,
				EnableAudioVbrEncoding = false // TODO: ??? !appSettings.disableVbrAudio()
			});

		// For streaming, prioritize opus transcoding after mp3/aac. It is too problematic with random failures
		// But for static (offline sync), it will be just fine.
		// Prioritize aac higher because the encoder can accept more channels than mp3
		if (canPlayAac)
		{
			profile.TranscodingProfiles.Add(new TranscodingProfile
			{
				Container = "aac",
				Type = TranscodingProfile_Type.Audio,
				AudioCodec = "aac",
				Context = TranscodingProfile_Context.Streaming,
				Protocol = TranscodingProfile_Protocol.Http,
				MaxAudioChannels = audioChannelCount.ToString(),
			});
		}

		if (canPlayMp3)
		{
			profile.TranscodingProfiles.Add(new TranscodingProfile
			{
				Container = "mp3",
				Type = TranscodingProfile_Type.Audio,
				AudioCodec = "mp3",
				Context = TranscodingProfile_Context.Streaming,
				Protocol = TranscodingProfile_Protocol.Http,
				MaxAudioChannels = audioChannelCount.ToString(),
			});
		}

		if (canPlayOpus)
		{
			profile.TranscodingProfiles.Add(new TranscodingProfile
			{
				Container = "opus",
				Type = TranscodingProfile_Type.Audio,
				AudioCodec = "opus",
				Context = TranscodingProfile_Context.Streaming,
				Protocol = TranscodingProfile_Protocol.Http,
				MaxAudioChannels = audioChannelCount.ToString(),
			});
		}

		profile.TranscodingProfiles.Add(new TranscodingProfile
		{
			Container = "wav",
			Type = TranscodingProfile_Type.Audio,
			AudioCodec = "wav",
			Context = TranscodingProfile_Context.Streaming,
			Protocol = TranscodingProfile_Protocol.Http,
			MaxAudioChannels = audioChannelCount.ToString(),
		});

		if (canPlayOpus)
		{
			profile.TranscodingProfiles.Add(new TranscodingProfile
			{
				Container = "opus",
				Type = TranscodingProfile_Type.Audio,
				AudioCodec = "opus",
				Context = TranscodingProfile_Context.Static,
				Protocol = TranscodingProfile_Protocol.Http,
				MaxAudioChannels = audioChannelCount.ToString(),
			});
		}

		if (canPlayMp3)
		{
			profile.TranscodingProfiles.Add(new TranscodingProfile
			{
				Container = "mp3",
				Type = TranscodingProfile_Type.Audio,
				AudioCodec = "mp3",
				Context = TranscodingProfile_Context.Static,
				Protocol = TranscodingProfile_Protocol.Http,
				MaxAudioChannels = audioChannelCount.ToString(),
			});
		}

		if (canPlayAac)
		{
			profile.TranscodingProfiles.Add(new TranscodingProfile
			{
				Container = "aac",
				Type = TranscodingProfile_Type.Audio,
				AudioCodec = "aac",
				Context = TranscodingProfile_Context.Static,
				Protocol = TranscodingProfile_Protocol.Http,
				MaxAudioChannels = audioChannelCount.ToString(),
			});
		}

		profile.TranscodingProfiles.Add(new TranscodingProfile
		{
			Container = "wav",
			Type = TranscodingProfile_Type.Audio,
			AudioCodec = "wav",
			Context = TranscodingProfile_Context.Static,
			Protocol = TranscodingProfile_Protocol.Http,
			MaxAudioChannels = audioChannelCount.ToString(),
		});

		if (hlsInFmp4VideoCodecs.Count > 0 && hlsInFmp4VideoAudioCodecs.Count > 0 && enableFmp4Hls)
		{
			// HACK: Since there is no filter for TS/MP4 in the API, specify HLS support in general and rely on retry after DirectPlay error
			// FIXME: Need support for {Container = "mp4", Protocol: "hls"} or {Container = "hls", SubContainer = "mp4"}
			profile.DirectPlayProfiles.Add(new DirectPlayProfile
			{
				Container = "hls",
				Type = DirectPlayProfile_Type.Video,
				VideoCodec = string.Join(',', hlsInFmp4VideoCodecs),
				AudioCodec = string.Join(',', hlsInFmp4VideoAudioCodecs)
			});

			profile.TranscodingProfiles.Add(new TranscodingProfile
			{
				Container = "mp4",
				Type = TranscodingProfile_Type.Video,
				AudioCodec = string.Join(',', hlsInFmp4VideoAudioCodecs),
				VideoCodec = string.Join(',', hlsInFmp4VideoCodecs),
				Context = TranscodingProfile_Context.Streaming,
				Protocol = TranscodingProfile_Protocol.Hls,
				MaxAudioChannels = audioChannelCount.ToString(),
				MinSegments = 1,
				BreakOnNonKeyFrames = hlsBreakOnNonKeyFrames,
			});
		}

		List<ProfileCondition> aacCodecProfileConditions = [];

		// Handle he-aac not supported
		bool heAacSupported = true; // TODO: Check codec 'video/mp4; codecs="avc1.640029, mp4a.40.5"'
		if (!heAacSupported)
		{
			// TODO: This needs to become part of the stream url in order to prevent stream copy
			aacCodecProfileConditions.Add(
				new ProfileCondition
				{
					Condition = ProfileCondition_Condition.NotEquals,
					Property = ProfileCondition_Property.AudioProfile,
					Value = "HE-AAC",
				});
		}

		bool supportsSecondaryAudio = false; // TODO
		if (!supportsSecondaryAudio)
		{
			aacCodecProfileConditions.Add(
				new ProfileCondition
				{
					Condition = ProfileCondition_Condition.Equals,
					Property = ProfileCondition_Property.IsSecondaryAudio,
					Value = "false",
					IsRequired = false
				});
		}

		if (aacCodecProfileConditions.Count > 0)
		{
			profile.CodecProfiles.Add(
				new CodecProfile
				{
					Type = CodecProfile_Type.VideoAudio,
					Codec = "aac",
					Conditions = aacCodecProfileConditions
				});
		}

		List<ProfileCondition> globalAudioCodecProfileConditions = [];
		List<ProfileCondition> globalVideoAudioCodecProfileConditions = [];

		// TODO: Check user settings allowedAudioChannels
		/*

        if (parseInt(userSettings.allowedAudioChannels(), 10) > 0) {
            globalAudioCodecProfileConditions.Add({
                Condition = ProfileCondition_Condition.LessThanEqual',
                Property = ProfileCondition_Property.AudioChannels',
                Value = audioChannelCount,
                IsRequired = false
            });

            globalVideoAudioCodecProfileConditions.Add({
                Condition = ProfileCondition_Condition.LessThanEqual',
                Property = ProfileCondition_Property.AudioChannels',
                Value = audioChannelCount,
                IsRequired = false
            });
        }
        */

		if (!supportsSecondaryAudio)
		{
			globalVideoAudioCodecProfileConditions.Add(
				new ProfileCondition
				{
					Condition = ProfileCondition_Condition.Equals,
					Property = ProfileCondition_Property.IsSecondaryAudio,
					Value = "false",
					IsRequired = false
				});
		}

		if (globalAudioCodecProfileConditions.Count > 0)
		{
			profile.CodecProfiles.Add(
				new CodecProfile
				{
					Type = CodecProfile_Type.Audio,
					Conditions = globalAudioCodecProfileConditions
				});
		}

		if (globalVideoAudioCodecProfileConditions.Count > 0)
		{
			profile.CodecProfiles.Add(
				new CodecProfile
				{
					Type = CodecProfile_Type.VideoAudio,
					Conditions = globalVideoAudioCodecProfileConditions
				});
		}

		int maxH264Level = 42;
		string h264Profiles = "high|main|baseline|constrained baseline";

		/* TODO: Handle these scenarios
        if (canPlayType('video/mp4; codecs="avc1.640833"'))
        {
            maxH264Level = 51;
        }

        // Support H264 Level 52 (Tizen 5.0) - app only
        if (canPlayType('video/mp4; codecs="avc1.640834"'))
        {
            maxH264Level = 52;
        }

        if (canPlayType('video/mp4; codecs="avc1.6e0033"')
            // These tests are passing in safari, but playback is failing
            && !browser.edge
        ) {
            h264Profiles += '|high 10';
        }
        */

		int maxHevcLevel = 120;
		string hevcProfiles = "main";

		/* TODO: Handle these scenarios
        // hevc main level 4.1
        if (canPlayType('video/mp4; codecs="hvc1.1.4.L123"')
                || canPlayType('video/mp4; codecs="hev1.1.4.L123"')) {
            maxHevcLevel = 123;
        }

        // hevc main10 level 4.1
        if (canPlayType('video/mp4; codecs="hvc1.2.4.L123"')
                || canPlayType('video/mp4; codecs="hev1.2.4.L123"')) {
            maxHevcLevel = 123;
            hevcProfiles = 'main|main 10';
        }

        // hevc main10 level 5.1
        if (canPlayType('video/mp4; codecs="hvc1.2.4.L153"')
                || canPlayType('video/mp4; codecs="hev1.2.4.L153"')) {
            maxHevcLevel = 153;
            hevcProfiles = 'main|main 10';
        }

        // hevc main10 level 6.1
        if (canPlayType('video/mp4; codecs="hvc1.2.4.L183"')
                || canPlayType('video/mp4; codecs="hev1.2.4.L183"')) {
            maxHevcLevel = 183;
            hevcProfiles = 'main|main 10';
        }
        */

		int maxAv1Level = 15; // level 5.3
		string av1Profiles = "main"; // av1 main covers 4:2:0 8 & 10 bits

		/* TODO: Handle these scenarios
        // av1 main level 6.0
        if (videoTestElement.canPlayType('video/mp4; codecs="av01.0.16M.08"')
                && videoTestElement.canPlayType('video/mp4; codecs="av01.0.16M.10"')) {
            maxAv1Level = 16;
        }

        // av1 main level 6.1
        if (videoTestElement.canPlayType('video/mp4; codecs="av01.0.17M.08"')
                && videoTestElement.canPlayType('video/mp4; codecs="av01.0.17M.10"')) {
            maxAv1Level = 17;
        }

        // av1 main level 6.2
        if (videoTestElement.canPlayType('video/mp4; codecs="av01.0.18M.08"')
                && videoTestElement.canPlayType('video/mp4; codecs="av01.0.18M.10"')) {
            maxAv1Level = 18;
        }

        // av1 main level 6.3
        if (videoTestElement.canPlayType('video/mp4; codecs="av01.0.19M.08"')
                && videoTestElement.canPlayType('video/mp4; codecs="av01.0.19M.10"')) {
            maxAv1Level = 19;
        }
        */

		string h264VideoRangeTypes = "SDR";
		string hevcVideoRangeTypes = "SDR";
		string vp9VideoRangeTypes = "SDR";
		string av1VideoRangeTypes = "SDR";

		bool supportsHdr10 = true; // TODO: Check
		if (supportsHdr10)
		{
			hevcVideoRangeTypes += "|HDR10";
			vp9VideoRangeTypes += "|HDR10";
			av1VideoRangeTypes += "|HDR10";
		}

		bool supportsHlg = supportsHdr10; // TODO: Check
		if (supportsHlg)
		{
			hevcVideoRangeTypes += "|HLG";
			vp9VideoRangeTypes += "|HLG";
			av1VideoRangeTypes += "|HLG";
		}

		bool supportsDolbyVision = true; // TODO: Check
		if (supportsDolbyVision)
		{
			hevcVideoRangeTypes += "|DOVI";
			hevcVideoRangeTypes += "|DOVIWithHDR10|DOVIWithHLG|DOVIWithSDR";

			// Profile 10 4k@24fps
			av1VideoRangeTypes += "|DOVI|DOVIWithHDR10|DOVIWithHLG|DOVIWithSDR";
		}

		List<ProfileCondition> h264CodecProfileConditions =
		[
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.NotEquals,
				Property = ProfileCondition_Property.IsAnamorphic,
				Value = "true",
				IsRequired = false
			},
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.EqualsAny,
				Property = ProfileCondition_Property.VideoProfile,
				Value = h264Profiles,
				IsRequired = false
			},
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.EqualsAny,
				Property = ProfileCondition_Property.VideoRangeType,
				Value = h264VideoRangeTypes,
				IsRequired = false
			},
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.LessThanEqual,
				Property = ProfileCondition_Property.VideoLevel,
				Value = maxH264Level.ToString(),
				IsRequired = false
			}
		];

		List<ProfileCondition> hevcCodecProfileConditions =
		[
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.NotEquals,
				Property = ProfileCondition_Property.IsAnamorphic,
				Value = "true",
				IsRequired = false
			},
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.EqualsAny,
				Property = ProfileCondition_Property.VideoProfile,
				Value = hevcProfiles,
				IsRequired = false
			},
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.EqualsAny,
				Property = ProfileCondition_Property.VideoRangeType,
				Value = hevcVideoRangeTypes,
				IsRequired = false
			},
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.LessThanEqual,
				Property = ProfileCondition_Property.VideoLevel,
				Value = maxHevcLevel.ToString(),
				IsRequired = false
			}
		];

		List<ProfileCondition> vp9CodecProfileConditions =
		[
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.EqualsAny,
				Property = ProfileCondition_Property.VideoRangeType,
				Value = vp9VideoRangeTypes,
				IsRequired = false
			}
		];

		List<ProfileCondition> av1CodecProfileConditions =
		[
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.NotEquals,
				Property = ProfileCondition_Property.IsAnamorphic,
				Value = "true",
				IsRequired = false
			},
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.EqualsAny,
				Property = ProfileCondition_Property.VideoProfile,
				Value = av1Profiles,
				IsRequired = false
			},
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.EqualsAny,
				Property = ProfileCondition_Property.VideoRangeType,
				Value = av1VideoRangeTypes,
				IsRequired = false
			},
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.LessThanEqual,
				Property = ProfileCondition_Property.VideoLevel,
				Value = maxAv1Level.ToString(),
				IsRequired = false
			}
		];

		/* TODO: This needed?
        if (!browser.edgeUwp) {
            h264CodecProfileConditions.Add(
                new ProfileCondition
                {
                    Condition = ProfileCondition_Condition.NotEquals,
                    Property = ProfileCondition_Property.IsInterlaced,
                    Value = "true",
                    IsRequired = false
                });

            hevcCodecProfileConditions.Add(
                new ProfileCondition
                {
                    Condition = ProfileCondition_Condition.NotEquals,
                    Property = ProfileCondition_Property.IsInterlaced,
                    Value = "true",
                    IsRequired = false
                });
        }
        */

		h264CodecProfileConditions.Add(
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.LessThanEqual,
				Property = ProfileCondition_Property.Width,
				Value = maxVideoWidth.ToString(),
				IsRequired = false
			});

		hevcCodecProfileConditions.Add(
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.LessThanEqual,
				Property = ProfileCondition_Property.Width,
				Value = maxVideoWidth.ToString(),
				IsRequired = false
			});

		av1CodecProfileConditions.Add(
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.LessThanEqual,
				Property = ProfileCondition_Property.Width,
				Value = maxVideoWidth.ToString(),
				IsRequired = false
			});

		profile.CodecProfiles.Add(
			new CodecProfile
			{
				Type = CodecProfile_Type.Video,
				Codec = "h264",
				Conditions = h264CodecProfileConditions,
			});

		profile.CodecProfiles.Add(
			new CodecProfile
			{
				Type = CodecProfile_Type.Video,
				Codec = "hevc",
				Conditions = hevcCodecProfileConditions,
			});

		profile.CodecProfiles.Add(
			new CodecProfile
			{
				Type = CodecProfile_Type.Video,
				Codec = "vp9",
				Conditions = vp9CodecProfileConditions,
			});

		profile.CodecProfiles.Add(
			new CodecProfile
			{
				Type = CodecProfile_Type.Video,
				Codec = "av1",
				Conditions = av1CodecProfileConditions,
			});

		List<ProfileCondition> globalVideoConditions = [];

		globalVideoConditions.Add(
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.LessThanEqual,
				Property = ProfileCondition_Property.VideoBitrate,
				Value = maxBitrate.ToString(),
			});

		globalVideoConditions.Add(
			new ProfileCondition
			{
				Condition = ProfileCondition_Condition.LessThanEqual,
				Property = ProfileCondition_Property.Width,
				Value = maxVideoWidth.ToString(),
				IsRequired = false,
			});

		if (globalVideoConditions.Count > 0)
		{
			profile.CodecProfiles.Add(
				new CodecProfile
				{
					Type = CodecProfile_Type.Video,
					Conditions = globalVideoConditions,
				});
		}

		// Subtitle profiles
		// External vtt or burn in
		string subtitleBurninSetting = ""; // TODO: appSettings.get("subtitleburnin");
		if (subtitleBurninSetting != "all")
		{
			profile.SubtitleProfiles.Add(
				new SubtitleProfile
				{
					Format = "subrip",
					Method = SubtitleProfile_Method.External,
				});

			bool supportsTextTracks = false; // TODO: Check
			if (supportsTextTracks)
			{
				profile.SubtitleProfiles.Add(
					new SubtitleProfile
					{
						Format = "vtt",
						Method = SubtitleProfile_Method.External,
					});
			}

			bool enableSsaRender = false; // TODO: Check settings
			if (!enableSsaRender && subtitleBurninSetting != "allcomplexformats")
			{
				profile.SubtitleProfiles.Add(
					new SubtitleProfile
					{
						Format = "ass",
						Method = SubtitleProfile_Method.External
					});
				profile.SubtitleProfiles.Add(
					new SubtitleProfile
					{
						Format = "ssa",
						Method = SubtitleProfile_Method.External
					});
			}

			// TODO: Can we overlay an image on top?
			/*
            if (options.enablePgsRender !== false
                && appSettings.get("subtitlerenderpgs") == "true"
                && subtitleBurninSetting != "allcomplexformats"
                && subtitleBurninSetting != "onlyimageformats")
            {
                profile.SubtitleProfiles.Add(
                    new SubtitleProfile
                    {
                        Format = "pgssub",
                        Method = SubtitleProfile_Method.External
                    });
            }
            */
		}

		Profile = profile;
	}

	private static async Task<uint> GetAudioChannelCountAsync()
	{
		string defaultAudioRenderDeviceId = MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default);
		DeviceInformation defaultAudioDevice = await DeviceInformation.CreateFromIdAsync(defaultAudioRenderDeviceId);

		AudioGraphSettings settings = new(AudioRenderCategory.Media)
		{
			PrimaryRenderDevice = defaultAudioDevice
		};
		CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

		if (result.Status != AudioGraphCreationStatus.Success)
		{
			// Audio graph creation failed. Just default to 2.
			return 2;
		}

		return result.Graph.EncodingProperties.ChannelCount;
	}
}
