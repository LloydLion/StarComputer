﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StarComputer.UI.Avalonia
{
	public class ViewModelBase : INotifyPropertyChanged, IDisposable
	{
		public event PropertyChangedEventHandler? PropertyChanged;


		protected bool RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = "Name of caller member")
		{
			if (EqualityComparer<T>.Default.Equals(field, value) == false)
			{
				field = value;
				RaisePropertyChanged(propertyName);
				return true;
			}

			return false;
		}

		protected void RaisePropertyChanged([CallerMemberName] string propertyName = "Name of caller member")
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		void IDisposable.Dispose() { GC.SuppressFinalize(this); }

		public virtual void Dispose() { }
	}
}
