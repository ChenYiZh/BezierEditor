using System.Windows;
using System.Windows.Media;

/// <summary>
/// 提供坐标变换功能，用于在屏幕坐标和世界坐标之间进行转换。
/// 支持平移和缩放变换，并维护变换矩阵。
/// </summary>
public class Transformer
{
    /// <summary> 当前变换的平移向量 </summary>
    private Vector position_ = new Vector(0, 0);

    /// <summary> 当前变换的缩放向量 </summary>
    private Vector scale_ = new Vector(1.0, 1.0);

    /// <summary> 由平移和缩放计算出的当前变换矩阵 </summary>
    private Matrix matrix_ = Matrix.Identity;

    /// <summary> 用于UI元素渲染的变换对象 </summary>
    private MatrixTransform transform_ = new MatrixTransform();

    /// <summary>
    /// 获取或设置变换的平移向量。
    /// 设置此属性会自动刷新变换矩阵。
    /// </summary>
    public Vector Position
    {
        get => position_;
        set
        {
            position_ = value;
            Refresh();
        }
    }

    /// <summary>
    /// 获取或设置变换的缩放向量。
    /// 设置此属性会自动刷新变换矩阵。
    /// </summary>
    public Vector Scale
    {
        get => scale_;
        set
        {
            scale_ = value;
            Refresh();
        }
    }

    /// <summary>
    /// 设置变换的平移位置。
    /// </summary>
    /// <param name="x">X 坐标。</param>
    /// <param name="y">Y 坐标。</param>
    public void SetPosition(double x, double y)
    {
        Position = new Vector(x, y);
    }

    /// <summary>
    /// 设置变换的缩放比例。
    /// </summary>
    /// <param name="x">X 方向的缩放比例。</param>
    /// <param name="y">Y 方向的缩放比例。</param>
    public void SetScale(double x, double y)
    {
        Scale = new Vector(x, y);
    }

    /// <summary>
    /// 在当前位置基础上移动指定的偏移量。
    /// </summary>
    /// <param name="x">X 方向的偏移量。</param>
    /// <param name="y">Y 方向的偏移量。</param>
    public void Move(double x, double y)
    {
        Position += new Vector(x, y);
    }

    /// <summary>
    /// 刷新变换矩阵。
    /// 根据当前的缩放和平移值重新计算变换矩阵。
    /// </summary>
    private void Refresh()
    {
        matrix_ = Matrix.Identity;
        matrix_.Scale(scale_.X, scale_.Y);
        matrix_.Translate(position_.X, position_.Y);
        transform_.Matrix = matrix_;
    }

    /// <summary>
    /// 将屏幕坐标转换为世界坐标。
    /// </summary>
    /// <param name="point">屏幕坐标点。</param>
    /// <returns>对应的世界坐标向量。</returns>
    public Vector ScreenToWorld(Point point)
    {
        Matrix inverseMatrix = matrix_;
        inverseMatrix.Invert();
        Point worldPoint = inverseMatrix.Transform(point);
        return new Vector(worldPoint.X, worldPoint.Y);
    }

    /// <summary>
    /// 将世界坐标转换为屏幕坐标。
    /// </summary>
    /// <param name="position">世界坐标向量。</param>
    /// <returns>对应的屏幕坐标点。</returns>
    public Point WorldToScreen(Vector position)
    {
        return matrix_.Transform(position).ToPoint();
    }

    /// <summary>
    /// 获取用于渲染的变换对象。
    /// </summary>
    /// <returns>当前变换的 [MatrixTransform](psi_element://System.Windows.Media.MatrixTransform) 对象。</returns>
    public Transform GetRenderTransform()
    {
        return transform_;
    }
}