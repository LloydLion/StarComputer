using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StarComputer.Common.Abstractions.Plugins.Resources;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;

namespace StarComputer.UI.Avalonia
{
	public class HttpLocalServer
	{
		private readonly IResourcesManager resources;
		private readonly ILogger logger;
		private readonly Dictionary<PluginResource, ResourceReplacement> replacements = new();
		private readonly HttpListener listener;
		private bool stopped = false;


		public HttpLocalServer(IResourcesManager resources, ILogger logger, IOptions<Options> options)
		{
			this.resources = resources;
			this.logger = logger;
			listener = new();
			HttpPrefix = string.Concat("http://localhost:", options.Value.Port, "/", options.Value.HttpPrefix, "/");
			listener.Prefixes.Add(HttpPrefix);
		}


		public string HttpPrefix { get; }

		public bool IsListening => listener.IsListening;


		public void Start()
		{
			if (IsListening)
				throw new InvalidOperationException("Server already started");
			if (stopped)
				throw new InvalidOperationException("Server stopped and cannot be started or stopped again");

			listener.Start();
			MainLoop();
		}

		public void Stop()
		{
			if (IsListening == false)
				throw new InvalidOperationException("Server is not listening");
			if (stopped)
				throw new InvalidOperationException("Server stopped and cannot be started or stopped again");

			listener.Stop();
			stopped = true;
		}

		public void ReplaceFile(PluginResource resource, ReadOnlyMemory<byte> fileData, string contentType, string? charset = null)
		{
			replacements.Add(resource, new(fileData, contentType, charset));
		}

		public void ReplaceFile(PluginResource resource, string fileData, string contentType)
		{
			replacements.Add(resource, new(Encoding.UTF8.GetBytes(fileData), contentType, "UTF-8"));
		}

		public void AddGugpage(PluginResource resource)
		{
			var data = "<h1>No HTML data to draw</h1>";
			ReplaceFile(resource, data, "text/html");
		}

		public void CancelFileReplacement(PluginResource resource)
		{
			replacements.Remove(resource);
		}

		private async void MainLoop()
		{
			while (IsListening)
			{
				try
				{
					var context = await listener.GetContextAsync();

					var outStream = context.Response.OutputStream;

					var url = context.Request.Url?.OriginalString;
					if (url is null)
					{
						context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
						outStream.Close();
						continue;
					}

					var resource = new PluginResource(url[HttpPrefix.Length..]);

					if (replacements.TryGetValue(resource, out var memory))
					{
						context.Response.ContentType = memory.ContentType;
						if (memory.Charset is not null)
							context.Response.ContentType += "; charset=" + memory.Charset;

						await outStream.WriteAsync(memory.Bytes);
						outStream.Close();
					}
					else
					{
						var stream = resources.ReadResource(resource);

						var metaResource = new PluginResource(resource.FullPath + ".meta.json");
						if (resources.HasResource(metaResource))
						{
							using var metaStreamReader = new StreamReader(resources.ReadResource(metaResource));
							var text = metaStreamReader.ReadToEnd();
							var meta = JsonConvert.DeserializeObject<ResourceMetaModel>(text);

							if (meta is not null && meta.ContentType is not null)
							{
								context.Response.ContentType = meta.ContentType;
								if (meta.Charset is not null)
									context.Response.ContentType += "; charset=" + meta.Charset;
							}
						}

						await stream.CopyToAsync(outStream);
						outStream.Close();
					}
				}
				catch (Exception ex)
				{
					logger.Log(LogLevel.Error, ex, "!!!");
				}
			}
		}


		public class Options
		{
			public string HttpPrefix { get; set; } = "starComputer";

			public int Port { get; set; } = 7676;
		}

		private record struct ResourceReplacement(ReadOnlyMemory<byte> Bytes, string ContentType, string? Charset);

		private record class ResourceMetaModel([property: JsonPropertyName("contentType")] string? ContentType, [property: JsonPropertyName("charset")] string? Charset);
	}
}
