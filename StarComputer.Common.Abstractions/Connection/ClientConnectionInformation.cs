namespace StarComputer.Common.Abstractions.Connection
{
	public record struct ClientConnectionInformation(string Login, Uri CallbackUri);
}
