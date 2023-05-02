using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace StarComputer.UI.Avalonia
{
	public partial class Sidebar : UserControl
	{
		public Orientation Orientation { get; set; }

		public bool Mirror { get; set; }

		public bool ShowCloseButton { get; set; }


		public Sidebar()
		{
			DataContext = this;
			InitializeComponent();

			Initialized += OnInitialized;
		}


		private void OnInitialized(object? sender, EventArgs e)
		{
			if (Orientation == Orientation.Horizontal)
			{
				var tmpList = new List<RowDefinition>
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(new GridLength(300, GridUnitType.Pixel))
				};

				if (Mirror)
				{
					tmpList.Reverse();
					resizeBar.SetValue(Grid.RowProperty, 1);
				}
				else mainDock.SetValue(Grid.RowProperty, 1);

				mainGrid.RowDefinitions.AddRange(tmpList);
			}
			else
			{
				var tmpList = new List<ColumnDefinition>
				{
					new ColumnDefinition(GridLength.Auto),
					new ColumnDefinition(new GridLength(300, GridUnitType.Pixel))
				};

				if (Mirror)
				{
					tmpList.Reverse();
					resizeBar.SetValue(Grid.ColumnProperty, 1);
				}
				else mainDock.SetValue(Grid.ColumnProperty, 1);

				mainGrid.ColumnDefinitions.AddRange(tmpList);
			}
		}

		private void OnPointerMoved(object? sender, PointerEventArgs e)
		{
			var point = e.GetCurrentPoint(this);
			if (point.Properties.IsLeftButtonPressed)
			{
				if (Orientation == Orientation.Horizontal)
				{

				}
				else
				{

				}
			}
		}
	}
}
