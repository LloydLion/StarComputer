namespace StarComputer.Server.Abstractions
{
	public class ClientApprovalResult
	{
		private readonly Action<string> failtureCallback;
		private readonly Action okCallback;


		public ClientApprovalResult(Action<string> failtureCallback, Action okCallback)
		{
			this.failtureCallback = failtureCallback;
			this.okCallback = okCallback;
		}


		public void CallFailture(string errorMessage) => failtureCallback(errorMessage);

		public void CallOk() => okCallback();
	}
}