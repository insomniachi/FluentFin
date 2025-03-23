using System.Text.Json.Serialization;

namespace FluentFin.Core.WebSockets.Messages
{
    public class SyncPlaySendCommand
    {
        [JsonConverter(typeof(JsonGuidConverter))]
        public Guid GroupId { get; set; }

        [JsonConverter(typeof(JsonGuidConverter))]
        public Guid PlaylistItemId { get; set; }

        public DateTime When { get; set; }

        public long? PositionTicks { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter<SendCommandType>))]
        public SendCommandType Command { get; set; }

        public DateTime EmittedAt { get; set; }
    }

    public enum SendCommandType
    {
        /// <summary>
        /// The unpause command. Instructs users to unpause playback.
        /// </summary>
        Unpause = 0,

        /// <summary>
        /// The pause command. Instructs users to pause playback.
        /// </summary>
        Pause = 1,

        /// <summary>
        /// The stop command. Instructs users to stop playback.
        /// </summary>
        Stop = 2,

        /// <summary>
        /// The seek command. Instructs users to seek to a specified time.
        /// </summary>
        Seek = 3
    }
}
