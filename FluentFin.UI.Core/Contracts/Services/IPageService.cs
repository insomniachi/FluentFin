﻿using System;

namespace FluentFin.Contracts.Services;

public interface IPageService
{
	Type GetPageType(string key);
}
