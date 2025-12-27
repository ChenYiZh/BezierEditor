/// <summary>
/// 泛型单例模式基类
/// 提供线程安全的单例实例访问
/// </summary>
/// <typeparam name="T">需要实现单例的类类型</typeparam>
public class Singleton<T> where T : class, new()
{
    /// <summary>
    /// 单例实例引用，使用 volatile 确保多线程可见性
    /// </summary>
    private static volatile T? instance_ = null;

    /// <summary>
    /// 线程同步锁对象，用于实例创建的线程安全
    /// </summary>
    private static readonly object lockObject_ = new object();

    /// <summary>
    /// 获取单例实例的公共属性
    /// 使用双重检查锁定模式确保线程安全
    /// </summary>
    public static T Instance
    {
        get
        {
            // 第一次检查：如果实例已存在，直接返回
            if (instance_ == null)
            {
                lock (lockObject_)
                {
                    // 第二次检查：在锁内再次检查，防止多个线程同时通过第一次检查
                    if (instance_ == null)
                    {
                        instance_ = new T();
                    }
                }
            }
            return instance_;
        }
    }

    /// <summary>
    /// 重置单例实例，主要用于测试或重新初始化场景
    /// </summary>
    public static void Reset()
    {
        lock (lockObject_)
        {
            instance_ = null;
        }
    }

    /// <summary>
    /// 受保护的构造函数，防止外部直接实例化
    /// 派生类可以调用此构造函数
    /// </summary>
    protected Singleton()
    {
    }
}