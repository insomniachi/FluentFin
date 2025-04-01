using System;

namespace FluentFin.Contracts.Services;

public interface IPageService
{
	Type GetPageType(string key);
	Type GetViewModelType(Type type);
	Type? GetParent(Type type);
}
