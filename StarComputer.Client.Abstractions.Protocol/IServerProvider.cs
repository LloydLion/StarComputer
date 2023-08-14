namespace StarComputer.Client.Abstractions.Protocol.Protocol;

public interface IServerProvider
{
	public Task<IServer> CreateAsync(string address);
}
