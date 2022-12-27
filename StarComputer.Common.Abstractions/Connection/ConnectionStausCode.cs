﻿namespace StarComputer.Common.Abstractions.Connection
{
	public enum ConnectionStausCode
	{
		ProtocolError = 12,
		InvalidPassword = 13,
		Successful = 14,
		NoFreePort = 15,
		ComputerRejected = 16,
		IncompatibleVersion = 17,
		IncompatiblePluginVersion = 18,
	}
}