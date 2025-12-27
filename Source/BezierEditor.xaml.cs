using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BezierTool;

/// <summary>
/// 贝塞尔曲线编辑器窗口，提供可视化编辑贝塞尔曲线的功能。
/// 支持添加、删除、移动控制点，调整控制柄，以及坐标变换和网格显示。
/// </summary>
public partial class BezierEditor
{
    /// <summary>
    /// 工具状态枚举，表示当前编辑器所处的操作状态。
    /// </summary>
    private enum EToolState
    {
        /// <summary>无操作状态</summary>
        NONE,
        /// <summary>视图拖拽状态</summary>
        VIEW_DRAGGING,
        /// <summary>控制点拖拽状态</summary>
        POINT_DRAGGING,
        /// <summary>左控制柄拖拽状态</summary>
        LEFTCTRL_DRAGGING,
        /// <summary>右控制柄拖拽状态</summary>
        RIGHTCTRL_DRAGGING,
    }

    /// <summary>
    /// 采样数据模型，用于存储贝塞尔曲线上的采样点坐标。
    /// </summary>
    public class SampleData
    {
        /// <summary>获取或设置 X 坐标值。</summary>
        public double X { get; set; }
        /// <summary>获取或设置 Y 坐标值。</summary>
        public double Y { get; set; }
    }

    /// <summary>指示窗口是否已初始化完成。</summary>
    private bool bInitialized_ = false;

    /// <summary>当前编辑的贝塞尔曲线视图对象。</summary>
    private BezierCurveView? bezierCurve_ = null;

    /// <summary>当前工具状态。</summary>
    private EToolState state_ = EToolState.NONE;

    /// <summary>当前选中的贝塞尔点。</summary>
    private BezierPointView? selectedPoint_ = null;

    /// <summary>坐标变换器，用于屏幕坐标和世界坐标之间的转换。</summary>
    private Transformer transform_ = new Transformer();

    /// <summary>网格间距向量，表示 X 和 Y 方向的网格间距。</summary>
    private Vector gridSpace_ = new Vector(1.0, 1.0);

    /// <summary>上一次鼠标位置（用于拖拽计算）。</summary>
    private Point lastMousePosition_;

    /// <summary>应用按钮点击时的回调函数。</summary>
    private Action<BezierCurve>? onApply_ = null;

    /// <summary>
    /// 获取一个值，指示是否正在拖拽控制点或控制柄。
    /// </summary>
    private bool IsDraggingPointOrControlPoint
    {
        get => state_ == EToolState.POINT_DRAGGING || IsDraggingControlPoint;
    }

    /// <summary>
    /// 获取一个值，指示是否正在拖拽控制柄（左或右）。
    /// </summary>
    private bool IsDraggingControlPoint
    {
        get => state_ == EToolState.LEFTCTRL_DRAGGING || state_ == EToolState.RIGHTCTRL_DRAGGING;
    }

    /// <summary>
    /// 初始化 BezierEditor 类的新实例。
    /// </summary>
    public BezierEditor()
    {
        bInitialized_ = false;
        InitializeComponent();
        // 初始化状态
        UpdateStatus("正在初始化...");
    }

    /// <summary>
    /// 窗口加载完成事件处理程序。
    /// </summary>
    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        bInitialized_ = true;
        OnShow();
    }

    /// <summary>
    /// 显示窗口时执行的初始化逻辑。
    /// </summary>
    private void OnShow()
    {
        try
        {
            if (bezierCurve_ == null)
            {
                bezierCurve_ = new BezierCurveView(this);
            }

            UpdateListPoints();
            ResetCanvasView();
            UpdateSampleData();
            UpdateStatus("贝塞尔曲线编辑器已就绪");
        }
        catch (Exception ex)
        {
            UpdateStatus($"初始化失败: {ex.Message}");
            MessageBox.Show($"初始化失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 更新控制点列表的数据绑定。
    /// </summary>
    private void UpdateListPoints()
    {
        LstPoints.ItemsSource = bezierCurve_.ItemSources;
        LstPoints.Items.Refresh();
    }

    /// <summary>
    /// 更新采样数据，根据当前步长在曲线上采样并显示在数据网格中。
    /// </summary>
    private void UpdateSampleData()
    {
        try
        {
            if (bezierCurve_.Points.Count < 2)
            {
                SampleDataGrid.ItemsSource = null;
                TxtXRange.Text = "N/A";
                return;
            }

            if (!double.TryParse(TxtStepSize.Text, out double stepSize) || stepSize <= 0)
            {
                return;
            }

            double minX = bezierCurve_.Points[0].Position.X;
            double maxX = bezierCurve_.Points[bezierCurve_.Points.Count - 1].Position.X;

            TxtXRange.Text = $"{minX:F2} - {maxX:F2}";
            int count = (int) ((maxX - minX) / stepSize);
            List<SampleData> samples = new List<SampleData>(count + 1);
            for (int i = 0; i <= count; i++)
            {
                double x = minX + stepSize * i;
                samples.Add(new()
                {
                    X = x,
                    Y = bezierCurve_.GetValue<double>(x)
                });
            }

            // 确保包含最后一个点
            if (Math.Abs(samples.Last().X - maxX) > 1E-6)
            {
                samples.Add(new()
                {
                    X = maxX,
                    Y = bezierCurve_.GetValue<double>(maxX)
                });
            }

            SampleDataGrid.ItemsSource = samples;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"采样更新失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新视图变换，将变换应用到画布并重新绘制坐标轴和网格。
    /// </summary>
    private void UpdateViewTransform()
    {
        // 应用变换到两个Canvas
        BezierCanvas.RenderTransform = transform_.GetRenderTransform();
        AxisCanvas.RenderTransform = transform_.GetRenderTransform();
        UpdateGridSpace();
        UpdateCanvasClips();
        DrawCoordinateAxes();
        UpdateVisualization();
    }

    /// <summary>
    /// 更新画布的裁剪区域，确保内容不会超出画布范围。
    /// </summary>
    private void UpdateCanvasClips()
    {
        // 获取mainCanvas的实际大小
        double canvasWidth = MainCanvas.ActualWidth;
        double canvasHeight = MainCanvas.ActualHeight;
        RectangleGeometry mainClip = MainCanvas.Clip as RectangleGeometry;
        if (mainClip != null)
        {
            mainClip.Rect = new Rect(0, 0, canvasWidth, canvasHeight);
        }
    }

    /// <summary>
    /// 绘制坐标轴和网格。
    /// </summary>
    private void DrawCoordinateAxes()
    {
        // 清除现有的坐标轴元素
        AxisCanvas.Children.Clear();

        double canvasWidth = MainCanvas.ActualWidth;
        double canvasHeight = MainCanvas.ActualHeight;

        // 计算视图范围（世界坐标）
        Vector topLeft = transform_.ScreenToWorld(new Point(0, 0));
        Vector bottomRight = transform_.ScreenToWorld(new Point(canvasWidth, canvasHeight));

        double minX = topLeft.X;
        double maxX = bottomRight.X;
        double minY = topLeft.Y;
        double maxY = bottomRight.Y;

        // 绘制网格
        DrawGrid(minX, maxX, minY, maxY);

        // 绘制坐标轴
        DrawAxes(minX, maxX, minY, maxY);

        // 绘制刻度标签
        DrawAxisLabels(minX, maxX, minY, maxY);
    }

    /// <summary>
    /// 绘制网格线。
    /// </summary>
    /// <param name="minX">最小 X 坐标。</param>
    /// <param name="maxX">最大 X 坐标。</param>
    /// <param name="minY">最小 Y 坐标。</param>
    /// <param name="maxY">最大 Y 坐标。</param>
    private void DrawGrid(double minX, double maxX, double minY, double maxY)
    {
        // 计算网格线的起始和结束位置
        double startX = Math.Floor(minX / gridSpace_.X) * gridSpace_.X;
        double endX = Math.Ceiling(maxX / gridSpace_.X) * gridSpace_.X;
        double startY = Math.Floor(minY / gridSpace_.Y) * gridSpace_.Y;
        double endY = Math.Ceiling(maxY / gridSpace_.Y) * gridSpace_.Y;

        // 绘制垂直网格线
        for (double x = startX; x <= endX; x += gridSpace_.X)
        {
            Line line = new Line
            {
                X1 = x,
                Y1 = minY,
                X2 = x,
                Y2 = maxY,
                Stroke = Brushes.LightGray,
                StrokeThickness = 0.5 / transform_.Scale.X
            };

            AxisCanvas.Children.Add(line);
        }

        // 绘制水平网格线
        for (double y = startY; y <= endY; y += gridSpace_.Y)
        {
            Line line = new Line
            {
                X1 = minX,
                Y1 = y,
                X2 = maxX,
                Y2 = y,
                Stroke = Brushes.LightGray,
                StrokeThickness = 0.5 / transform_.Scale.Y
            };

            AxisCanvas.Children.Add(line);
        }
    }

    /// <summary>
    /// 绘制坐标轴。
    /// </summary>
    /// <param name="minX">最小 X 坐标。</param>
    /// <param name="maxX">最大 X 坐标。</param>
    /// <param name="minY">最小 Y 坐标。</param>
    /// <param name="maxY">最大 Y 坐标。</param>
    private void DrawAxes(double minX, double maxX, double minY, double maxY)
    {
        double x = minX > 0 ? minX : maxX < 0 ? maxX : 0;
        double y = minY > 0 ? minY : maxY < 0 ? maxY : 0;
        // X轴
        var xAxis = new Line
        {
            X1 = minX,
            Y1 = y,
            X2 = maxX,
            Y2 = y,
            Stroke = Brushes.Black,
            StrokeThickness = 1.5 / transform_.Scale.Y
        };

        // Y轴
        var yAxis = new Line
        {
            X1 = x,
            Y1 = minY,
            X2 = x,
            Y2 = maxY,
            Stroke = Brushes.Black,
            StrokeThickness = 1.5 / transform_.Scale.X
        };

        AxisCanvas.Children.Add(xAxis);
        AxisCanvas.Children.Add(yAxis);

        // 绘制箭头
        DrawArrow(new Point(maxX, y),
            new Point(maxX - 10 / transform_.Scale.X, y - 5 / transform_.Scale.Y),
            new Point(maxX - 10 / transform_.Scale.X, y + 5 / transform_.Scale.Y));
        DrawArrow(new Point(x, minY),
            new Point(x - 5 / transform_.Scale.X, minY + 10 / transform_.Scale.Y),
            new Point(x + 5 / transform_.Scale.X, minY + 10 / transform_.Scale.Y));
    }

    /// <summary>
    /// 计算刻度标签的小数位数。
    /// </summary>
    /// <param name="num">网格间距值。</param>
    /// <returns>建议的小数位数。</returns>
    private static int CalScale(double num)
    {
        double log10X = Math.Log10(num);
        int floorLog = (int) Math.Floor(log10X);
        int y = 1 - floorLog;
        return Math.Max(0, y);
    }

    /// <summary>
    /// 绘制坐标轴刻度标签。
    /// </summary>
    /// <param name="minX">最小 X 坐标。</param>
    /// <param name="maxX">最大 X 坐标。</param>
    /// <param name="minY">最小 Y 坐标。</param>
    /// <param name="maxY">最大 Y 坐标。</param>
    private void DrawAxisLabels(double minX, double maxX, double minY, double maxY)
    {
        double centerX = (maxX + minX) / 2;
        double centerY = (maxY + minY) / 2;
        bool yOnLeft = centerX > 0;
        bool xOnTop = centerY > 0;
        double xBase = minX > 0 ? minX : maxX < 0 ? maxX : 0;
        double yBase = minY > 0 ? minY : maxY < 0 ? maxY : 0;
        // 计算刻度位置
        double startX = Math.Floor(minX / gridSpace_.X) * gridSpace_.X;
        double endX = Math.Ceiling(maxX / gridSpace_.X) * gridSpace_.X;
        double startY = Math.Floor(minY / gridSpace_.Y) * gridSpace_.Y;
        double endY = Math.Ceiling(maxY / gridSpace_.Y) * gridSpace_.Y;

        int scaleX = CalScale(gridSpace_.X);
        int scaleY = CalScale(gridSpace_.Y);

        double labelWidth = 50;
        double labelHeight = 16;

        // X轴刻度
        for (double x = startX; x <= endX; x += gridSpace_.X)
        {
            if (Math.Abs(x) > 0.001) // 跳过原点
            {
                // 刻度线
                Line tick = new Line
                {
                    X1 = x,
                    Y1 = yBase - 3 / transform_.Scale.Y,
                    X2 = x,
                    Y2 = yBase + 3 / transform_.Scale.Y,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1 / transform_.Scale.X
                };

                // 标签
                TextBlock label = new TextBlock
                {
                    Text = x.ToString($"F{scaleX}"),
                    Width = labelWidth,
                    Height = labelHeight,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 12,
                    Foreground = Brushes.Black,
                    Background = Brushes.Transparent,
                    RenderTransform = new ScaleTransform(1 / transform_.Scale.X, 1 / transform_.Scale.Y)
                };

                AxisCanvas.Children.Add(tick);
                AxisCanvas.Children.Add(label);

                Canvas.SetLeft(label, x - labelWidth / 2 / transform_.Scale.X);
                Canvas.SetTop(label,
                    xOnTop
                        ? yBase + 8 / transform_.Scale.Y
                        : yBase - labelHeight / transform_.Scale.Y - 8 / transform_.Scale.Y);
            }
        }

        // Y轴刻度
        for (double y = startY; y <= endY; y += gridSpace_.Y)
        {
            if (Math.Abs(y) > 0.001) // 跳过原点
            {
                // 刻度线
                Line tick = new Line
                {
                    X1 = xBase - 3 / transform_.Scale.X,
                    Y1 = y,
                    X2 = xBase + 3 / transform_.Scale.X,
                    Y2 = y,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1 / transform_.Scale.Y
                };

                // 标签
                TextBlock label = new TextBlock
                {
                    Text = (-y).ToString($"F{scaleY}"),
                    Width = labelWidth,
                    Height = labelHeight,
                    TextAlignment = yOnLeft ? TextAlignment.Left : TextAlignment.Right,
                    FontSize = 12,
                    Foreground = Brushes.Black,
                    Background = Brushes.Transparent,
                    RenderTransform = new ScaleTransform(1 / transform_.Scale.X, 1 / transform_.Scale.Y)
                };

                AxisCanvas.Children.Add(tick);
                AxisCanvas.Children.Add(label);

                Canvas.SetLeft(label,
                    yOnLeft
                        ? xBase + 8 / transform_.Scale.X
                        : xBase - labelWidth / transform_.Scale.X - 8 / transform_.Scale.X);
                Canvas.SetTop(label, y - labelHeight / 2 / transform_.Scale.Y);
            }
        }

        // 原点标签
        TextBlock originLabel = new TextBlock
        {
            Text = "0",
            FontSize = 10,
            Foreground = Brushes.Black,
            Background = Brushes.Transparent,
            RenderTransform = new ScaleTransform(1 / transform_.Scale.X, 1 / transform_.Scale.Y)
        };

        AxisCanvas.Children.Add(originLabel);
        Canvas.SetLeft(originLabel, 8 / transform_.Scale.X);
        Canvas.SetTop(originLabel, 8 / transform_.Scale.Y);
    }

    /// <summary>
    /// 绘制坐标轴箭头。
    /// </summary>
    /// <param name="tip">箭头尖端位置。</param>
    /// <param name="leftWing">左翼位置。</param>
    /// <param name="rightWing">右翼位置。</param>
    private void DrawArrow(Point tip, Point leftWing, Point rightWing)
    {
        Polygon arrow = new Polygon
        {
            Points = new PointCollection {tip, leftWing, rightWing},
            Fill = Brushes.Black,
        };

        AxisCanvas.Children.Add(arrow);
    }

    /// <summary>
    /// 重置画布视图。
    /// </summary>
    /// <param name="bStretch">是否拉伸视图以填充整个画布。</param>
    private void ResetCanvasView(bool bStretch = true)
    {
        ResetViewTransform(bStretch);
        UpdateViewTransform();
    }

    /// <summary>
    /// 重置视图变换。
    /// </summary>
    /// <param name="bStretch">是否拉伸视图以填充整个画布。</param>
    private void ResetViewTransform(bool bStretch)
    {
        double canvasWidth = MainCanvas.ActualWidth;
        double canvasHeight = MainCanvas.ActualHeight;

        bezierCurve_.GetValueRange(
            out double minX,
            out double minY,
            out double maxX,
            out double maxY);

        double lenX = maxX - minX;
        double lenY = maxY - minY;

        if (bStretch)
        {
            transform_.SetPosition(0, canvasHeight);
            transform_.SetScale(canvasWidth / lenX, canvasHeight / lenY);
        }
        else
        {
            double scale;
            double offsetX;
            double offsetY;

            if (lenY / lenX > canvasHeight / canvasWidth)
            {
                scale = canvasHeight / lenY;
                offsetX = (canvasWidth - lenX * scale) / 2.0;
                offsetY = canvasHeight;
            }
            else
            {
                scale = canvasWidth / lenX;
                offsetX = 0;
                offsetY = canvasHeight - (canvasHeight - lenY * scale) / 2.0;
            }

            transform_.SetPosition(offsetX - scale * minX, offsetY + scale * maxY);
            transform_.SetScale(scale, scale);
        }
    }

    /// <summary>
    /// 根据当前视图范围更新网格间距。
    /// </summary>
    private void UpdateGridSpace()
    {
        double canvasWidth = MainCanvas.ActualWidth;
        double canvasHeight = MainCanvas.ActualHeight;

        // 计算视图范围（世界坐标）
        Vector topLeft = transform_.ScreenToWorld(new Point(0, 0));
        Vector bottomRight = transform_.ScreenToWorld(new Point(canvasWidth, canvasHeight));

        double lenX = bottomRight.X - topLeft.X;
        double lenY = bottomRight.Y - topLeft.Y;

        gridSpace_.X = CalculateOptimalGridSize(lenX);
        gridSpace_.Y = CalculateOptimalGridSize(lenY);
    }

    /// <summary>
    /// 计算最优网格间距。
    /// </summary>
    /// <param name="length">视图范围长度。</param>
    /// <returns>计算出的网格间距。</returns>
    private double CalculateOptimalGridSize(double length)
    {
        double space = length / 10;
        int pow2 = (int) Math.Log10(space / 2);
        int pow5 = (int) Math.Log10(space / 5);
        int pow10 = (int) Math.Log10(space / 10);
        double space2 = 2 * Math.Pow(10, pow2);
        double space5 = 5 * Math.Pow(10, pow5);
        double space10 = 10 * Math.Pow(10, pow10);

        // 计算每种间距下 length/space 的值
        double ratio2 = length / space2;
        double ratio5 = length / space5;
        double ratio10 = length / space10;

        // 计算与10的差距
        double diff2 = Math.Abs(ratio2 - 10);
        double diff5 = Math.Abs(ratio5 - 10);
        double diff10 = Math.Abs(ratio10 - 10);

        // 找出最接近10的间距
        if (diff2 <= diff5 && diff2 <= diff10)
        {
            return space2;
        }

        if (diff5 <= diff10)
        {
            return space5;
        }

        return space10;
    }

    /// <summary>
    /// 更新可视化元素，包括贝塞尔曲线和控制点。
    /// </summary>
    private void UpdateVisualization()
    {
        bezierCurve_.Refresh(MainCanvas, BezierCanvas, BezierPath, selectedPoint_, transform_,
            ChkAccurate.IsChecked ?? false);
        UpdatePointCount();
    }

    /// <summary>
    /// 更新指定控制点的位置。
    /// </summary>
    /// <param name="point">要更新的控制点。</param>
    private void UpdatePointPosition(BezierPoint point)
    {
        if (bezierCurve_.Points.Contains(point))
        {
            ((BezierPointView) point).Refresh(transform_);
            UpdateVisualization();
        }
    }

    /// <summary>
    /// 更新鼠标位置显示（世界坐标）。
    /// </summary>
    /// <param name="screenPosition">屏幕坐标位置。</param>
    private void UpdateMousePosition(Point screenPosition)
    {
        if (TxtMousePosition != null)
        {
            Vector worldPosition = transform_.ScreenToWorld(screenPosition);
            TxtMousePosition.Text = $"X: {worldPosition.X:F1}, Y: {worldPosition.Y:F1}";
        }
    }

    /// <summary>
    /// 更新属性面板，显示当前选中点的属性。
    /// </summary>
    /// <param name="sender">触发更新的控件。</param>
    private void UpdatePropertyPanel(object? sender = null)
    {
        // 刷新列表显示
        UpdateListPoints();
        if (selectedPoint_ != null)
        {
            BezierPointView point = selectedPoint_;
            if (!Equals(sender, TxtPointX))
            {
                TxtPointX.Text = point.Position.X.ToString("F2");
            }

            if (!Equals(sender, TxtPointY))
            {
                TxtPointY.Text = point.Position.Y.ToString("F2");
            }

            // 更新对称性复选框
            if (ChkSymmetric != null)
            {
                ChkSymmetric.IsChecked = point.IsSymmetric;
            }

            if (!Equals(sender, TxtLeftControlX))
            {
                TxtLeftControlX.Text = point.LeftControl.X.ToString("F2");
            }

            if (!Equals(sender, TxtLeftControlY))
            {
                TxtLeftControlY.Text = point.LeftControl.Y.ToString("F2");
            }

            if (!Equals(sender, TxtRightControlX))
            {
                TxtRightControlX.Text = point.RightControl.X.ToString("F2");
            }

            if (!Equals(sender, TxtRightControlY))
            {
                TxtRightControlY.Text = point.RightControl.Y.ToString("F2");
            }
        }
        else
        {
            TxtPointX.Text = "";
            TxtPointY.Text = "";

            if (ChkSymmetric != null)
            {
                ChkSymmetric.IsChecked = false;
            }

            TxtLeftControlX.Text = "";
            TxtLeftControlY.Text = "";
            TxtRightControlX.Text = "";
            TxtRightControlY.Text = "";
        }
    }

    /// <summary>
    /// 更新点数量显示。
    /// </summary>
    private void UpdatePointCount()
    {
        if (TxtPointCount != null)
        {
            TxtPointCount.Text = bezierCurve_.Points.Count.ToString();
        }
    }

    /// <summary>
    /// 更新状态信息。
    /// </summary>
    /// <param name="message">要显示的状态消息。</param>
    private void UpdateStatus(string message)
    {
        if (TxtStatus != null)
        {
            TxtStatus.Text = message;
        }
    }

    /// <summary>
    /// 步长变化事件处理程序。
    /// </summary>
    private void OnStepSizeChanged(object sender, TextChangedEventArgs e)
    {
        if (bezierCurve_ != null)
        {
            UpdateSampleData();
        }
    }

    /// <summary>
    /// 点选择变化事件处理程序。
    /// </summary>
    private void OnPointSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstPoints.SelectedItem is BezierPointView selectedPoint)
        {
            selectedPoint_ = selectedPoint;
            selectedPoint.Refresh(transform_);
            UpdatePropertyPanel();
        }
    }

    /// <summary>
    /// 点属性变化事件处理程序。
    /// </summary>
    private void OnPointPropertyChanged(object sender, TextChangedEventArgs e)
    {
        if (selectedPoint_ != null && !IsDraggingPointOrControlPoint)
        {
            BezierPointView point = selectedPoint_;
            // 更新X坐标
            if (sender == TxtPointX && double.TryParse(TxtPointX.Text, out double x))
            {
                point.Position = new Vector(x, selectedPoint_.Position.Y);
            }
            // 更新Y坐标
            else if (sender == TxtPointY && double.TryParse(TxtPointY.Text, out double y))
            {
                point.Position = new Vector(selectedPoint_.Position.X, y);
            }

            bezierCurve_.Sort();
            UpdatePointPosition(selectedPoint_);

            UpdatePropertyPanel(sender);
            UpdateSampleData();
        }
    }

    /// <summary>
    /// 对称性变化事件处理程序。
    /// </summary>
    private void OnSymmetricChanged(object sender, RoutedEventArgs e)
    {
        if (selectedPoint_ != null && ChkSymmetric != null)
        {
            BezierPointView point = selectedPoint_;
            bool isSymmetric = ChkSymmetric.IsChecked == true;
            point.IsSymmetric = isSymmetric;

            // 如果切换到对称模式，重置控制杆
            if (isSymmetric)
            {
                point.ResetControlHandles();
                UpdatePointPosition(selectedPoint_);
                UpdatePropertyPanel();
            }

            UpdateStatus(isSymmetric ? "对称模式：调整一个控制杆，另一个会自动对称" : "独立模式：控制杆可分别调整");
        }
    }

    /// <summary>
    /// 控制点属性变化事件处理程序。
    /// </summary>
    private void OnControlPropertyChanged(object sender, TextChangedEventArgs e)
    {
        if (selectedPoint_ != null && !IsDraggingPointOrControlPoint)
        {
            BezierPointView point = selectedPoint_;
            // 更新左控制点
            if (sender == TxtLeftControlX && double.TryParse(TxtLeftControlX.Text, out double leftX))
            {
                point.LeftControl = new Vector(
                    leftX,
                    point.LeftControl.Y);
                UpdatePointPosition(selectedPoint_);
            }
            else if (sender == TxtLeftControlY && double.TryParse(TxtLeftControlY.Text, out double leftY))
            {
                point.LeftControl = new Vector(
                    point.LeftControl.X,
                    leftY);
                UpdatePointPosition(selectedPoint_);
            }
            // 更新右控制点
            else if (sender == TxtRightControlX && double.TryParse(TxtRightControlX.Text, out double rightX))
            {
                point.RightControl = new Vector(
                    rightX,
                    point.RightControl.Y);
                UpdatePointPosition(selectedPoint_);
            }
            else if (sender == TxtRightControlY && double.TryParse(TxtRightControlY.Text, out double rightY))
            {
                point.RightControl = new Vector(
                    point.RightControl.X,
                    rightY);
                UpdatePointPosition(selectedPoint_);
            }

            // 更新属性面板显示
            UpdatePropertyPanel(sender);
            UpdateSampleData();
        }
    }

    /// <summary>
    /// 主画布鼠标按下事件处理程序。
    /// </summary>
    private void OnMainCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        Console.WriteLine($"OnMainCanvasMouseDown: Button={e.ChangedButton}, Position={e.GetPosition(MainCanvas)}");
        Point mousePosition = e.GetPosition(MainCanvas);
        lastMousePosition_ = mousePosition;
        Vector position = mousePosition.ToVector();

        // 将屏幕坐标转换为世界坐标
        Vector worldPosition = transform_.ScreenToWorld(mousePosition);

        // 检查是否点击在贝塞尔曲线元素上
        BezierPointView? hitPoint = null;
        bool? bLeftControl = null;

        // 检查是否点击在点或控制点上（考虑缩放后的点击区域）
        double hitRadius = 10.0; // 根据缩放调整点击半径

        Ellipse? ellipse = BezierCanvas.InputHitTest(worldPosition.ToPoint()) as Ellipse;

        if (ellipse != null)
        {
            foreach (BezierPoint point in bezierCurve_.Points)
            {
                BezierPointView viewPoint = (BezierPointView) point;
                if (ellipse == viewPoint.Point)
                {
                    hitPoint = viewPoint;
                    break;
                }

                if (ellipse == viewPoint.LeftControlPoint)
                {
                    hitPoint = viewPoint;
                    bLeftControl = true;
                    break;
                }

                if (ellipse == viewPoint.RightControlPoint)
                {
                    hitPoint = viewPoint;
                    bLeftControl = false;
                    break;
                }
            }
        }

        if (hitPoint != null)
        {
            Console.WriteLine($"点击在贝塞尔元素上，等待 OnPointMouseDown 处理");
            OnPointMouseDown(hitPoint, bLeftControl);
        }
        else
        {
            // 点击空白处，开始拖拽视图
            state_ = EToolState.VIEW_DRAGGING;
            MainCanvas.Cursor = Cursors.ScrollAll;

            UpdateStatus("拖拽视图调整位置");
            Console.WriteLine("开始视图拖拽");
        }

        UpdateMousePosition(mousePosition);
    }

    /// <summary>
    /// 点鼠标按下事件处理程序。
    /// </summary>
    /// <param name="point">被点击的控制点。</param>
    /// <param name="isLeftControl">指示是否点击了左控制柄。</param>
    private void OnPointMouseDown(BezierPointView point, bool? isLeftControl)
    {
        Console.WriteLine($"OnPointMouseDown: point={point.Point.Name}, isLeftControl={isLeftControl}");

        // 选中点
        selectedPoint_ = point;
        foreach (BezierPoint bezierPoint in bezierCurve_.Points)
        {
            BezierPointView viewPoint = (BezierPointView) bezierPoint;
            viewPoint.IsSelected = bezierPoint == selectedPoint_;
            viewPoint.Refresh(transform_);
        }

        UpdatePropertyPanel();
        LstPoints.SelectedItem = point;

        // 如果是对称模式，重置控制杆
        if (point.IsSymmetric)
        {
            point.ResetControlHandles();
            UpdatePointPosition(point);
            UpdatePropertyPanel();
        }

        state_ = !isLeftControl.HasValue
            ? EToolState.POINT_DRAGGING
            : (isLeftControl.Value ? EToolState.LEFTCTRL_DRAGGING : EToolState.RIGHTCTRL_DRAGGING);
        UpdateStatus(IsDraggingControlPoint ? "拖动控制点调整曲线曲率" : "拖动点调整位置");
        Console.WriteLine($"开始点拖拽: isDraggingControlPoint={IsDraggingControlPoint}, 对称模式={point.IsSymmetric}");
    }

    /// <summary>
    /// 主画布鼠标移动事件处理程序。
    /// </summary>
    private void OnMainCanvasMouseMove(object sender, MouseEventArgs e)
    {
        Point position = e.GetPosition(MainCanvas);
        UpdateMousePosition(position);

        if (state_ == EToolState.NONE)
        {
            lastMousePosition_ = position;
            return;
        }

        if (state_ == EToolState.VIEW_DRAGGING && e.LeftButton == MouseButtonState.Pressed)
        {
            // 拖拽视图
            Point delta = new Point(position.X - lastMousePosition_.X, position.Y - lastMousePosition_.Y);

            // 更新视图偏移（屏幕坐标）
            transform_.Move(delta.X, delta.Y);
            UpdateViewTransform();
        }
        // 修改控制点拖拽逻辑（在 OnMainCanvasMouseMove 方法中）
        else if (selectedPoint_ != null && e.LeftButton == MouseButtonState.Pressed)
        {
            // 拖拽点（世界坐标）
            Vector worldPosition = transform_.ScreenToWorld(position);
            Vector lastWorldPosition = transform_.ScreenToWorld(lastMousePosition_);
            Vector delta = worldPosition - lastWorldPosition;


            BezierPointView point = selectedPoint_;

            switch (state_)
            {
                case EToolState.POINT_DRAGGING:
                {
                    // 拖动主点
                    point.PositionScaled += delta;
                    bezierCurve_.Sort();
                }
                    break;
                case EToolState.LEFTCTRL_DRAGGING:
                {
                    // 使用新的 SetLeftControl 方法，它会自动处理对称性
                    point.AbsoluteLeftControlScaled += delta;
                }
                    break;
                case EToolState.RIGHTCTRL_DRAGGING:
                {
                    // 使用新的 SetRightControl 方法，它会自动处理对称性
                    point.AbsoluteRightControlScaled += delta;
                }
                    break;
            }

            UpdatePropertyPanel();
            UpdatePointPosition(selectedPoint_);
            UpdateSampleData();
        }

        lastMousePosition_ = position;
    }

    /// <summary>
    /// 主画布鼠标释放事件处理程序。
    /// </summary>
    private void OnMainCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        state_ = EToolState.NONE;
        MainCanvas.Cursor = Cursors.Arrow;
        UpdateStatus("就绪");
    }

    /// <summary>
    /// 缩放区域鼠标进入事件处理程序。
    /// </summary>
    private void OnZoomAreaMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Border zoomArea)
        {
            // 添加悬停效果
            zoomArea.Background = new SolidColorBrush(Color.FromArgb(0x10, 0x00, 0x00, 0x00));

            // 显示工具提示
            string tooltipText = zoomArea == VerticalZoomArea
                ? "使用鼠标滚轮进行垂直缩放"
                : "使用鼠标滚轮进行水平缩放";

            zoomArea.ToolTip = tooltipText;
        }
    }

    /// <summary>
    /// 缩放区域鼠标离开事件处理程序。
    /// </summary>
    private void OnZoomAreaMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Border zoomArea)
        {
            // 恢复透明背景
            zoomArea.Background = Brushes.Transparent;
        }
    }

    /// <summary>
    /// 主画布鼠标滚轮事件处理程序 - 缩放功能。
    /// </summary>
    private void OnMainCanvasMouseWheel(object sender, MouseWheelEventArgs e)
    {
        try
        {
            bool isZoomY = sender == VerticalZoomArea;
            bool isZoomX = !isZoomY && sender == HorizontalZoomArea;
            bool isZoomAll = !isZoomX;
            double canvasWidth = MainCanvas.ActualWidth;
            double canvasHeight = MainCanvas.ActualHeight;
            Point mousePosition = isZoomAll ? e.GetPosition(MainCanvas) : new Point(canvasWidth / 2, canvasHeight / 2);
            Vector worldPosition = transform_.ScreenToWorld(mousePosition);

            // 计算缩放因子
            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            Vector newScale = transform_.Scale * zoomFactor;
            if (isZoomY)
            {
                newScale.X = transform_.Scale.X;
            }

            if (isZoomX)
            {
                newScale.Y = transform_.Scale.Y;
            }

            transform_.SetPosition(
                mousePosition.X - worldPosition.X * newScale.X,
                mousePosition.Y - worldPosition.Y * newScale.Y);
            transform_.Scale = newScale;

            UpdateViewTransform();
            UpdateStatus($"缩放: {newScale * 100:F0}%");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"缩放错误: {ex.Message}");
        }
    }

    // ========== 工具栏事件 ==========

    /// <summary>
    /// 添加点按钮点击事件处理程序。
    /// </summary>
    private void OnAddPointClick(object sender, RoutedEventArgs e)
    {
        // 在视图中心添加点
        double canvasWidth = MainCanvas.ActualWidth;
        double canvasHeight = MainCanvas.ActualHeight;
        Point centerScreen = new Point(canvasWidth / 2, canvasHeight / 2);
        Vector centerWorld = transform_.ScreenToWorld(centerScreen);
        centerWorld.Y = -centerWorld.Y;
        AddPoint(centerWorld);
    }

    /// <summary>
    /// 画布右键点击事件处理程序 - 在点击位置添加新点。
    /// </summary>
    private void OnCanvasRightMouseDown(object sender, MouseButtonEventArgs e)
    {
        Vector worldPosition = transform_.ScreenToWorld(e.GetPosition(MainCanvas));
        worldPosition.Y = -worldPosition.Y;
        AddPoint(worldPosition);
    }

    /// <summary>
    /// 在指定位置添加新点。
    /// </summary>
    /// <param name="position">要添加点的世界坐标位置。</param>
    private void AddPoint(Vector position)
    {
        BezierPoint point;
        bezierCurve_.AddPoint(position, out point);
        selectedPoint_ = (BezierPointView) point;
        UpdateVisualization();
        UpdatePropertyPanel();

        UpdateStatus($"在 ({position.X:F1}, {position.Y:F1}) 添加了新点");
        UpdateSampleData();
    }

    /// <summary>
    /// 删除点按钮点击事件处理程序。
    /// </summary>
    private void OnDeletePointClick(object sender, RoutedEventArgs e)
    {
        if (selectedPoint_ != null)
        {
            bezierCurve_.DeletePoint(selectedPoint_);
            selectedPoint_ = null;
            UpdateVisualization();
            UpdatePropertyPanel();

            UpdateStatus($"已删除点");
            UpdateSampleData();
        }
    }

    /// <summary>
    /// 重置视图（拉伸）按钮点击事件处理程序。
    /// </summary>
    private void OnResetViewStretchClick(object sender, RoutedEventArgs e)
    {
        ResetCanvasView();
    }

    /// <summary>
    /// 重置视图（缩放）按钮点击事件处理程序。
    /// </summary>
    private void OnResetViewScaleClick(object sender, RoutedEventArgs e)
    {
        ResetCanvasView(false);
    }

    /// <summary>
    /// 精确采样复选框点击事件处理程序。
    /// </summary>
    private void OnAccurateCheckBoxClick(object sender, RoutedEventArgs e)
    {
        if (bezierCurve_ != null)
        {
            UpdateViewTransform();
        }
    }

    /// <summary>
    /// 主画布大小变化事件处理程序。
    /// </summary>
    private void OnMainCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (bezierCurve_ != null)
        {
            UpdateViewTransform();
        }
    }

    /// <summary>
    /// 窗口键盘按下事件处理程序 - 处理删除键。
    /// </summary>
    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            OnDeletePointClick(sender, e);
        }
    }

    /// <summary>
    /// 应用按钮点击事件处理程序。
    /// </summary>
    private void OnApplyButtonClick(object sender, RoutedEventArgs e)
    {
        onApply_?.Invoke(bezierCurve_!);
        Close();
    }

    /// <summary>
    /// 窗口关闭事件处理程序。
    /// </summary>
    private void OnWindowClosed(object? sender, EventArgs eventArgs)
    {
        // 清理资源
        onApply_ = null;
        Singleton<BezierEditor>.Reset();
    }

    /// <summary>
    /// 显示贝塞尔曲线编辑器窗口。
    /// </summary>
    /// <param name="bezierCurve">要编辑的贝塞尔曲线对象，如果为 null 则创建新曲线。</param>
    /// <param name="callback">应用按钮点击时的回调函数，接收编辑后的贝塞尔曲线对象。</param>
    public static void Show(BezierCurve? bezierCurve, Action<BezierCurve> callback)
    {
        BezierEditor editor = Singleton<BezierEditor>.Instance;
        editor.Show();
        editor.bezierCurve_ = bezierCurve == null ? null : new BezierCurveView(editor, bezierCurve);
        editor.onApply_ = callback;
        editor.OnShow();
    }
}