using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Drawing = System.Drawing;
//using ValveControlSystem;
using System.Runtime.InteropServices;

namespace FloatableUserControl
{
    public class FloatableUserControl : UserControl
    {
        private Grid _gridContainer = new Grid();
        private DockPanel _titlePanel;
        private Button _btnClose;
        private Button _btnMenuDown;
        private Button _btnMaximize;
        private Button _btnRestore;
        private Rectangle _rectTitle;
        private MenuItem _miFloat;
        private MenuItem _miDock;
        private TextBlock _txtTitle;
        private Window _winFloat;
        private UserControlState _state = UserControlState.Dock;
        private bool _thisMoveable = false;
        private double _originalWidth;
        private double _originalHeight;
        private GridUnitType _originalHeightType;
        private GridUnitType _originalWidthType;
        private Grid _gridParent;
        private int _rowOfThis;
        private int _columnOfThis;
        private Point _pointInRectangle;
        private StackPanel _stpOperation;
        private Border _borderOfThis;
        public delegate void ClosedEventHandler();
        public event ClosedEventHandler Closed;
        public static readonly DependencyProperty StrTitleProperty =
            DependencyProperty.Register("StrTitle", typeof(string), typeof(FloatableUserControl));

        public Grid GridContainer
        {
            get
            {
                return _gridContainer;
            }

            set
            {
                _gridContainer = value;
            }
        }

        public string StrTitle
        {
            get
            {
                return (string)this.GetValue(StrTitleProperty);
            }
            set
            {
                this.SetValue(StrTitleProperty, value);
            }
        }

        public UserControlState State
        {
            get
            {
                return _state;
            }

            set
            {
                _state = value;
            }
        }

        public FloatableUserControl()
        {
            //主Grid有两行。
            Grid mainGrid = new Grid();
            RowDefinition firstRowDefinition = new RowDefinition();
            RowDefinition secondRowDefinition = new RowDefinition();
            firstRowDefinition.Height = new GridLength(22);
            secondRowDefinition.Height = new GridLength(1, GridUnitType.Star);
            mainGrid.RowDefinitions.Add(firstRowDefinition);
            mainGrid.RowDefinitions.Add(secondRowDefinition);

            //标题栏面板
            _titlePanel = new DockPanel();
            _titlePanel.Background = new SolidColorBrush(Colors.White);
            _titlePanel.Focusable = true;
            _stpOperation = new StackPanel();//标题栏右侧操作面板
            _rectTitle = new Rectangle();
            _rectTitle.Fill = new SolidColorBrush(Colors.Red);
            _rectTitle.Opacity = 0;
            Grid gridTitle = new Grid();
            this.SizeChanged += FloatableUserControl_SizeChanged;
            _rectTitle.MouseMove += RectTitle_MouseMove;
            _rectTitle.PreviewMouseLeftButtonDown += RectTitle_PreviewMouseLeftButtonDown;
            _rectTitle.PreviewMouseRightButtonDown += RectTitle_PreviewMouseRightButtonDown;
            _rectTitle.MouseLeave += RectTitle_MouseLeave;
            _rectTitle.PreviewMouseLeftButtonUp += RectTitle_PreviewMouseLeftButtonUp;
            _rectTitle.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            _rectTitle.SetValue(VerticalAlignmentProperty, VerticalAlignment.Stretch);
            _miFloat = new MenuItem();
            _miDock = new MenuItem();
            _miFloat.Click += MiFloat_Click;
            _miDock.Click += MiDock_Click;
            _miDock.IsEnabled = false;
            _rectTitle.ContextMenu = new ContextMenu();
            _rectTitle.ContextMenu.Items.Add(_miFloat);
            _rectTitle.ContextMenu.Items.Add(_miDock);
            _rectTitle.Focusable = true;
            _rectTitle.GotFocus += FloatableUserControl_GotFocus;
            _rectTitle.LostFocus += FloatableUserControl_LostFocus;
            _stpOperation.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Right);
            _stpOperation.SetValue(MarginProperty, new Thickness(0, 0, 5, 0));
            _stpOperation.Orientation = Orientation.Horizontal;

            //标题栏标题
            _txtTitle = new TextBlock();
            //_txtTitle.Background = new SolidColorBrush(Colors.LightGreen);
            _txtTitle.Opacity = 1;
            _txtTitle.Margin = new Thickness(5, 0, 0, 0);
            _txtTitle.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            gridTitle.Children.Add(_txtTitle);
            gridTitle.Children.Add(_rectTitle);
            _titlePanel.Children.Add(gridTitle);
            _titlePanel.Children.Add(_stpOperation);
            _stpOperation.SetValue(Panel.ZIndexProperty, 100);

            //关闭按钮
            Geometry geoClose = Geometry.Parse("M13.46,12L19,17.54V19H17.54L12,13.46L6.46,19H5V17.54L10.54,12L5,6.46V5H6.46L12,10.54L17.54,5H19V6.46L13.46,12Z");
            Path closePath = new Path() { Data = geoClose };
            closePath.Fill = new SolidColorBrush(Colors.Black);
            _btnClose = new Button();
            _btnClose.Content = closePath;
            _btnClose.Background = new SolidColorBrush(Colors.Transparent);
            _btnClose.BorderThickness = new Thickness(0);
            _btnClose.Height = 15;
            _btnClose.Width = 15;
            _btnClose.MouseEnter += Button_MouseEnter;
            _btnClose.MouseLeave += Button_MouseLeave;
            _btnClose.Click += BtnClose_Click;
            closePath.Height = 11;
            closePath.Width = 11;
            closePath.Stretch = Stretch.Fill;
            closePath.SetValue(MarginProperty, new Thickness(0));
            closePath.SetValue(PaddingProperty, new Thickness(0));
            closePath.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            closePath.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);

            //下拉菜单按钮
            Geometry geoMenuDown = Geometry.Parse("M7,10L12,15L17,10H7Z");
            Path menuDownPath = new Path() { Data = geoMenuDown };
            menuDownPath.Fill = new SolidColorBrush(Colors.Black);
            menuDownPath.Height = 6;
            menuDownPath.Width = 11;
            menuDownPath.Stretch = Stretch.Fill;

            _btnMenuDown = new Button();
            _btnMenuDown.Content = menuDownPath;
            _btnMenuDown.Background = new SolidColorBrush(Colors.Transparent);
            _btnMenuDown.Height = 15;
            _btnMenuDown.Width = 15;
            _btnMenuDown.BorderThickness = new Thickness(0);
            _btnMenuDown.MouseEnter += Button_MouseEnter;
            _btnMenuDown.MouseLeave += Button_MouseLeave;
            _btnMenuDown.Click += BtnMenuDown_Click;

            //Path closePath = Application.Current.TryFindResource("WindowClose") as Path;
            _stpOperation.Children.Add(_btnMenuDown);
            _stpOperation.Children.Add(_btnClose);
            this.GotFocus += FloatableUserControl_GotFocus;
            this.LostFocus += FloatableUserControl_LostFocus;
            this.Loaded += FloatableUserControl_Loaded;
            //_titlePanel.Background = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xf2, 0x9d));
            mainGrid.Children.Add(_titlePanel);
            _titlePanel.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Stretch);

            _borderOfThis = new Border();
            mainGrid.Children.Add(_borderOfThis);
            _borderOfThis.BorderThickness = new Thickness(0);
            _borderOfThis.BorderBrush = new SolidColorBrush(Colors.LightBlue);
            _borderOfThis.SetValue(Grid.RowSpanProperty, 2);
            _borderOfThis.MouseMove += DisplayResizeCursor;
            _borderOfThis.PreviewMouseDown += Resize;

            mainGrid.Children.Add(GridContainer);
            GridContainer.SetValue(Grid.RowProperty, 1);

            this.Content = mainGrid;
        }

        public void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (this.State == UserControlState.Float)
            {
                enableDockAndCloseWin();
                removeWin();
            }
            else if (this.State == UserControlState.Dock)
            {
                removeControlAndZeroGrid();
            }

            _borderOfThis.BorderThickness = new Thickness(0);
            if (Closed != null)
            {
                Closed();
            }
            this.State = UserControlState.Closed;
        }

        private void RectTitle_MouseLeave(object sender, MouseEventArgs e)
        {
            _thisMoveable = false;
        }

        private void RectTitle_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed && _thisMoveable)
                {
                    if (this.State == UserControlState.Dock)
                    {
                        MiFloat_Click(sender, e);
                    }
                    _winFloat.DragMove();
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void MiDock_Click(object sender, RoutedEventArgs e)
        {
            if (this.State == UserControlState.Float)
            {
                this.State = UserControlState.Dock;
                _borderOfThis.BorderThickness = new Thickness(0);
                enableDockAndCloseWin();
                AddControlAndSetGrid();
                removeWin();
                bool rectFocusResult = _rectTitle.Focus();
                GridContainer.Margin = new Thickness(0);//将标题下边的控件边框距离改成0
            }
        }

        private void MiFloat_Click(object sender, RoutedEventArgs e)
        {
            this.State = UserControlState.Float;
            _miDock.IsEnabled = true;
            _miFloat.IsEnabled = false;
            _winFloat = new Window();
            _winFloat.Width = this.ActualWidth;//边框长度为8，左右两个。当_winFloat.AllowsTransparency = true;设置了之后不存在边框。
            _winFloat.Height = this.ActualHeight;//边框长度为8，上下两个。
            //_originalWidth = this.ActualWidth;
            //_originalHeight = this.ActualHeight;
            getOriginalHeightAndWidth();
            GridContainer.Margin = new Thickness(2,0,2,2);//将标题下边的控件边框距离改成2

            HwndSource mainWinHwnd = (HwndSource.FromDependencyObject(this) as HwndSource);
            //MainWindow mainWin = mainWinHwnd.RootVisual as MainWindow;
            //if (mainWin != null)
            //{
            //    mainWin.ChildrenWindow.Add(_winFloat);
            //}
            //添加最大化按钮。
            Geometry geoMaximize = Geometry.Parse("M4,4H20V20H4V4M6,8V18H18V8H6Z");
            Path maximizePath = new Path() { Data = geoMaximize };
            maximizePath.Fill = new SolidColorBrush(Colors.Black);
            _btnMaximize = new Button();
            _btnMaximize.Content = maximizePath;
            _btnMaximize.Background = new SolidColorBrush(Colors.Transparent);
            _btnMaximize.BorderThickness = new Thickness(0);
            _btnMaximize.Height = 15;
            _btnMaximize.Width = 15;
            _btnMaximize.MouseEnter += Button_MouseEnter;
            _btnMaximize.MouseLeave += Button_MouseLeave;
            _btnMaximize.Click += BtnMaximize_Click;
            maximizePath.Height = 11;
            maximizePath.Width = 11;
            maximizePath.Stretch = Stretch.Fill;
            _stpOperation.Children.Insert(1, _btnMaximize);

            removeControlAndZeroGrid();

            _winFloat.Content = this;
            _winFloat.WindowStyle = WindowStyle.None;
            _winFloat.AllowsTransparency = true;
            _winFloat.WindowState = WindowState.Normal;
            _winFloat.ShowInTaskbar = true;
            _winFloat.WindowStartupLocation = WindowStartupLocation.Manual;

            using (Drawing.Graphics g = Drawing.Graphics.FromHdc(dc))
            {
                xDpi = g.DpiX;
                yDpi = g.DpiY;
            }

            POINT p = new POINT();
            if (GetCursorPos(out p))//API方法
            {
                _winFloat.Top = (p.y - 10) / yDpi * 96 - _pointInRectangle.Y;//需要往上偏移10像素。
                _winFloat.Left = (p.x - 10) / xDpi * 96 - _pointInRectangle.X;//需要往左偏移10像素。
            }

            //Point pointOfMouseInScreen = (e.Source as FrameworkElement).PointToScreen(pointOfMouse);
            _winFloat.Topmost = true;
            _winFloat.SourceInitialized += MainWindow_SourceInitialized;
            _winFloat.StateChanged += WinFloat_StateChanged;
            _winFloat.PreviewMouseMove += ResetCursor;
            _winFloat.Deactivated += WinFloat_Deactivated;
            _winFloat.Activated += WinFloat_Activated;
            _winFloat.MinHeight = 51;
            _winFloat.MinWidth = 51;
            _borderOfThis.BorderThickness = new Thickness(2);
            _winFloat.Show();
            _rectTitle.MouseLeftButtonDown += RectTitle_MouseLeftButtonDown;
            bool rectFocusResult = _rectTitle.Focus();
        }

        private void WinFloat_Activated(object sender, EventArgs e)
        {
            _titlePanel.Background = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xf2, 0x9d));
        }

        private void WinFloat_Deactivated(object sender, EventArgs e)
        {
            _titlePanel.Background = new SolidColorBrush(Colors.White);
        }

        public void FocusTitleRect()
        {
            bool rectFocusResult = _rectTitle.Focus();
        }

        private void removeWin()
        {
            HwndSource mainWinHwnd = (HwndSource.FromDependencyObject(_gridParent) as HwndSource);
            //MainWindow mainWin = mainWinHwnd.RootVisual as MainWindow;
            //if (mainWin != null)
            //{
            //    mainWin.ChildrenWindow.Remove(_winFloat);
            //}
        }

        private void enableDockAndCloseWin()
        {
            _winFloat.Content = null;
            _winFloat.Close();
            _stpOperation.Children.RemoveAt(1);

            _miDock.IsEnabled = false;
            _miFloat.IsEnabled = true;
        }

        public void AddControlAndSetGrid()
        {
            try
            {
                if (_gridParent.Children.Contains(this))
                {
                    return;
                }
                if (_gridParent.RowDefinitions.Count > _rowOfThis)
                {
                    _gridParent.RowDefinitions[_rowOfThis].Height = new GridLength(_originalHeight, _originalHeightType);
                }
                if (_gridParent.ColumnDefinitions.Count > _columnOfThis)
                {
                    _gridParent.ColumnDefinitions[_columnOfThis].Width = new GridLength(_originalWidth, _originalWidthType);
                }
                _gridParent.Children.Add(this);
                this.SetValue(Grid.RowProperty, _rowOfThis);
                this.SetValue(Grid.ColumnProperty, _columnOfThis);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void removeControlAndZeroGrid()
        {
            _gridParent = this.Parent as Grid;
            _rowOfThis = (int)this.GetValue(Grid.RowProperty);
            _columnOfThis = (int)this.GetValue(Grid.ColumnProperty);
            _gridParent.Children.Remove(this);
            if (_gridParent.RowDefinitions.Count > _rowOfThis)
            {
                _gridParent.RowDefinitions[_rowOfThis].Height = new GridLength(0);
            }
            if (_gridParent.ColumnDefinitions.Count > _columnOfThis)
            {
                _gridParent.ColumnDefinitions[_columnOfThis].Width = new GridLength(0);
            }
        }

        private void getOriginalHeightAndWidth()
        {
            try
            {
                Grid tempParent = this.Parent as Grid;
                if (tempParent != null)
                {
                    _gridParent = tempParent;
                    _rowOfThis = (int)this.GetValue(Grid.RowProperty);
                    _columnOfThis = (int)this.GetValue(Grid.ColumnProperty);
                    if (_gridParent.RowDefinitions.Count > _rowOfThis)
                    {
                        GridLength heightLength = _gridParent.RowDefinitions[_rowOfThis].Height;
                        _originalHeightType = heightLength.GridUnitType;
                        _originalHeight = heightLength.Value;
                    }
                    if (_gridParent.ColumnDefinitions.Count > _columnOfThis)
                    {
                        GridLength widthLength = _gridParent.ColumnDefinitions[_columnOfThis].Width;
                        _originalWidthType = widthLength.GridUnitType;
                        _originalWidth = widthLength.Value;
                    }
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void RectTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && _winFloat != null && e.Handled == false)
            {
                if (_winFloat.WindowState == WindowState.Maximized)
                {
                    BtnRestore_Click(sender, e);
                    e.Handled = true;
                }
                else if (_winFloat.WindowState == WindowState.Normal)
                {
                    BtnMaximize_Click(sender, e);
                    e.Handled = true;
                }
            }
        }

        private void WinFloat_StateChanged(object sender, EventArgs e)
        {
            if (_winFloat.WindowState == WindowState.Maximized)
            {
                _stpOperation.Children.RemoveAt(1);
                Geometry geoRestore = Geometry.Parse("M4,8H8V4H20V16H16V20H4V8M16,8V14H18V6H10V8H16M6,12V18H14V12H6Z");
                Path restorePath = new Path() { Data = geoRestore };
                restorePath.Fill = new SolidColorBrush(Colors.Black);
                _btnRestore = new Button();
                _btnRestore.Content = restorePath;
                _btnRestore.Background = new SolidColorBrush(Colors.Transparent);
                _btnRestore.BorderThickness = new Thickness(0);
                _btnRestore.Height = 15;
                _btnRestore.Width = 15;
                _btnRestore.MouseEnter += Button_MouseEnter;
                _btnRestore.MouseLeave += Button_MouseLeave;
                _btnRestore.Click += BtnRestore_Click; ;
                restorePath.Height = 11;
                restorePath.Width = 11;
                restorePath.Stretch = Stretch.Fill;
                _stpOperation.Children.Insert(1, _btnRestore);
            }
            else if (_winFloat.WindowState == WindowState.Normal)
            {
                _stpOperation.Children.RemoveAt(1);
                _stpOperation.Children.Insert(1, _btnMaximize);
            }
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (_winFloat != null && this.State == UserControlState.Float)
            {
                _winFloat.WindowState = WindowState.Maximized;
            }
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (_winFloat != null)
            {
                _winFloat.WindowState = WindowState.Normal;
            }
        }

        private void BtnMenuDown_Click(object sender, RoutedEventArgs e)
        {
            _rectTitle.ContextMenu.IsOpen = true;
        }

        public void FloatableUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _miFloat.Header ="悬浮";
            _miDock.Header = "停靠";
            _txtTitle.Text = StrTitle;
            //保存原始长度和宽度
            //_originalWidth = this.ActualWidth;
            //_originalHeight = this.ActualHeight;
            getOriginalHeightAndWidth();
        }

        private void RectTitle_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //bool rectFocusResult = _rectTitle.Focus();
            //Debug.WriteLine("矩形获取焦点结果：" + rectFocusResult.ToString());
            _thisMoveable = false;
        }

        private void RectTitle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //bool userCtrlFocusResult = this.Focus();
            //bool btnCloseFocusResult = _btnClose.Focus();
            bool rectFocusResult = _rectTitle.Focus();
            _pointInRectangle = e.GetPosition(_rectTitle);
            _thisMoveable = true;
            //Debug.WriteLine("矩形获取焦点结果：" + rectFocusResult.ToString());
            //Debug.WriteLine("用户控件获取焦点结果：" + userCtrlFocusResult.ToString());
            //Debug.WriteLine("关闭按钮获取焦点结果：" + btnCloseFocusResult.ToString());
            //FloatableUserControl_GotFocus(sender, e);
        }
        private void RectTitle_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            bool rectFocusResult = _rectTitle.Focus();
        }

        private void FloatableUserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _rectTitle.Width = (this.ActualWidth - 50) < 0 ? 0 : (this.ActualWidth - 50);
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            Button b = sender as Button;
            if (b != null)
            {
                b.BorderThickness = new Thickness(0);
            }
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            Button b = sender as Button;
            if (b != null)
            {
                b.BorderThickness = new Thickness(1);
            }
        }

        private void FloatableUserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            _titlePanel.Background = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xf2, 0x9d));
        }
        private void FloatableUserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            _titlePanel.Background = new SolidColorBrush(Colors.White);
        }

        //Copy from http://blog.csdn.net/withdreams/article/details/7497583
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetCursorPos(out POINT pt);


        //Copy from http://codego.net/28224/
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        Single xDpi, yDpi;
        IntPtr dc = GetDC(IntPtr.Zero);


        #region Copy from http://www.cnblogs.com/scy251147/archive/2012/07/25/2609197.html 用于最大化露出任务栏，以及拖拽边框改变窗口大小。

        #region 这一部分用于最大化时不遮蔽任务栏
        private static void WmGetMinMaxInfo(System.IntPtr hwnd, System.IntPtr lParam)
        {

            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            System.IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != System.IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        /// <summary>
        /// POINT aka POINTAPI
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            /// <summary>
            /// x coordinate of point.
            /// </summary>
            public int x;
            /// <summary>
            /// y coordinate of point.
            /// </summary>
            public int y;

            /// <summary>
            /// Construct a point of coordinates (x,y).
            /// </summary>
            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        };
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct RECT
        {
            /// <summary> Win32 </summary>
            public int left;
            /// <summary> Win32 </summary>
            public int top;
            /// <summary> Win32 </summary>
            public int right;
            /// <summary> Win32 </summary>
            public int bottom;

            /// <summary> Win32 </summary>
            public static readonly RECT Empty = new RECT();

            /// <summary> Win32 </summary>
            public int Width
            {
                get { return Math.Abs(right - left); }  // Abs needed for BIDI OS
            }
            /// <summary> Win32 </summary>
            public int Height
            {
                get { return bottom - top; }
            }

            /// <summary> Win32 </summary>
            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }


            /// <summary> Win32 </summary>
            public RECT(RECT rcSrc)
            {
                this.left = rcSrc.left;
                this.top = rcSrc.top;
                this.right = rcSrc.right;
                this.bottom = rcSrc.bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            /// <summary>
            /// </summary>            
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));

            /// <summary>
            /// </summary>            
            public RECT rcMonitor = new RECT();

            /// <summary>
            /// </summary>            
            public RECT rcWork = new RECT();

            /// <summary>
            /// </summary>            
            public int dwFlags = 0;
        }

        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);
        #endregion

        private HwndSource _hs;
        void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            _hs = PresentationSource.FromVisual((Visual)sender) as HwndSource;
            _hs.AddHook(new HwndSourceHook(WndProc));
            //throw new NotImplementedException();
        }

        Dictionary<int, int> messages = new Dictionary<int, int>();

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //Debug.Print(string.Format("窗体消息：{0}，wParam:{1},lParam:{2}", msg.ToString(), wParam.ToString(), lParam.ToString()));
            switch (msg)
            {
                case 0x0024:/* WM_GETMINMAXINFO */
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
                default: break;
            }
            return (System.IntPtr)0;
            //return new IntPtr(0);
        }
        #region  还原鼠标形状
        private void ResetCursor(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton != MouseButtonState.Pressed)
            {
                this.Cursor = Cursors.Arrow;
            }
        }
        #endregion

        #region 显示拖拉鼠标形状
        public double RelativeClip = 10;
        private void DisplayResizeCursor(object sender, MouseEventArgs e)
        {
            Point pos = Mouse.GetPosition(this);
            double x = pos.X;
            double y = pos.Y;
            double w = this.ActualWidth;  //注意这个地方使用ActualWidth,才能够实时显示宽度变化
            double h = this.ActualHeight;
            if (_winFloat != null)
            {
                if (_winFloat.WindowState == WindowState.Normal)
                {
                    if (x <= RelativeClip & y <= RelativeClip) // left top
                    {
                        this.Cursor = Cursors.SizeNWSE;
                    }
                    if (x >= w - RelativeClip & y <= RelativeClip) //right top
                    {
                        this.Cursor = Cursors.SizeNESW;
                    }

                    if (x >= w - RelativeClip & y >= h - RelativeClip) //bottom right
                    {
                        this.Cursor = Cursors.SizeNWSE;
                    }

                    if (x <= RelativeClip & y >= h - RelativeClip)  // bottom left
                    {
                        this.Cursor = Cursors.SizeNESW;
                    }

                    if ((x >= RelativeClip & x <= w - RelativeClip) & y <= RelativeClip) //top
                    {
                        this.Cursor = Cursors.SizeNS;
                    }

                    if (x >= w - RelativeClip & (y >= RelativeClip & y <= h - RelativeClip)) //right
                    {
                        this.Cursor = Cursors.SizeWE;
                    }

                    if ((x >= RelativeClip & x <= w - RelativeClip) & y > h - RelativeClip) //bottom
                    {
                        this.Cursor = Cursors.SizeNS;
                    }

                    if (x <= RelativeClip & (y <= h - RelativeClip & y >= RelativeClip)) //left
                    {
                        this.Cursor = Cursors.SizeWE;
                    }
                }
            }
        }
        #endregion

        #region 判断区域，改变窗体大小
        private void Resize(object sender, MouseButtonEventArgs e)
        {
            Point pos = Mouse.GetPosition(this);
            double x = pos.X;
            double y = pos.Y;
            double w = this.ActualWidth;
            double h = this.ActualHeight;

            #region TODO:resize details
            if (_winFloat == null)
            {
                return;
            }
            if (_winFloat.WindowState == WindowState.Normal)
            {
                if (enableXResize && enableYResize)
                {
                    #region corners
                    if (x <= RelativeClip & y <= RelativeClip) // left top
                    {
                        this.Cursor = Cursors.SizeNWSE;
                        //Debug.WriteLine("hit");
                        ResizeWindow(ResizeDirection.TopLeft);
                    }
                    if (x >= w - RelativeClip & y <= RelativeClip) //right top
                    {
                        this.Cursor = Cursors.SizeNESW;
                        ResizeWindow(ResizeDirection.TopRight);
                    }

                    if (x >= w - RelativeClip & y >= h - RelativeClip) //bottom right
                    {
                        this.Cursor = Cursors.SizeNWSE;
                        ResizeWindow(ResizeDirection.BottomRight);
                    }

                    if (x <= RelativeClip & y >= h - RelativeClip)  // bottom left
                    {
                        this.Cursor = Cursors.SizeNESW;
                        ResizeWindow(ResizeDirection.BottomLeft);
                    }
                    #endregion
                }

                if (enableXResize)
                {
                    #region x direction
                    if ((x >= RelativeClip & x <= w - RelativeClip) & y <= RelativeClip) //top
                    {
                        this.Cursor = Cursors.SizeNS;
                        ResizeWindow(ResizeDirection.Top);
                    }
                    if ((x >= RelativeClip & x <= w - RelativeClip) & y > h - RelativeClip) //bottom
                    {
                        this.Cursor = Cursors.SizeNS;
                        ResizeWindow(ResizeDirection.Bottom);
                    }
                    #endregion
                }

                if (enableYResize)
                {
                    #region y direction
                    if (x >= w - RelativeClip & (y >= RelativeClip & y <= h - RelativeClip)) //right
                    {
                        this.Cursor = Cursors.SizeWE;
                        ResizeWindow(ResizeDirection.Right);
                    }
                    if (x <= RelativeClip & (y <= h - RelativeClip & y >= RelativeClip)) //left
                    {
                        this.Cursor = Cursors.SizeWE;
                        ResizeWindow(ResizeDirection.Left);
                    }
                    #endregion
                }
            }
            #endregion
        }
        #endregion

        #region 这一部分是四个边加上四个角
        public enum ResizeDirection
        {
            Left = 1,
            Right = 2,
            Top = 3,
            TopLeft = 4,
            TopRight = 5,
            Bottom = 6,
            BottomLeft = 7,
            BottomRight = 8,
        }
        #endregion

        #region  用于改变窗体大小
        private const int WM_SYSCOMMAND = 0x112;
        private const int WM_LBUTTONUP = 0x0202;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// 在Win32中，61440+1 代表左边，61440+2代表右边，以此类推。
        /// </summary>
        /// <param name="direction"></param>
        private void ResizeWindow(ResizeDirection direction)
        {
            if (_winFloat.WindowState == WindowState.Normal)
            {
                if (direction == ResizeDirection.Left || direction == ResizeDirection.Right)
                {
                    if (this.ActualWidth > 50)
                    {
                        //Debug.WriteLine("hit");
                        SendMessage(_hs.Handle, WM_SYSCOMMAND, (IntPtr)(61440 + direction), IntPtr.Zero);
                    }
                }
                else if (direction == ResizeDirection.Bottom || direction == ResizeDirection.Top)
                {
                    if (this.ActualHeight > 50)
                    {
                        SendMessage(_hs.Handle, WM_SYSCOMMAND, (IntPtr)(61440 + direction), IntPtr.Zero);
                    }
                }
                else
                {
                    SendMessage(_hs.Handle, WM_SYSCOMMAND, (IntPtr)(61440 + direction), IntPtr.Zero);
                }
            }
        }
        #endregion


        #region 重写的DragMove，以便解决利用系统自带的DragMove出现Exception的情况
        public void DragMove(object sender, MouseButtonEventArgs e)
        {
            if (_winFloat.WindowState == WindowState.Normal)
            {
                SendMessage(_hs.Handle, WM_SYSCOMMAND, (IntPtr)0xf012, IntPtr.Zero);
                SendMessage(_hs.Handle, WM_LBUTTONUP, IntPtr.Zero, IntPtr.Zero);
            }
        }
        #endregion

        bool enableXResize = true;
        bool enableYResize = true;

        protected override void OnRender(DrawingContext drawingContext)
        {
            enableXResize = true;
            enableYResize = true;
            double x = this.ActualWidth;
            double y = this.ActualHeight;
            if (x < MinWidth)
                enableXResize = false;
            if (y < MinHeight)
                enableYResize = false;

            if (!enableXResize)
            {
                this.Width = MinWidth;
                return;
            }

            if (!enableYResize)
            {
                this.Height = MinHeight;
                return;
            }

            base.OnRender(drawingContext);
        }
        #endregion
    }
}
