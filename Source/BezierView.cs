using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

/// <summary>
/// 提供贝塞尔曲线视图相关的扩展方法和静态工具。
/// </summary>
public static class BezierView
{
    /// <summary>
    /// 将 Vector 转换为 Point。
    /// </summary>
    /// <param name="vector">要转换的向量。</param>
    /// <returns>转换后的点。</returns>
    public static Point ToPoint(this Vector vector)
    {
        return new Point(vector.X, vector.Y);
    }

    /// <summary>
    /// 将 Point 转换为 Vector。
    /// </summary>
    /// <param name="point">要转换的点。</param>
    /// <returns>转换后的向量。</returns>
    public static Vector ToVector(this Point point)
    {
        return new Vector(point.X, point.Y);
    }
}

/// <summary>
/// 创建 BezierPointView 实例的工厂类。
/// </summary>
public class BezierPointViewFactory : BezierPointFactory
{
    /// <summary>
    /// 获取关联的窗口。
    /// </summary>
    private Window Window { get; set; }

    /// <summary>
    /// 使用指定的窗口初始化 BezierPointViewFactory 的新实例。
    /// </summary>
    /// <param name="window">用于创建可视化元素的窗口。</param>
    public BezierPointViewFactory(Window window)
    {
        Window = window;
    }

    /// <summary>
    /// 根据给定的位置创建一个新的 BezierPointView 实例。
    /// </summary>
    /// <param name="position">新控制点的位置。</param>
    /// <returns>新创建的 BezierPointView 对象。</returns>
    public override BezierPoint Create(Vector position)
    {
        return new BezierPointView(Window, position.X, position.Y);
    }
}

/// <summary>
/// 表示贝塞尔曲线的可视化控制点，包含 WPF 可视化元素。
/// </summary>
public class BezierPointView : BezierPoint
{
    /// <summary>
    /// 获取或设置一个值，指示左右控制柄是否保持对称。
    /// </summary>
    public bool IsSymmetric { get; set; }

    /// <summary>
    /// 获取或设置是否选中该点。
    /// </summary>
    public bool IsSelected { get; set; } = false;

    /// <summary>
    /// 获取关联的窗口。
    /// </summary>
    public Window Window { get; private set; }

    /// <summary>
    /// 可视化元素数组。
    /// </summary>
    private UIElement[] elements_ = new UIElement[5];

    /// <summary>
    /// 获取所有可视化元素。
    /// </summary>
    public UIElement[] Elements => elements_;

    /// <summary>
    /// 获取或设置经过 Y 轴翻转（用于屏幕坐标系）和缩放后的位置。
    /// </summary>
    public Vector PositionScaled
    {
        get => new(Position.X, -Position.Y);
        set => Position = new Vector(value.X, -value.Y);
    }

    /// <summary>
    /// 获取或设置经过 Y 轴翻转（用于屏幕坐标系）和缩放后的相对左控制柄向量。
    /// </summary>
    public Vector RelativeLeftControlScaled
    {
        get => new(LeftControl.X, -LeftControl.Y);
        set
        {
            LeftControl = new Vector(value.X, -value.Y);
            if (IsSymmetric)
            {
                RightControl = -LeftControl;
            }
        }
    }

    /// <summary>
    /// 获取或设置经过 Y 轴翻转（用于屏幕坐标系）和缩放后的绝对左控制柄位置。
    /// </summary>
    public Vector AbsoluteLeftControlScaled
    {
        get => PositionScaled + RelativeLeftControlScaled;
        set => RelativeLeftControlScaled = value - PositionScaled;
    }

    /// <summary>
    /// 获取或设置经过 Y 轴翻转（用于屏幕坐标系）和缩放后的相对右控制柄向量。
    /// </summary>
    public Vector RelativeRightControlScaled
    {
        get => new(RightControl.X, -RightControl.Y);
        set
        {
            RightControl = new Vector(value.X, -value.Y);
            if (IsSymmetric)
            {
                LeftControl = -RightControl;
            }
        }
    }

    /// <summary>
    /// 获取或设置经过 Y 轴翻转（用于屏幕坐标系）和缩放后的绝对右控制柄位置。
    /// </summary>
    public Vector AbsoluteRightControlScaled
    {
        get => PositionScaled + RelativeRightControlScaled;
        set => RelativeRightControlScaled = value - PositionScaled;
    }

    /// <summary>
    /// 获取或设置主点椭圆。
    /// </summary>
    public Ellipse Point
    {
        get => (Ellipse) elements_[0];
        set => elements_[0] = value;
    }

    /// <summary>
    /// 获取或设置左控制点椭圆。
    /// </summary>
    public Ellipse LeftControlPoint
    {
        get => (Ellipse) elements_[1];
        set => elements_[1] = value;
    }

    /// <summary>
    /// 获取或设置右控制点椭圆。
    /// </summary>
    public Ellipse RightControlPoint
    {
        get => (Ellipse) elements_[2];
        set => elements_[2] = value;
    }

    /// <summary>
    /// 获取或设置左控制线。
    /// </summary>
    public Line LeftControlLine
    {
        get => (Line) elements_[3];
        set => elements_[3] = value;
    }

    /// <summary>
    /// 获取或设置右控制线。
    /// </summary>
    public Line RightControlLine
    {
        get => (Line) elements_[4];
        set => elements_[4] = value;
    }

    /// <summary>
    /// 使用指定的窗口和坐标初始化 BezierPointView 的新实例。
    /// </summary>
    /// <param name="window">关联的窗口。</param>
    /// <param name="x">控制点的 X 坐标。</param>
    /// <param name="y">控制点的 Y 坐标。</param>
    public BezierPointView(Window window, double x, double y) : base(x, y)
    {
        IsSymmetric = true;
        Window = window;
        CreateView();
    }

    /// <summary>
    /// 重置控制柄，如果是对称模式，则保持左右控制柄对称且长度相等。
    /// </summary>
    public void ResetControlHandles()
    {
        if (IsSymmetric)
        {
            // 计算当前控制杆的平均长度
            double leftLength = LeftControl.Length;
            double rightLength = RightControl.Length;
            double avgLength = (leftLength + rightLength) / 2;

            if (avgLength > 0)
            {
                // 保持左控制杆方向，调整长度
                if (leftLength > 0)
                {
                    LeftControl = LeftControl / leftLength * avgLength;
                }
                else
                {
                    LeftControl = new Vector(-avgLength, 0);
                }

                // 设置右控制杆为对称
                RightControl = new Vector(-LeftControl.X, -LeftControl.Y);
            }
            else
            {
                // 默认控制杆
                LeftControl = new Vector(-0.5, 0);
                RightControl = new Vector(0.5, 0);
            }
        }
    }

    /// <summary>
    /// 创建控制点的所有可视化元素（点、控制柄、连接线）。
    /// </summary>
    public void CreateView()
    {
        // 创建控制线
        LeftControlLine = CreateControlLine(Window, PositionScaled, AbsoluteLeftControlScaled);
        RightControlLine = CreateControlLine(Window, PositionScaled, AbsoluteRightControlScaled);

        // 创建控制点
        LeftControlPoint = CreatePointEllipse(Window, AbsoluteLeftControlScaled);
        RightControlPoint = CreatePointEllipse(Window, AbsoluteRightControlScaled);

        // 创建主点
        Point = CreatePointEllipse(Window, PositionScaled, false);
    }

    /// <summary>
    /// 刷新所有可视化元素。
    /// </summary>
    /// <param name="transform">坐标变换器。</param>
    public void Refresh(Transformer transform)
    {
        RefreshEllipse(transform, Point, PositionScaled, IsSelected);
        // 更新控制线
        RefreshLine(transform, LeftControlLine, AbsoluteLeftControlScaled);
        RefreshLine(transform, RightControlLine, AbsoluteRightControlScaled);
        // 更新控制点
        RefreshEllipse(transform, LeftControlPoint, AbsoluteLeftControlScaled);
        RefreshEllipse(transform, RightControlPoint, AbsoluteRightControlScaled);
    }

    /// <summary>
    /// 创建控制线。
    /// </summary>
    /// <param name="window">关联的窗口。</param>
    /// <param name="start">起点。</param>
    /// <param name="end">终点。</param>
    /// <returns>创建的控制线。</returns>
    private static Line CreateControlLine(Window window, Vector start, Vector end)
    {
        var line = new Line
        {
            X1 = start.X,
            Y1 = start.Y,
            X2 = end.X,
            Y2 = end.Y,
            Style = (Style) window.FindResource("ControlLineStyle")
        };

        return line;
    }

    /// <summary>
    /// 创建点椭圆。
    /// </summary>
    /// <param name="window">关联的窗口。</param>
    /// <param name="position">位置。</param>
    /// <param name="isSelected">是否选中状态。</param>
    /// <returns>创建的椭圆。</returns>
    private Ellipse CreatePointEllipse(Window window, Vector position, bool? isSelected = null)
    {
        Ellipse ellipse = new Ellipse
        {
            Style = isSelected.HasValue
                ? (isSelected.Value
                    ? (Style) window.FindResource("SelectedPointEllipseStyle")
                    : (Style) window.FindResource("PointEllipseStyle")
                )
                : (Style) window.FindResource("ControlPointEllipseStyle")
        };

        Canvas.SetLeft(ellipse, position.X - ellipse.Width / 2);
        Canvas.SetTop(ellipse, position.Y - ellipse.Height / 2);
        return ellipse;
    }

    /// <summary>
    /// 刷新控制线。
    /// </summary>
    /// <param name="transform">坐标变换器。</param>
    /// <param name="line">要刷新的线。</param>
    /// <param name="controlPoint">控制点位置。</param>
    private void RefreshLine(Transformer transform, Line line, Vector controlPoint)
    {
        Point start = transform.WorldToScreen(PositionScaled);
        Point end = transform.WorldToScreen(controlPoint);
        line.X1 = start.X;
        line.Y1 = start.Y;
        line.X2 = end.X;
        line.Y2 = end.Y;
        line.StrokeThickness = 1;
        line.RenderTransform = new ScaleTransform(1 / transform.Scale.X, 1 / transform.Scale.Y);
    }

    /// <summary>
    /// 刷新椭圆。
    /// </summary>
    /// <param name="transform">坐标变换器。</param>
    /// <param name="ellipse">要刷新的椭圆。</param>
    /// <param name="position">位置。</param>
    /// <param name="isSelected">是否选中状态。</param>
    private void RefreshEllipse(Transformer transform, Ellipse ellipse, Vector position, bool? isSelected = null)
    {
        ellipse.Width = 8;
        ellipse.Height = 8;
        if (isSelected.HasValue)
        {
            if (isSelected.Value)
            {
                ellipse.Style = (Style) Window.FindResource("SelectedPointEllipseStyle");
                ellipse.Width = 12;
                ellipse.Height = 12;
            }
            else
            {
                ellipse.Style = (Style) Window.FindResource("PointEllipseStyle");
                ellipse.Width = 10;
                ellipse.Height = 10;
            }
        }

        ellipse.RenderTransform = new ScaleTransform(1 / transform.Scale.X, 1 / transform.Scale.Y);

        // 更新位置（因为大小可能改变了）
        Canvas.SetLeft(ellipse, position.X - ellipse.Width / transform.Scale.X / 2);
        Canvas.SetTop(ellipse, position.Y - ellipse.Height / transform.Scale.Y / 2);
    }
}

/// <summary>
/// 表示贝塞尔曲线的可视化视图，管理控制点的可视化元素和曲线绘制。
/// </summary>
public sealed class BezierCurveView : BezierCurve
{
    /// <summary>
    /// 可观察的点集合，用于数据绑定。
    /// </summary>
    private ObservableCollection<BezierPoint> itemSources_;

    /// <summary>
    /// 获取可观察的点集合。
    /// </summary>
    public ObservableCollection<BezierPoint> ItemSources => itemSources_;

    /// <summary>
    /// 使用指定的窗口初始化 BezierCurveView 的新实例。
    /// </summary>
    /// <param name="window">关联的窗口。</param>
    public BezierCurveView(Window window) : base()
    {
        Factory = new BezierPointViewFactory(window);
        InitializeDefaultPoints();
        itemSources_ = new ObservableCollection<BezierPoint>(Points);
    }

    /// <summary>
    /// 使用指定的窗口和现有曲线初始化 BezierCurveView 的新实例。
    /// </summary>
    /// <param name="window">关联的窗口。</param>
    /// <param name="bezierCurve">要复制的源曲线对象。</param>
    public BezierCurveView(Window window, BezierCurve bezierCurve) : base(bezierCurve)
    {
        Factory = new BezierPointViewFactory(window);
        InitializeDefaultPoints();
        itemSources_ = new ObservableCollection<BezierPoint>(Points);
    }

    /// <summary>
    /// 在曲线上添加一个新的控制点。
    /// </summary>
    /// <param name="position">新控制点的位置坐标。</param>
    /// <param name="newPoint">输出参数，返回新创建的 BezierPoint 对象。</param>
    /// <returns>新控制点插入的索引位置。</returns>
    public override int AddPoint(Vector position, out BezierPoint newPoint)
    {
        int index = base.AddPoint(position, out newPoint);
        itemSources_ = new ObservableCollection<BezierPoint>(Points);
        return index;
    }

    /// <summary>
    /// 从曲线中删除指定的控制点。
    /// </summary>
    /// <param name="point">要删除的 BezierPoint 对象。</param>
    /// <returns>如果点成功删除则为 true；否则为 false。</returns>
    public override bool DeletePoint(BezierPoint? point)
    {
        bool ret = base.DeletePoint(point);
        itemSources_ = new ObservableCollection<BezierPoint>(Points);
        return ret;
    }

    /// <summary>
    /// 根据 X 坐标对控制点进行排序。
    /// </summary>
    public void Sort()
    {
        List<BezierPoint> points = (List<BezierPoint>) Points;
        points.Sort((a, b) => a.Position.X.CompareTo(b.Position.X));
        itemSources_ = new ObservableCollection<BezierPoint>(Points);
    }

    /// <summary>
    /// 刷新所有可视化元素。
    /// </summary>
    /// <param name="mainCanvas">主画布。</param>
    /// <param name="bezierCanvas">贝塞尔曲线画布。</param>
    /// <param name="bezierPath">贝塞尔曲线路径。</param>
    /// <param name="selectedPoint">当前选中的点。</param>
    /// <param name="transform">坐标变换器。</param>
    /// <param name="bAccurate">是否使用精确采样模式绘制曲线。</param>
    public void Refresh(
        Canvas mainCanvas,
        Canvas bezierCanvas,
        Path bezierPath,
        BezierPoint? selectedPoint,
        Transformer transform)
    {
        Canvas canvas = bezierCanvas;

        ClearVisualization(bezierCanvas, bezierPath);

        RefreshPath(mainCanvas, bezierPath, transform);

        // 为每个点创建可视化元素
        foreach (BezierPoint point in Points)
        {
            BezierPointView viewPoint = (BezierPointView) point;
            viewPoint.CreateView();
            viewPoint.IsSelected = point == selectedPoint;
            // 添加到画布
            foreach (UIElement element in viewPoint.Elements)
            {
                canvas.Children.Add(element);
            }

            viewPoint.Refresh(transform);
        }
    }

    /// <summary>
    /// 清除所有可视化元素。
    /// </summary>
    /// <param name="bezierCanvas">贝塞尔曲线画布。</param>
    /// <param name="bezierPath">贝塞尔曲线路径。</param>
    private void ClearVisualization(Canvas bezierCanvas, Path bezierPath)
    {
        Canvas canvas = bezierCanvas;
        Path path = bezierPath;
        // 移除所有动态添加的元素（除了贝塞尔曲线路径）
        List<UIElement> elementsToRemove = new List<UIElement>();

        foreach (UIElement element in canvas.Children)
        {
            if (element is Ellipse || element is Line)
            {
                if (element != path)
                {
                    elementsToRemove.Add(element);
                }
            }
        }

        foreach (var element in elementsToRemove)
        {
            canvas.Children.Remove(element);
        }
    }

    /// <summary>
    /// 刷新贝塞尔曲线路径。
    /// </summary>
    /// <param name="mainCanvas">主画布。</param>
    /// <param name="bezierPath">贝塞尔曲线路径。</param>
    /// <param name="transform">坐标变换器。</param>
    private void RefreshPath(Canvas mainCanvas, Path bezierPath, Transformer transform)
    {
        // 更新贝塞尔曲线路径
        bezierPath.Data = CreatePathGeometry(mainCanvas, transform);
        bezierPath.StrokeThickness = 3;
        bezierPath.RenderTransform = new ScaleTransform(1 / transform.Scale.X, 1 / transform.Scale.Y);
    }

    /// <summary>
    /// 创建贝塞尔曲线路径几何图形。
    /// </summary>
    /// <param name="mainCanvas">主画布。</param>
    /// <param name="transform">坐标变换器。</param>
    /// <param name="bAccurate">是否使用精确采样模式绘制曲线。</param>
    /// <returns>路径几何图形。</returns>
    private PathGeometry CreatePathGeometry(Canvas mainCanvas, Transformer transform)
    {
        IReadOnlyList<BezierPoint> points = Points;
        if (points.Count < 2)
        {
            return new PathGeometry();
        }

        PathGeometry pathGeometry = new PathGeometry();
        PathFigure pathFigure = new PathFigure();
        // 起始点
        BezierPointView firstPoint = (BezierPointView) points[0];
        pathFigure.StartPoint = transform.WorldToScreen(firstPoint.PositionScaled);

        // 添加贝塞尔曲线段
        for (int i = 1; i < points.Count; i++)
        {
            BezierPointView prevPoint = (BezierPointView) points[i - 1];
            BezierPointView currentPoint = (BezierPointView) points[i];

            BezierSegment bezierSegment = new BezierSegment(
                transform.WorldToScreen(prevPoint.PositionScaled + prevPoint.RelativeRightControlScaled),
                transform.WorldToScreen(currentPoint.PositionScaled + currentPoint.RelativeLeftControlScaled),
                transform.WorldToScreen(currentPoint.PositionScaled),
                true);

            pathFigure.Segments.Add(bezierSegment);
        }

        pathGeometry.Figures.Add(pathFigure);
        return pathGeometry;
    }

    /// <summary>
    /// 获取曲线中所有控制点的 X 和 Y 坐标范围。
    /// </summary>
    /// <param name="minX">最小 X 坐标。</param>
    /// <param name="minY">最小 Y 坐标。</param>
    /// <param name="maxX">最大 X 坐标。</param>
    /// <param name="maxY">最大 Y 坐标。</param>
    public void GetValueRange(out double minX, out double minY, out double maxX, out double maxY)
    {
        Vector pos1 = ((BezierPointView) Points[0]).PositionScaled;
        Vector pos2 = ((BezierPointView) Points[1]).PositionScaled;
        minX = Math.Min(pos1.X, pos2.X);
        minY = Math.Min(pos1.Y, pos2.Y);
        maxX = Math.Max(pos1.X, pos2.X);
        maxY = Math.Max(pos1.Y, pos2.Y);
        foreach (var bezierPoint in Points)
        {
            BezierPointView point = (BezierPointView) bezierPoint;
            if (point.PositionScaled.X < minX)
            {
                minX = point.PositionScaled.X;
            }

            if (point.PositionScaled.X > maxX)
            {
                maxX = point.PositionScaled.X;
            }

            if (point.PositionScaled.Y < minY)
            {
                minY = point.PositionScaled.Y;
            }

            if (point.PositionScaled.Y > maxY)
            {
                maxY = point.PositionScaled.Y;
            }
        }
    }
}