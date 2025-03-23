using System.Text.Json.Serialization;

namespace FluentFin.Core.WebSockets.Messages;

public abstract class GroupUpdate
{
    [JsonConverter(typeof(JsonGuidConverter))]
    public Guid GroupId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<GroupUpdateType>))]
    public GroupUpdateType Type { get; set; }
}

public class GroupUpdate<T> : GroupUpdate
{
    public T Data { get; set; } = default!;
}

public class PlayQueueUpdate
{
    [JsonConverter(typeof(JsonStringEnumConverter<PlayQueueUpdateReason>))]
    public PlayQueueUpdateReason Reason { get; set; }
    public DateTime LastUpdate { get; set; }
    public IReadOnlyList<SyncPlayQueueItem> Playlist { get; set; } = [];
    public int PlayingItemIndex { get; set; }
    public long StartPositionTicks { get; set; }
    public bool IsPlaying { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<GroupShuffleMode>))]
    public GroupShuffleMode ShuffleMode { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<GroupRepeatMode>))]
    public GroupRepeatMode RepeatMode { get; set; }
}

public class SyncPlayQueueItem
{
    [JsonConverter(typeof(JsonGuidConverter))]
    public Guid ItemId { get; set; }

    [JsonConverter(typeof(JsonGuidConverter))]
    public Guid PlaylistItemId { get; set; }
}

public enum GroupRepeatMode
{
    /// <summary>
    /// Repeat one item only.
    /// </summary>
    RepeatOne = 0,

    /// <summary>
    /// Cycle the playlist.
    /// </summary>
    RepeatAll = 1,

    /// <summary>
    /// Do not repeat.
    /// </summary>
    RepeatNone = 2
}

public enum GroupShuffleMode
{
    /// <summary>
    /// Sorted playlist.
    /// </summary>
    Sorted = 0,

    /// <summary>
    /// Shuffled playlist.
    /// </summary>
    Shuffle = 1
}

public enum PlayQueueUpdateReason
{
    /// <summary>
    /// A user is requesting to play a new playlist.
    /// </summary>
    NewPlaylist = 0,

    /// <summary>
    /// A user is changing the playing item.
    /// </summary>
    SetCurrentItem = 1,

    /// <summary>
    /// A user is removing items from the playlist.
    /// </summary>
    RemoveItems = 2,

    /// <summary>
    /// A user is moving an item in the playlist.
    /// </summary>
    MoveItem = 3,

    /// <summary>
    /// A user is adding items the queue.
    /// </summary>
    Queue = 4,

    /// <summary>
    /// A user is adding items to the queue, after the currently playing item.
    /// </summary>
    QueueNext = 5,

    /// <summary>
    /// A user is requesting the next item in queue.
    /// </summary>
    NextItem = 6,

    /// <summary>
    /// A user is requesting the previous item in queue.
    /// </summary>
    PreviousItem = 7,

    /// <summary>
    /// A user is changing repeat mode.
    /// </summary>
    RepeatMode = 8,

    /// <summary>
    /// A user is changing shuffle mode.
    /// </summary>
    ShuffleMode = 9
}

public enum GroupUpdateType
{
    /// <summary>
    /// The user-joined update. Tells members of a group about a new user.
    /// </summary>
    UserJoined,

    /// <summary>
    /// The user-left update. Tells members of a group that a user left.
    /// </summary>
    UserLeft,

    /// <summary>
    /// The group-joined update. Tells a user that the group has been joined.
    /// </summary>
    GroupJoined,

    /// <summary>
    /// The group-left update. Tells a user that the group has been left.
    /// </summary>
    GroupLeft,

    /// <summary>
    /// The group-state update. Tells members of the group that the state changed.
    /// </summary>
    StateUpdate,

    /// <summary>
    /// The play-queue update. Tells a user the playing queue of the group.
    /// </summary>
    PlayQueue,

    /// <summary>
    /// The not-in-group error. Tells a user that they don't belong to a group.
    /// </summary>
    NotInGroup,

    /// <summary>
    /// The group-does-not-exist error. Sent when trying to join a non-existing group.
    /// </summary>
    GroupDoesNotExist,

    /// <summary>
    /// The create-group-denied error. Sent when a user tries to create a group without required permissions.
    /// </summary>
    CreateGroupDenied,

    /// <summary>
    /// The join-group-denied error. Sent when a user tries to join a group without required permissions.
    /// </summary>
    JoinGroupDenied,

    /// <summary>
    /// The library-access-denied error. Sent when a user tries to join a group without required access to the library.
    /// </summary>
    LibraryAccessDenied
}
