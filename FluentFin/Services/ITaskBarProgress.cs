namespace FluentFin.Services;

public interface ITaskBarProgress
{
	void Clear();
	void SetProgressPercent(int percent);
}