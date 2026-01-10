using System.Windows;

/// <summary>
/// 提供贝塞尔曲线相关的静态计算工具方法。
/// </summary>
public static class BezierUtils
{
    /// <summary>
    /// 使用多项式形式计算三次贝塞尔曲线上的点。
    /// </summary>
    /// <param name="t">时间参数，范围 [0, 1]</param>
    /// <param name="p0">第一个控制点</param>
    /// <param name="p1">第二个控制点</param>
    /// <param name="p2">第三个控制点</param>
    /// <param name="p3">第四个控制点</param>
    /// <returns>三次贝塞尔曲线在时间参数 t 处的点</returns>
    public static Vector Evaluate(double t, Vector p0, Vector p1, Vector p2, Vector p3)
    {
        // 处理边界情况
        if (t <= 0.0)
        {
            return p0;
        }

        if (t >= 1.0)
        {
            return p3;
        }

        Vector a = -p0 + 3.0f * p1 - 3.0f * p2 + p3;
        Vector b = 3.0f * p0 - 6.0f * p1 + 3.0f * p2;
        Vector c = -3.0f * p0 + 3.0f * p1;
        Vector d = p0;

        return ((a * t + b) * t + c) * t + d;
    }

    /// <summary>
    /// 计算三次贝塞尔曲线在 t 处的 X 坐标
    /// </summary>
    /// <param name="t">时间参数，范围 [0, 1]</param>
    /// <param name="p0">第一个控制点</param>
    /// <param name="p1">第二个控制点</param>
    /// <param name="p2">第三个控制点</param>
    /// <param name="p3">第四个控制点</param>
    /// <returns>三次贝塞尔曲线在时间参数 t 处的 X 坐标</returns>
    public static double EvaluateX(double t, Vector p0, Vector p1, Vector p2, Vector p3)
    {
        // 处理边界情况
        if (t <= 0.0)
        {
            return p0.X;
        }

        if (t >= 1.0)
        {
            return p3.X;
        }

        double a = -p0.X + 3.0 * p1.X - 3.0 * p2.X + p3.X;
        double b = 3.0 * p0.X - 6.0 * p1.X + 3.0 * p2.X;
        double c = -3.0 * p0.X + 3.0 * p1.X;
        double d = p0.X;

        return ((a * t + b) * t + c) * t + d;
    }

    /// <summary>
    /// 计算三次贝塞尔曲线在 t 处的 Y 坐标
    /// </summary>
    /// <param name="t">时间参数，范围 [0, 1]</param>
    /// <param name="p0">第一个控制点</param>
    /// <param name="p1">第二个控制点</param>
    /// <param name="p2">第三个控制点</param>
    /// <param name="p3">第四个控制点</param>
    /// <returns>三次贝塞尔曲线在时间参数 t 处的 Y 坐标</returns>
    public static double EvaluateY(double t, Vector p0, Vector p1, Vector p2, Vector p3)
    {
        // 处理边界情况
        if (t <= 0.0)
        {
            return p0.Y;
        }

        if (t >= 1.0)
        {
            return p3.Y;
        }

        double a = -p0.Y + 3.0 * p1.Y - 3.0 * p2.Y + p3.Y;
        double b = 3.0 * p0.Y - 6.0 * p1.Y + 3.0 * p2.Y;
        double c = -3.0 * p0.Y + 3.0 * p1.Y;
        double d = p0.Y;

        return ((a * t + b) * t + c) * t + d;
    }

    /// <summary>
    /// 计算三次贝塞尔曲线在 t 处的 X 坐标导数
    /// </summary>
    /// <param name="t">时间参数，范围 [0, 1]</param>
    /// <param name="p0">第一个控制点</param>
    /// <param name="p1">第二个控制点</param>
    /// <param name="p2">第三个控制点</param>
    /// <param name="p3">第四个控制点</param>
    /// <returns>三次贝塞尔曲线在时间参数 t 处的 X 坐标导数</returns>
    private static double EvaluateDerivativeX(double t, Vector p0, Vector p1, Vector p2, Vector p3)
    {
        double a = -p0.X + 3.0 * p1.X - 3.0 * p2.X + p3.X;
        double b = 3.0 * p0.X - 6.0 * p1.X + 3.0 * p2.X;
        double c = -3.0 * p0.X + 3.0 * p1.X;

        return (3.0 * a * t + 2.0 * b) * t + c;
    }

    /// <summary>
    /// 使用数值方法找到给定X坐标对应的t值
    /// 使用二分查找法进行粗定位，然后使用牛顿迭代法提高精度
    /// </summary>
    /// <param name="targetX">目标X坐标</param>
    /// <param name="p0">第一个控制点</param>
    /// <param name="p1">第二个控制点</param>
    /// <param name="p2">第三个控制点</param>
    /// <param name="p3">第四个控制点</param>
    /// <param name="maxIterations">最大迭代次数，默认20</param>
    /// <param name="tolerance">容差，默认1e-10</param>
    /// <returns>对应的t值，范围[0, 1]</returns>
    public static double FindTForX(double targetX, Vector p0, Vector p1, Vector p2, Vector p3,
        int maxIterations = 20, double tolerance = 1e-10)
    {
        // 步骤1：使用二分查找法进行粗定位
        double low = 0.0;
        double high = 1.0;
        double t = 0.5;

        // 二分查找，最多进行10次迭代
        for (int i = 0; i < 10; i++)
        {
            t = (low + high) / 2.0;
            double currentX = EvaluateX(t, p0, p1, p2, p3);

            if (Math.Abs(currentX - targetX) < tolerance)
            {
                return t;
            }

            if (currentX < targetX)
            {
                low = t;
            }
            else
            {
                high = t;
            }
        }

        // 步骤2：使用牛顿迭代法提高精度
        for (int i = 0; i < maxIterations; i++)
        {
            double currentX = EvaluateX(t, p0, p1, p2, p3);
            double derivativeX = EvaluateDerivativeX(t, p0, p1, p2, p3);

            // 检查导数是否接近0，避免除零错误
            if (Math.Abs(derivativeX) < 1e-15)
            {
                break;
            }

            double delta = (currentX - targetX) / derivativeX;
            t -= delta;

            // 确保t在[0, 1]范围内
            if (t < 0.0) t = 0.0;
            if (t > 1.0) t = 1.0;

            // 检查是否达到精度要求
            if (Math.Abs(delta) < tolerance)
            {
                break;
            }
        }

        return t;
    }
}

/// <summary>
/// 提供创建 BezierPoint 实例的工厂类。
/// </summary>
public class BezierPointFactory
{
    /// <summary>
    /// 根据给定的位置创建一个新的 BezierPoint 实例。
    /// </summary>
    /// <param name="position">新控制点的位置。</param>
    /// <returns>新创建的 BezierPoint 对象。</returns>
    public virtual BezierPoint Create(Vector position)
    {
        return new BezierPoint(position.X, position.Y);
    }
}

/// <summary>
/// 表示贝塞尔曲线的一个控制点，包含位置和左右控制柄信息
/// </summary>
public class BezierPoint
{
    /// <summary>
    /// 获取或设置控制点的位置坐标
    /// </summary>
    public Vector Position { get; set; }

    /// <summary>
    /// 获取或设置左侧控制柄的向量
    /// 用于控制曲线进入该点的方向和曲率
    /// </summary>
    public Vector LeftControl { get; set; }

    /// <summary>
    /// 获取或设置右侧控制柄的向量
    /// 用于控制曲线离开该点的方向和曲率
    /// </summary>
    public Vector RightControl { get; set; }

    /// <summary>
    /// 初始化一个新的控制点实例
    /// 默认位置为(0,0)，左右控制柄分别为(-0.5,0)和(0.5,0)
    /// </summary>
    public BezierPoint()
    {
        Position = new Vector(0, 0);
        LeftControl = new Vector(-0.5, 0);
        RightControl = new Vector(0.5, 0);
    }

    /// <summary>
    /// 使用指定坐标初始化一个新的控制点实例
    /// </summary>
    /// <param name="x">控制点的X坐标</param>
    /// <param name="y">控制点的Y坐标</param>
    public BezierPoint(double x, double y) : this()
    {
        Position = new Vector(x, y);
    }
}

/// <summary>
/// 表示一个贝塞尔曲线对象，用于管理和操作贝塞尔曲线的控制点
/// </summary>
public class BezierCurve
{
    private List<BezierPoint> points_;

    /// <summary>
    /// 获取贝塞尔曲线的控制点集合
    /// 这些点按X坐标排序，用于构建平滑的贝塞尔曲线
    /// </summary>
    public IReadOnlyList<BezierPoint> Points => points_;

    /// <summary>
    /// 获取或设置用于创建新控制点的工厂对象。
    /// </summary>
    protected BezierPointFactory Factory { get; set; } = new BezierPointFactory();

    /// <summary>
    /// 使用另一个 BezierCurve 实例初始化新实例（拷贝构造函数）。
    /// </summary>
    /// <param name="bezierCurve">要复制的源曲线对象。</param>
    public BezierCurve(BezierCurve bezierCurve)
    {
        points_ = bezierCurve.points_;
        Factory = bezierCurve.Factory;
    }

    /// <summary>
    /// 初始化一个新的贝塞尔曲线实例
    /// 默认会创建两个控制点：(0,0)和(1,1)
    /// </summary>
    public BezierCurve()
    {
        points_ = new List<BezierPoint>();
        InitializeDefaultPoints();
    }

    /// <summary>
    /// 初始化默认控制点
    /// 默认创建两个控制点：(0,0)和(1,1)
    /// </summary>
    protected void InitializeDefaultPoints()
    {
        if (points_.Count < 2)
        {
            points_.Clear();
            AddPoint(new Vector(0, 0), out _);
            AddPoint(new Vector(1, 1), out _);
        }
        else
        {
            List<BezierPoint> points = points_;
            points_ = new List<BezierPoint>(points.Count);
            foreach (BezierPoint point in points)
            {
                AddPoint(point.Position, out _);
            }
        }
    }

    /// <summary>
    /// 在曲线上添加一个新的控制点
    /// 点将根据X坐标自动插入到正确位置以保持排序
    /// </summary>
    /// <param name="position">新控制点的位置坐标</param>
    /// <param name="newPoint">输出参数，返回新创建的BezierPoint对象</param>
    /// <returns>
    /// 新控制点插入的索引位置（基于当前Points集合）
    /// 返回值为0表示插入到开头，返回值为Points.Count表示添加到末尾
    /// </returns>
    /// <remarks>
    /// 该方法保证：
    /// 1. 控制点始终按X坐标升序排列
    /// 2. 允许添加相同X坐标的点（会插入到相同X坐标点的后面）
    /// 3. 输出参数newPoint始终不为null
    /// 
    /// 注意：如果position包含无效值（如NaN或Infinity），将抛出ArgumentException
    /// </remarks>
    /// <exception cref="ArgumentException">当position包含无效坐标值时抛出</exception>
    public virtual int AddPoint(Vector position, out BezierPoint newPoint)
    {
        if (double.IsNaN(position.X) || double.IsNaN(position.Y) ||
            double.IsInfinity(position.X) || double.IsInfinity(position.Y))
        {
            throw new ArgumentException("Position coordinates cannot be NaN or Infinity", nameof(position));
        }

        newPoint = Factory.Create(position);

        // 找到插入位置（按X坐标排序）
        int insertIndex = 0;
        for (int i = 0; i < Points.Count; i++)
        {
            if (position.X > Points[i].Position.X)
            {
                insertIndex = i + 1;
            }
        }

        if (insertIndex >= Points.Count)
        {
            points_.Add(newPoint);
        }
        else
        {
            points_.Insert(insertIndex, newPoint);
        }

        return insertIndex;
    }

    /// <summary>
    /// 从曲线中删除指定的控制点
    /// 如果点不存在或点数少于3个(保持曲线有效性)，则不执行任何操作
    /// </summary>
    /// <param name="point">要删除的BezierPoint对象，可以为null</param>
    /// <returns>
    /// true - 如果点成功删除；
    /// false - 如果点不存在、参数为null或当前点数≤2(保持曲线有效性)
    /// </returns>
    /// <remarks>
    /// 该方法确保曲线始终保留至少2个控制点，这是构建有效贝塞尔曲线的最低要求。
    /// 删除操作会保持控制点的X坐标排序不变。
    /// </remarks>
    public virtual bool DeletePoint(BezierPoint? point)
    {
        if (point != null && Points.Count > 2)
        {
            return points_.Remove(point);
        }

        return false;
    }

    /// <summary>
    /// 根据给定的X坐标(time)获取贝塞尔曲线上对应的Y值
    /// </summary>
    /// <typeparam name="T">返回值的类型，支持基础数值类型(int, long, float, double等)</typeparam>
    /// <param name="x">X坐标值</param>
    /// <returns>曲线在指定X坐标处的Y值</returns>
    /// <exception cref="ArgumentException">
    /// 当Points为空或T不是支持的返回类型时抛出
    /// </exception>
    public T GetValue<T>(double x) where T : System.Numerics.INumber<T>
    {
        // 参数验证
        if (Points == null || Points.Count == 0)
        {
            throw new ArgumentException("Control points collection is empty", nameof(Points));
        }

        // 处理边界情况
        if (x <= Points[0].Position.X)
        {
            return T.CreateChecked(Points[0].Position.Y);
        }

        if (x >= Points[Points.Count - 1].Position.X)
        {
            return T.CreateChecked(Points[Points.Count - 1].Position.Y);
        }

        Vector[] points = new Vector[4];

        // 找到time所在的区间段
        for (int i = 0; i < Points.Count - 1; i++)
        {
            if (x >= Points[i].Position.X && x <= Points[i + 1].Position.X)
            {
                points[0] = Points[i].Position;
                points[1] = Points[i].Position + Points[i].RightControl;
                points[2] = Points[i + 1].Position + Points[i + 1].LeftControl;
                points[3] = Points[i + 1].Position;
                // 计算贝塞尔曲线上的点
                double t = BezierUtils.FindTForX(x, points[0], points[1], points[2], points[3]);

                // 三次贝塞尔曲线公式
                double y = BezierUtils.EvaluateY(t, points[0], points[1], points[2], points[3]);

                return T.CreateChecked(y);
            }
        }

        return T.CreateChecked(Points[0].Position.Y);
    }
}