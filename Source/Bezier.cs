using System.Windows;

/// <summary>
/// 提供贝塞尔曲线相关的静态计算工具方法。
/// </summary>
public static class BezierUtils
{
    /// <summary>
    /// 获取杨辉三角指定行的数组
    /// </summary>
    /// <param name="rowIndex">行索引（从0开始）</param>
    /// <returns>杨辉三角指定行的系数数组</returns>
    /// <exception cref="ArgumentException">当 rowIndex 小于 0 时抛出</exception>
    public static List<double> GetPascalTriangleRow(int rowIndex)
    {
        if (rowIndex < 0)
        {
            throw new ArgumentException("Row index cannot be negative", nameof(rowIndex));
        }

        List<double> row = new List<double>();
        row.Add(1.0);

        for (int i = 1; i <= rowIndex; i++)
        {
            // 使用组合数公式计算杨辉三角系数
            double value = 1.0 * row[i - 1] * (rowIndex - i + 1) / i;
            row.Add(value);
        }

        return row;
    }

    /// <summary>
    /// 计算贝塞尔曲线上的点
    /// </summary>
    /// <typeparam name="T">向量类型，必须实现 IVector<T> 接口</typeparam>
    /// <param name="time">时间参数，范围 [0, 1]</param>
    /// <param name="points">控制点数组</param>
    /// <returns>贝塞尔曲线在指定时间参数处的点</returns>
    /// <exception cref="ArgumentException">当 points 为 null 或空数组时抛出</exception>
    public static Vector Bezier(double time, IReadOnlyList<Vector> points)
    {
        if (points == null || points.Count == 0)
        {
            throw new ArgumentException("Points array cannot be null or empty", nameof(points));
        }

        // 处理边界情况
        if (time <= 0.0)
        {
            return points[0];
        }

        if (time >= 1.0)
        {
            return points[points.Count - 1];
        }

        int count = points.Count;
        List<double> coefficients = GetPascalTriangleRow(count - 1);

        // 初始化结果向量
        Vector result = new Vector();

        // 计算贝塞尔曲线公式
        for (int i = 0; i < count; i++)
        {
            double coefficient = coefficients[i];
            double weight = coefficient * Math.Pow(1.0 - time, count - 1 - i) * Math.Pow(time, i);

            Vector weightedPoint = points[i] * weight;
            result += weightedPoint;
        }

        return result;
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
    /// <param name="time">X坐标值</param>
    /// <returns>曲线在指定X坐标处的Y值</returns>
    /// <exception cref="ArgumentException">
    /// 当Points为空或T不是支持的返回类型时抛出
    /// </exception>
    public T GetValue<T>(double time) where T : System.Numerics.INumber<T>
    {
        // 参数验证
        if (Points == null || Points.Count == 0)
        {
            throw new ArgumentException("Control points collection is empty", nameof(Points));
        }

        // 处理边界情况
        if (time <= Points[0].Position.X)
        {
            return T.CreateChecked(Points[0].Position.Y);
        }

        if (time >= Points[Points.Count - 1].Position.X)
        {
            return T.CreateChecked(Points[Points.Count - 1].Position.Y);
        }

        Vector[] points = new Vector[4];

        // 找到time所在的区间段
        for (int i = 0; i < Points.Count - 1; i++)
        {
            if (time >= Points[i].Position.X && time <= Points[i + 1].Position.X)
            {
                points[0] = Points[i].Position;
                points[1] = Points[i].Position + Points[i].RightControl;
                points[2] = Points[i + 1].Position + Points[i + 1].LeftControl;
                points[3] = Points[i + 1].Position;
                // 计算贝塞尔曲线上的点
                double t = (time - Points[i].Position.X) / (Points[i + 1].Position.X - Points[i].Position.X);

                // 三次贝塞尔曲线公式
                double y = BezierUtils.Bezier(t, points).Y;

                return T.CreateChecked(y);
            }
        }

        return T.CreateChecked(Points[0].Position.Y);
    }
}