namespace StarComputer.Client.Abstractions;

public interface IServerProvider
{
	public Task<IServer> CreateAsync(string address);
}
