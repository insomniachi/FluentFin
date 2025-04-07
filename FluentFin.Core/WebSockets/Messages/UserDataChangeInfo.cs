using Jellyfin.Sdk.Generated.Models;
using Microsoft.Kiota.Abstractions.Serialization;

namespace FluentFin.Core.WebSockets.Messages;

public class UserDataChangeInfo : IParsable
{
	public Guid? UserId { get; set; }
	public List<UserItemDataDto> UserDataList { get; set; } = [];

	public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
	{
		return new Dictionary<string, Action<IParseNode>>
		{
			{ "UserId", n => { UserId = n.GetGuidValue(); }},
			{ "UserDataList", n => UserDataList = [.. n.GetCollectionOfObjectValues(UserItemDataDto.CreateFromDiscriminatorValue)]}
		};
	}

	public void Serialize(ISerializationWriter writer)
	{
		_ = writer ?? throw new ArgumentNullException(nameof(writer));
		writer.WriteGuidValue("IsFavorite", UserId);
		writer.WriteCollectionOfObjectValues("UserDataList", UserDataList);
	}
}