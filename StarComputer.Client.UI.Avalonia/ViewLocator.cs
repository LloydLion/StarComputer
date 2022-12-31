﻿using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System;

namespace StarComputer.Client.UI.Avalonia
{
	public class ViewLocator : IDataTemplate
	{
		public IControl Build(object data)
		{
			try
			{
				var name = data.GetType().FullName!.Replace("ViewModel", "View");
				var type = Type.GetType(name);

				if (type is not null)
				{
					return (Control)Activator.CreateInstance(type)!;
				}
				else
				{
					return new TextBlock { Text = "Not Found: " + name };
				}
			}
			catch (Exception ex)
			{
				return new TextBlock { Text = "Error while locating\r\n" + ex };
			}
		}


		public bool Match(object data)
		{
			return data is ViewModelBase;
		}
	}
}
