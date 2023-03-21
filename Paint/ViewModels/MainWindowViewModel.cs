using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using ReactiveUI;
using Paint.Models;
using Paint.Views;
using System.Collections.ObjectModel;
using System.Reactive;

namespace Paint.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {

        private int _shaperN;
        private readonly Mapper _map;
        private readonly Canvas _canv;
        private UserControl _content;
        
        private readonly UserControl[] _contentArray = new UserControl[] {
            new Shape1_UserControl(),
            new Shape2_UserControl(),
            new Shape3_UserControl(),
            new Shape4_UserControl(),
            new Shape5_UserControl(),
            new Shape6_UserControl()
        };
        private IBrush _addColor = Brushes.White;
        public IBrush AddColor { get => _addColor; set => this.RaiseAndSetIfChanged(ref _addColor, value); }

        private void Update()
        {
            bool valid = _map.ValidInput();
            bool valid2 = _map.ValidName();

            AddColor = valid ? valid2 ? Brushes.Lime : Brushes.Yellow : Brushes.Pink;
            ShapeNameColor = valid2 ? Brushes.Lime : Brushes.Yellow;

            if (_map.newName != null)
            {
                var name = _map.newName;
                _map.newName = null;
                ShapeName = name;
            }

            var select = _map.select_shaper;
            if (select != -1)
            {
                _map.select_shaper = -1;
                if (select == _shaperN) SelectedShaper = select == 0 ? 1 : 0;
                SelectedShaper = select;
            }
        }
        private static void Update(object? inst)
        {
            if (inst != null && inst is MainWindowViewModel @mwvm) @mwvm.Update();
        }

        public MainWindowViewModel(MainWindow mw)
        {
            _content = _contentArray[0];
            _map = new(Update, this);
            _canv = mw.Find<Canvas>("canvas");
            Update();

            Add = ReactiveCommand.Create<Unit, Unit>(_ => { FuncAdd(); return new Unit(); });
            Clear = ReactiveCommand.Create<Unit, Unit>(_ => { FuncClear(); return new Unit(); });
            Export = ReactiveCommand.Create<string, Unit>(n => { FuncExport(n); return new Unit(); });
            Import = ReactiveCommand.Create<string, Unit>(n => { FuncImport(n); return new Unit(); });
        }

        public int SelectedShaper
        {
            get => _shaperN;
            set { this.RaiseAndSetIfChanged(ref _shaperN, value); _map.ChangeFigure(value); Content = _contentArray[value]; }
        }

        public UserControl Content
        {
            get => _content;
            set => this.RaiseAndSetIfChanged(ref _content, value);
        }

        private void FuncAdd()
        {
            Shape? newy = _map.Create(false);
            if (newy == null) return;

            _canv.Children.Add(newy);
            Update();
        }
        private void FuncClear() => _map.Clear();
        private void FuncExport(string type)
        {
            if (type == "PNG")
            {
                ServiceVisible = false;
                Utils.RenderToFile(_canv, "../../../Export.png");
                ServiceVisible = true;
            }
            else _map.Export(type == "XML");
        }
        private void FuncImport(string type)
        {
            Shape[]? beginners = _map.Import(type == "XML");
            if (beginners == null) return;

            foreach (var beginner in beginners) _canv.Children.Add(beginner);
            Update();
        }

        public ReactiveCommand<Unit, Unit> Clear { get; }
        public ReactiveCommand<string, Unit> Export { get; }
        public ReactiveCommand<string, Unit> Import { get; }
        public ReactiveCommand<Unit, Unit> Add { get; }
        

        public void ShapeTap(string name)
        {
            var item = _map.ShapeTap(name);
            this.RaiseAndSetIfChanged(ref _curShape, item, nameof(SelectedShape));
        }

        private IBrush _nameColor = Brushes.White;
        public string ShapeName { get => _map.shapeName; set { this.RaiseAndSetIfChanged(ref _map.shapeName, value); Update(); } }
        public IBrush ShapeNameColor { get => _nameColor; set => this.RaiseAndSetIfChanged(ref _nameColor, value); }

        public string ShapeColor { get => _map.shapeColor; set { this.RaiseAndSetIfChanged(ref _map.shapeColor, value); Update(); } }
        public string ShapeFillColor { get => _map.shapeFillColor; set { this.RaiseAndSetIfChanged(ref _map.shapeFillColor, value); Update(); } }
        public int ShapeThickness { get => _map.shapeThickness; set { this.RaiseAndSetIfChanged(ref _map.shapeThickness, value); Update(); } }

        public SafeNum ShapeWidth => _map.shapeWidth;
        public SafeNum ShapeHeight => _map.shapeHeight;
        public SafeNum ShapeHorizDiagonal => _map.shapeHorizDiagonal;
        public SafeNum ShapeVertDiagonal => _map.shapeVertDiagonal;

        public SafePoint ShapeStartDot => _map.shapeStartDot;
        public SafePoint ShapeEndDot => _map.shapeEndDot;
        public SafePoint ShapeCenterDot => _map.shapeCenterDot;
        public SafePoints ShapeDots => _map.shapeDots;

        public SafeGeometry ShapeCommands => _map.shapeCommands;

        private readonly static string[] Colors = new[] {
            "Yellow", "Blue", "Green", "Red",
            "Orange", "Brown", "Pink", "Aqua",
            "Lime",
            "White", "LightGray", "DarkGray", "Black"
        };
        public static string[] ColorsArr { get => Colors; }

        public ObservableCollection<ShapeListBoxItem> Shapes { get => _map.shapes; }

        private bool _serviceVisible = true;
        public bool ServiceVisible { get => _serviceVisible; set => this.RaiseAndSetIfChanged(ref _serviceVisible, value); }

        private ShapeListBoxItem? _curShape;
        public ShapeListBoxItem? SelectedShape
        {
            get => _curShape;
            set { this.RaiseAndSetIfChanged(ref _curShape, value); _map.Select(value); }
        }
    }
}