using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core;

public static class DeviceProfiles
{
	public static DeviceProfile Flyleaf { get; } =
		 new DeviceProfile
		 {
			 Name = "Flyleaf",
			 MaxStaticBitrate = 20_000_000,
			 MaxStreamingBitrate = 12_000_000,
			 CodecProfiles = [
				new ()
				{
					Type = CodecProfile_Type.Video,
					Codec = "h264,h265,hevc,mpeg4,divx,xvid,wmv,vc1,vp8,vp9,av1"
				},
				new ()
				{
					Type = CodecProfile_Type.Audio,
					Codec = "aac,ac3,eac3,mp3,flac,alac,opus,vorbis,pcm,wma"
				},
			],
			 DirectPlayProfiles = [
				new ()
				{
					Type = DirectPlayProfile_Type.Video,
					Container = "mp4,mkv,avi,mov,flv,ts,m2ts,webm,ogv,3gp,hls",
					VideoCodec = "h264,hevc,mpeg4,divx,xvid,wmv,vc1,vp8,vp9,av1,avi,mpeg,mpeg2video",
					AudioCodec = "aac,ac3,eac3,mp3,flac,alac,opus,vorbis,wma"
				},
				new ()
				{
					Type = DirectPlayProfile_Type.Audio,
					Container = "mp3,aac,flac,alac,wav,ogg,wma",
					AudioCodec = "mp3,aac,flac,alac,opus,vorbis,wma,pcm,mpa,wav,ogg,oga,webma,ape"
				},
			],
			 TranscodingProfiles = [
				new ()
				{
					Type = TranscodingProfile_Type.Video,
					Context = TranscodingProfile_Context.Streaming,
					Protocol = TranscodingProfile_Protocol.Hls,
					Container = "ts",
					VideoCodec = "h264,hevc",
					AudioCodec = "aac,mp3,ac3",
					CopyTimestamps = false,
					EnableSubtitlesInManifest = true,
				},
				new()
				{
					Type = TranscodingProfile_Type.Audio,
					Context = TranscodingProfile_Context.Streaming,
					Protocol = TranscodingProfile_Protocol.Http,
					Container = "mp3",
					AudioCodec = "mp3",
					MaxAudioChannels = "2",
				}
			],
			 SubtitleProfiles = [
				new () { Format = "vtt", Method = SubtitleProfile_Method.Embed },
				new () { Format = "vtt", Method = SubtitleProfile_Method.Hls },
				new () { Format = "vtt", Method = SubtitleProfile_Method.External },
				new () { Format = "vtt", Method = SubtitleProfile_Method.Encode },

				new () { Format = "webvtt", Method = SubtitleProfile_Method.Embed },
				new () { Format = "webvtt", Method = SubtitleProfile_Method.Hls },
				new () { Format = "webvtt", Method = SubtitleProfile_Method.External },
				new () { Format = "webvtt", Method = SubtitleProfile_Method.Encode },

				new () { Format = "srt", Method = SubtitleProfile_Method.Embed },
				new () { Format = "srt", Method = SubtitleProfile_Method.Hls },
				new () { Format = "srt", Method = SubtitleProfile_Method.External },
				new () { Format = "srt", Method = SubtitleProfile_Method.Encode },

				new () { Format = "subrip", Method = SubtitleProfile_Method.Embed },
				new () { Format = "subrip", Method = SubtitleProfile_Method.Hls },
				new () { Format = "subrip", Method = SubtitleProfile_Method.External },
				new () { Format = "subrip", Method = SubtitleProfile_Method.Encode },

				new () { Format = "ttml", Method = SubtitleProfile_Method.Embed },
				new () { Format = "ttml", Method = SubtitleProfile_Method.Hls },
				new () { Format = "ttml", Method = SubtitleProfile_Method.External },
				new () { Format = "ttml", Method = SubtitleProfile_Method.Encode },

				new () { Format = "dvbsub", Method = SubtitleProfile_Method.Embed },
				new () { Format = "dvbsub", Method = SubtitleProfile_Method.Hls },
				new () { Format = "dvbsub", Method = SubtitleProfile_Method.External },
				new () { Format = "dvbsub", Method = SubtitleProfile_Method.Encode },

				new () { Format = "ass", Method = SubtitleProfile_Method.Embed },
				new () { Format = "ass", Method = SubtitleProfile_Method.Hls },
				new () { Format = "ass", Method = SubtitleProfile_Method.External },
				new () { Format = "ass", Method = SubtitleProfile_Method.Encode },

				new () { Format = "idx", Method = SubtitleProfile_Method.Embed },
				new () { Format = "idx", Method = SubtitleProfile_Method.Hls },
				new () { Format = "idx", Method = SubtitleProfile_Method.External },
				new () { Format = "idx", Method = SubtitleProfile_Method.Encode },

				new () { Format = "pgs", Method = SubtitleProfile_Method.Embed },
				new () { Format = "pgs", Method = SubtitleProfile_Method.Hls },
				new () { Format = "pgs", Method = SubtitleProfile_Method.External },
				new () { Format = "pgs", Method = SubtitleProfile_Method.Encode },

				new () { Format = "pgssub", Method = SubtitleProfile_Method.Embed },
				new () { Format = "pgssub", Method = SubtitleProfile_Method.Hls },
				new () { Format = "pgssub", Method = SubtitleProfile_Method.External },
				new () { Format = "pgssub", Method = SubtitleProfile_Method.Encode },

				new () { Format = "ssa", Method = SubtitleProfile_Method.Embed },
				new () { Format = "ssa", Method = SubtitleProfile_Method.Hls },
				new () { Format = "ssa", Method = SubtitleProfile_Method.External },
				new () { Format = "ssa", Method = SubtitleProfile_Method.Encode },

				new () { Format = "microdvd", Method = SubtitleProfile_Method.Embed },
				new () { Format = "microdvd", Method = SubtitleProfile_Method.Hls },
				new () { Format = "microdvd", Method = SubtitleProfile_Method.External },
				new () { Format = "microdvd", Method = SubtitleProfile_Method.Encode },

				new () { Format = "mov_text", Method = SubtitleProfile_Method.Embed },
				new () { Format = "mov_text", Method = SubtitleProfile_Method.Hls },
				new () { Format = "mov_text", Method = SubtitleProfile_Method.External },
				new () { Format = "mov_text", Method = SubtitleProfile_Method.Encode },

				new () { Format = "mpl2", Method = SubtitleProfile_Method.Embed },
				new () { Format = "mpl2", Method = SubtitleProfile_Method.Hls },
				new () { Format = "mpl2", Method = SubtitleProfile_Method.External },
				new () { Format = "mpl2", Method = SubtitleProfile_Method.Encode },

				new () { Format = "pjs", Method = SubtitleProfile_Method.Embed },
				new () { Format = "pjs", Method = SubtitleProfile_Method.Hls },
				new () { Format = "pjs", Method = SubtitleProfile_Method.External },
				new () { Format = "pjs", Method = SubtitleProfile_Method.Encode },

				new () { Format = "realtext", Method = SubtitleProfile_Method.Embed },
				new () { Format = "realtext", Method = SubtitleProfile_Method.Hls },
				new () { Format = "realtext", Method = SubtitleProfile_Method.External },
				new () { Format = "realtext", Method = SubtitleProfile_Method.Encode },

				new () { Format = "scc", Method = SubtitleProfile_Method.Embed },
				new () { Format = "scc", Method = SubtitleProfile_Method.Hls },
				new () { Format = "scc", Method = SubtitleProfile_Method.External },
				new () { Format = "scc", Method = SubtitleProfile_Method.Encode },

				new () { Format = "smi", Method = SubtitleProfile_Method.Embed },
				new () { Format = "smi", Method = SubtitleProfile_Method.Hls },
				new () { Format = "smi", Method = SubtitleProfile_Method.External },
				new () { Format = "smi", Method = SubtitleProfile_Method.Encode },

				new () { Format = "stl", Method = SubtitleProfile_Method.Embed },
				new () { Format = "stl", Method = SubtitleProfile_Method.Hls },
				new () { Format = "stl", Method = SubtitleProfile_Method.External },
				new () { Format = "stl", Method = SubtitleProfile_Method.Encode },

				new () { Format = "sub", Method = SubtitleProfile_Method.Embed },
				new () { Format = "sub", Method = SubtitleProfile_Method.Hls },
				new () { Format = "sub", Method = SubtitleProfile_Method.External },
				new () { Format = "sub", Method = SubtitleProfile_Method.Encode },

				new () { Format = "subviewer", Method = SubtitleProfile_Method.Embed },
				new () { Format = "subviewer", Method = SubtitleProfile_Method.Hls },
				new () { Format = "subviewer", Method = SubtitleProfile_Method.External },
				new () { Format = "subviewer", Method = SubtitleProfile_Method.Encode },

				new () { Format = "teletext", Method = SubtitleProfile_Method.Embed },
				new () { Format = "teletext", Method = SubtitleProfile_Method.Hls },
				new () { Format = "teletext", Method = SubtitleProfile_Method.External },
				new () { Format = "teletext", Method = SubtitleProfile_Method.Encode },

				new () { Format = "text", Method = SubtitleProfile_Method.Embed },
				new () { Format = "text", Method = SubtitleProfile_Method.Hls },
				new () { Format = "text", Method = SubtitleProfile_Method.External },
				new () { Format = "text", Method = SubtitleProfile_Method.Encode },

				new () { Format = "vplayer", Method = SubtitleProfile_Method.Embed },
				new () { Format = "vplayer", Method = SubtitleProfile_Method.Hls },
				new () { Format = "vplayer", Method = SubtitleProfile_Method.External },
				new () { Format = "vplayer", Method = SubtitleProfile_Method.Encode },

				new () { Format = "xsub", Method = SubtitleProfile_Method.Embed },
				new () { Format = "xsub", Method = SubtitleProfile_Method.Hls },
				new () { Format = "xsub", Method = SubtitleProfile_Method.External },
				new () { Format = "xsub", Method = SubtitleProfile_Method.Encode },
			]
		 };
}
